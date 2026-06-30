using System.Security.Cryptography;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.DataSync;
using POS.Common.Helpers;
using POS.Infrastructure.Files;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Repositories.Interfaces;
using IFileLogHelper = POS.Infrastructure.Logging.IFileLogHelper;

namespace POS.Application.Features.DataSync;

/// <summary>
/// Orchestrate sinh file master data .zip. Business logic ở đây; I/O (DB stream, zip, khóa) ở Infrastructure.
/// Định dạng file: JSON envelope SyncTableList (bám convention DataRawService.CreateFileSODFakeAsync).
/// </summary>
public sealed class MasterDataSyncService(
    ISyncRepository syncRepository,
    IFileArchiveService fileArchiveService,
    ISyncFileLock syncFileLock,
    IOptions<MasterDataSyncOptions> options,
    IKibanaService kibanaService,
    IFileLogHelper fileLogHelper) : IMasterDataSyncService
{
    private readonly MasterDataSyncOptions _opt = options.Value;

    // TODO: confirm vs POS parser — convention trước đây dùng "DELETE-INSERT"; user yêu cầu "TRUNC-INSERT".
    private const string ActionTruncInsert = "TRUNC-INSERT";
    // Bảng lớn tách nhiều file: batch đầu TRUNC-INSERT, các batch sau INSERT (append) — tránh truncate sạch batch trước.
    private const string ActionInsert = "INSERT";

    public async Task LogDownloadAsync(
        string? fileName, string? filePath, long fileSizeBytes, long durationMs,
        string status, string? clientIp, CancellationToken ct = default)
    {
        try
        {
            // Best-effort parse siteCode/posTerminal từ tên zip {site}_{type}_{terminal}_{yyyyMMdd}.zip.
            string? siteCode = null, posTerminal = null;
            var name = Path.GetFileNameWithoutExtension(fileName ?? string.Empty);
            var parts = name.Split('_');
            if (parts.Length >= 4)
            {
                siteCode = parts[0];
                posTerminal = parts[2];
            }

            await syncRepository.InsertDownloadLogAsync(
                siteCode, posTerminal, fileName, filePath, fileSizeBytes, durationMs, status, clientIp, ct);
        }
        catch (Exception ex)
        {
            // Fail-safe: log lỗi không được phá luồng download (vd bảng chưa tạo).
            fileLogHelper.WriteExpLogs("MasterDataSyncService.LogDownloadAsync", ex);
        }
    }

    public async Task<GetMasterDataFileResult> EnsureMasterDataFileAsync(
        GetMasterDataFileRequest req, CancellationToken ct = default)
    {
        var dateSuffix = _opt.DateInZipName ? $"_{DateTime.Now:yyyyMMdd}" : string.Empty;
        var zipName = $"{req.SiteCode}_{req.TypeSync}_{req.PosTerminal}{dateSuffix}.zip";
        var zipPath = Path.Combine(req.TargetDir, zipName);

        // 1. File hợp lệ trong ngày đã có → trả ngay (R1: file cũ bị xóa trong IsTodayZipValid).
        if (IsTodayZipValid(zipPath))
            return Success(zipName, req);

        // 2. Khóa theo terminal (bounded) + double-check.
        var lockKey = $"{req.TypeSync}_{req.SiteCode}_{req.PosTerminal}";
        using var lockHandle = await syncFileLock.AcquireAsync(lockKey, ct);
        if (IsTodayZipValid(zipPath))
            return Success(zipName, req);

        Directory.CreateDirectory(req.TargetDir);
        var tmpDir = Path.Combine(req.TargetDir, $"_tmp_{Guid.NewGuid():N}");
        var tmpZip = Path.Combine(req.TargetDir, $"{Guid.NewGuid():N}.zip");
        var tableCount = 0;
        try
        {
            Directory.CreateDirectory(tmpDir);

            var tables = await syncRepository.GetSyncTablesAsync(ct);
            var processId = StringHelper.InitRandomString(req.SiteCode);

            // Precompute (table, index) trước khi song song hóa để tableIndex ổn định và không race.
            var tableEntries = tables
                .Where(t => !string.IsNullOrWhiteSpace(t.TableName))
                .Select((t, idx) => (Table: t, Index: idx + 1))
                .ToList();

            // Song song hóa SP2: mỗi bảng dùng SqlConnection riêng → thread-safe.
            // MaxParallelTables ≤ 0 → sequential (fallback an toàn).
            var skippedCount = 0; // đếm bảng SP2 trả 0 dòng (thread-safe qua Interlocked)
            await Parallel.ForEachAsync(tableEntries, new ParallelOptions
            {
                MaxDegreeOfParallelism = _opt.MaxParallelTables > 0 ? _opt.MaxParallelTables : 1,
                CancellationToken = ct
            }, async (entry, token) =>
            {
                // Random 1 LẦN/bảng (KHÔNG mỗi batch) để các file cùng bảng chung prefix + sắp đúng thứ tự.
                var rnd = StringHelper.InitRandomString(string.Empty);
                var tableIndex = entry.Index;

                // Tên file mỗi batch: {Site}_{Table}_{rnd}_{tableIndex}_{batchNo:D3}.txt (batchNo zero-pad để sort đúng).
                string FileNameFor(int batchNo) =>
                    $"{req.SiteCode}_{entry.Table.TableName}_{rnd}_{tableIndex}_{batchNo:D3}.txt";

                // Batch đầu TRUNC-INSERT (truncate 1 lần), các batch sau INSERT (append) — chống mất dữ liệu khi tách file.
                string ActionFor(int batchNo) => batchNo == 1 ? ActionTruncInsert : ActionInsert;

                // @POSLastCounter = 0 khi typeSync=ALL hoặc IsFirstDataAll; ngược lại dùng POSLastCounter của bảng.
                var counter = req.TypeSync == "ALL" || entry.Table.IsFirstDataAll ? 0L : entry.Table.POSLastCounter;

                // Filter per-store: bảng IsByStore=1 + có ColumnFilter → WHERE [ColumnFilter] = siteCode.
                // Bảng global (IsByStore=0) hoặc thiếu ColumnFilter → không filter (lấy all).
                var filterColumn = string.Empty;
                var filterValue = string.Empty;
                if (entry.Table.IsByStore && !string.IsNullOrWhiteSpace(entry.Table.ColumnFilter))
                {
                    filterColumn = entry.Table.ColumnFilter.Trim();
                    filterValue = req.SiteCode;
                }
                else if (entry.Table.IsByStore)
                {
                    // IsByStore=1 nhưng thiếu ColumnFilter → fallback lấy all (tránh mất data), cảnh báo để rà cấu hình.
                    kibanaService.LogInfo("MasterDataSync.Filter", req.PosTerminal,
                        $"Bảng '{entry.Table.TableName}' IsByStore=1 nhưng ColumnFilter rỗng → export toàn bộ (không filter).");
                }

                try
                {
                    var (_, rowCount) = await syncRepository.StreamTableToFilesAsync(
                        entry.Table, tmpDir, FileNameFor, ActionFor, processId, counter, _opt.BatchSizePerFile,
                        filterColumn, filterValue, token);

                    if (rowCount == 0)
                    {
                        // SP2 trả 0 dòng → không tạo file → tránh POS nhận TRUNC-INSERT + Data:[] → mất data.
                        Interlocked.Increment(ref skippedCount);
                        kibanaService.LogInfo("MasterDataSync.EmptyTable", req.PosTerminal,
                            $"Bảng '{entry.Table.TableName}' SP2 trả 0 dòng " +
                            $"(filter: [{filterColumn}]='{filterValue}') — bỏ qua, không tạo file.");
                    }
                }
                catch (Exception exTable)
                {
                    // Nêu rõ bảng + tham số đang chạy để biết SP [SyncGetDataByTable] lỗi ở bảng nào.
                    throw new InvalidOperationException(
                        $"Sinh master data lỗi tại bảng '{entry.Table.TableName}' " +
                        $"(OrderByName='{entry.Table.OrderByName}', POSLastCounter={counter}, Procedure='{entry.Table.Procedure}'): " +
                        exTable.Message, exTable);
                }
            });
            tableCount = tableEntries.Count;

            // Nén thư mục tạm → zip tạm → File.Move sang tên chính thức (publish atomic).
            fileArchiveService.CreateZipFromDirectory(tmpDir, tmpZip);
            File.Move(tmpZip, zipPath, overwrite: true);

            // SHA-256 companion file: ops/monitoring verify integrity bằng `sha256sum`.
            // POS có thể download {zipName}.sha256 để self-verify sau khi tải zip (tùy chọn).
            var hash = await ComputeSha256HexAsync(zipPath, ct);
            await File.WriteAllTextAsync(zipPath + ".sha256", hash, ct);

            CleanupSiblingZips(req, zipName);

            if (skippedCount == tableCount && tableCount > 0)
                kibanaService.LogInfo("MasterDataSync.AllEmpty", req.PosTerminal,
                    $"Cảnh báo: {zipName} được publish nhưng TẤT CẢ {tableCount} bảng SP2 trả 0 dòng — zip rỗng!");

            kibanaService.LogInfo("MasterDataSync.Ensure", req.PosTerminal,
                $"Generated {zipName} ({tableCount} tables, {skippedCount} empty skipped) at {req.TargetDir}");
            return Success(zipName, req, tableCount);
        }
        catch (Exception ex)
        {
            // error-cleanup: KHÔNG publish file thiếu bảng — cleanup ở finally, log + throw.
            kibanaService.LogException("MasterDataSync.Ensure", req.PosTerminal, 0, "",
                $"Generate {zipName} failed: {ex.Message}");
            fileLogHelper.WriteExpLogs("MasterDataSyncService.EnsureMasterDataFileAsync", ex);
            throw;
        }
        finally
        {
            TryDeleteDirectory(tmpDir);
            TryDeleteFile(tmpZip);
        }
    }

    /// <summary>
    /// File tồn tại VÀ được tạo trong ngày → hợp lệ. File cũ (sang ngày mới) → xóa để sinh lại (R1).
    /// </summary>
    private bool IsTodayZipValid(string zipPath)
    {
        if (!File.Exists(zipPath)) return false;
        try
        {
            var info = new FileInfo(zipPath);
            if (info.LastWriteTime.Date == DateTime.Now.Date)
                return true;
            // File cũ → xóa cả zip lẫn companion .sha256.
            info.Delete();
            TryDeleteFile(zipPath + ".sha256");
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("MasterDataSyncService.IsTodayZipValid", ex);
        }
        return false;
    }

    /// <summary>
    /// Xóa zip cùng prefix (cùng terminal) có tên ≠ file hôm nay → folder chỉ còn 1 file/ngày (R1).
    /// Thêm: xóa file cũ hơn KeepZipDays (lưới an toàn cho file mồ côi).
    /// </summary>
    private void CleanupSiblingZips(GetMasterDataFileRequest req, string currentZipName)
    {
        try
        {
            var prefix = $"{req.SiteCode}_{req.TypeSync}_{req.PosTerminal}_";
            foreach (var file in Directory.GetFiles(req.TargetDir, $"{prefix}*.zip"))
            {
                var name = Path.GetFileName(file);
                if (string.Equals(name, currentZipName, StringComparison.OrdinalIgnoreCase))
                    continue;

                var isOld = (DateTime.Now - new FileInfo(file).LastWriteTime).TotalDays > _opt.KeepZipDays;
                // Khác tên hôm nay → xóa luôn (tránh POS nhận file cũ); hoặc quá hạn KeepZipDays.
                if (!IsCurrentDateName(name) || isOld)
                {
                    TryDeleteFile(file);
                    TryDeleteFile(file + ".sha256");
                }
            }
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("MasterDataSyncService.CleanupSiblingZips", ex);
        }
    }

    /// <summary>Tính SHA-256 hex của file (lowercase). Dùng BCL SHA256.HashDataAsync — không cần nuget.</summary>
    private static async Task<string> ComputeSha256HexAsync(string filePath, CancellationToken ct)
    {
        await using var fs = File.OpenRead(filePath);
        var bytes = await SHA256.HashDataAsync(fs, ct);
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private bool IsCurrentDateName(string fileName)
        => _opt.DateInZipName && fileName.Contains($"_{DateTime.Now:yyyyMMdd}.zip", StringComparison.OrdinalIgnoreCase);

    private static GetMasterDataFileResult Success(string zipName, GetMasterDataFileRequest req, int tableCount = 0)
        => new()
        {
            Success = true,
            FileName = zipName,
            RelativePath = $"{req.PathSync}/{req.FolderFile}/{zipName}",
            TableCount = tableCount,
            Message = "OK"
        };

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("MasterDataSyncService.TryDeleteFile", ex);
        }
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
        catch (Exception ex)
        {
            fileLogHelper.WriteExpLogs("MasterDataSyncService.TryDeleteDirectory", ex);
        }
    }
}
