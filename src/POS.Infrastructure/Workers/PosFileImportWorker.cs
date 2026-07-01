using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.Ops;
using POS.Common.Helpers;
using POS.Infrastructure.Files;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Workers;

/// <summary>
/// Đường nạp sale bằng file (song song với <see cref="PosSalesConsumerWorker"/> đọc RabbitMQ).
/// Quét InboxFolder → gặp .zip thì giải nén → mỗi .txt là 1 <see cref="KafkaMessageDto"/> →
/// insert DB qua đúng luồng <see cref="ICentralSaleRepository.InInsertToTableByJson"/>.
/// Thành công → xóa zip; có bất kỳ record lỗi → move zip sang ErrorFolder.
/// </summary>
public sealed class PosFileImportWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<FileImportOptions> options,
    IFileArchiveService archive,
    IRedisService redis,
    ILogger<PosFileImportWorker> logger
) : BackgroundService
{
    private const string RedisKey = "Worker:Heartbeat:PosFileImport";

    private readonly FileImportOptions _opt = options.Value;
    private long _processedCount;
    private bool _warnedDisabled;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _opt.PollIntervalSeconds));
        var ttl = Math.Max(60, _opt.PollIntervalSeconds * 3);
        logger.LogInformation("[PosFileImport] Started — inbox '{Inbox}', interval {Sec}s",
            _opt.InboxFolder, _opt.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var status = "Running";
            try
            {
                if (!_opt.Enabled || string.IsNullOrWhiteSpace(_opt.InboxFolder))
                {
                    if (!_warnedDisabled)
                    {
                        logger.LogWarning("[PosFileImport] Disabled or InboxFolder empty — idle");
                        _warnedDisabled = true;
                    }
                }
                else
                {
                    await ScanOnceAsync(stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                status = "Degraded";
                logger.LogError(ex, "[PosFileImport] Scan cycle failed — continue");
            }

            WriteHeartbeat(status, ttl);

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        WriteHeartbeat("Stopped", 300);
        logger.LogInformation("[PosFileImport] Stopped");
    }

    /// <summary>Quét InboxFolder một lượt, xử lý tối đa MaxFilesPerCycle file .zip đã ổn định.</summary>
    private async Task ScanOnceAsync(CancellationToken ct)
    {
        Directory.CreateDirectory(_opt.InboxFolder);
        if (!string.IsNullOrWhiteSpace(_opt.ErrorFolder)) Directory.CreateDirectory(_opt.ErrorFolder);
        var workRoot = string.IsNullOrWhiteSpace(_opt.WorkFolder)
            ? Path.Combine(_opt.InboxFolder, "_work")
            : _opt.WorkFolder;
        Directory.CreateDirectory(workRoot);

        var stableBefore = DateTime.UtcNow.AddSeconds(-Math.Max(0, _opt.StableSeconds));
        var zips = new DirectoryInfo(_opt.InboxFolder)
            .EnumerateFiles(_opt.FileFilter, SearchOption.TopDirectoryOnly)
            .Where(f => f.LastWriteTimeUtc <= stableBefore)
            .OrderBy(f => f.LastWriteTimeUtc)
            .Take(Math.Max(1, _opt.MaxFilesPerCycle))
            .ToList();

        foreach (var zip in zips)
        {
            if (ct.IsCancellationRequested) break;
            await ProcessZipAsync(zip.FullName, workRoot, ct);
        }
    }

    /// <summary>Claim (move) → giải nén → insert từng .txt → xóa/move theo kết quả.</summary>
    private async Task ProcessZipAsync(string inboxZipPath, string workRoot, CancellationToken ct)
    {
        var workDir = Path.Combine(workRoot, Guid.NewGuid().ToString("N"));
        var zipName = Path.GetFileName(inboxZipPath);
        var claimedZip = Path.Combine(workDir, zipName);

        // 1. CLAIM: rút zip khỏi inbox trước khi xử lý (chống 2 instance giành nhau, tránh quét lại giữa chừng).
        try
        {
            Directory.CreateDirectory(workDir);
            File.Move(inboxZipPath, claimedZip);
        }
        catch (Exception ex)
        {
            // File đang bị lock (còn ghi dở) hoặc instance khác đã lấy → bỏ qua vòng này.
            logger.LogWarning("[PosFileImport] Cannot claim '{Zip}' — skip: {Msg}", zipName, ex.Message);
            TryDeleteDir(workDir);
            return;
        }

        var extractDir = Path.Combine(workDir, "extracted");
        var anyFailed = false;
        try
        {
            archive.ExtractToDirectory(claimedZip, extractDir);

            var txtFiles = Directory.EnumerateFiles(extractDir, "*.txt", SearchOption.AllDirectories).ToList();
            if (txtFiles.Count == 0)
            {
                // Lộ tên các file thực có để phát hiện sai extension/định dạng.
                var actual = Directory.EnumerateFiles(extractDir, "*", SearchOption.AllDirectories)
                    .Select(Path.GetFileName).Take(20).ToList();
                logger.LogWarning("[PosFileImport] '{Zip}' khong co .txt — treat as error. File thuc co: {Files}",
                    zipName, actual.Count == 0 ? "(rong)" : string.Join(", ", actual));
                anyFailed = true;
            }
            else
            {
                logger.LogInformation("[PosFileImport] '{Zip}' — {Count} file .txt", zipName, txtFiles.Count);
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICentralSaleRepository>();

            foreach (var txt in txtFiles)
            {
                if (ct.IsCancellationRequested) break;
                var ok = await ProcessTxtAsync(repo, txt, ct);
                if (!ok) anyFailed = true;
            }
        }
        catch (Exception ex)
        {
            anyFailed = true;
            logger.LogError(ex, "[PosFileImport] Failed processing '{Zip}'", zipName);
        }

        // 4. Kết luận zip: lỗi → move sang error; ok → xóa. Luôn cleanup workDir.
        if (anyFailed)
            MoveToError(claimedZip, zipName);
        else
            TryDeleteFile(claimedZip);

        TryDeleteDir(workDir);
    }

    /// <summary>
    /// Xử lý 1 file .txt. Tên file: <c>Type_PosNo_TransactionId.txt</c> → cung cấp định danh
    /// (storeNo = Left(PosNo,4), posNo, transactionId). NỘI DUNG file = message <c>{Type, Data}</c>
    /// truyền nguyên cho SP. Trả về true nếu insert OK.
    /// </summary>
    private async Task<bool> ProcessTxtAsync(ICentralSaleRepository repo, string txtPath, CancellationToken ct)
    {
        var name = Path.GetFileName(txtPath);
        try
        {
            var stem = Path.GetFileNameWithoutExtension(txtPath);
            if (!TryParseFileName(stem, out var typeName, out var posNo, out var transactionId))
            {
                logger.LogWarning("[PosFileImport] Ten file '{Txt}' sai dinh dang Type_PosNo_TransactionId — skip", name);
                return false;
            }
            var storeNo = StringHelper.Left(posNo, 4);

            var body = await File.ReadAllTextAsync(txtPath, ct);
            if (string.IsNullOrWhiteSpace(body))
            {
                logger.LogWarning("[PosFileImport] File rong '{Txt}' — skip", name);
                return false;
            }

            logger.LogInformation(
                "[PosFileImport] Processing '{Txt}' — type={Type} store={Store} pos={Pos} txId={TxId} {Len}B",
                name, typeName, storeNo, posNo, transactionId, body.Length);

            var result = await repo.InInsertToTableByJson(storeNo, posNo, transactionId, body, _opt.Source, ct);

            if (result.Item1)
            {
                Interlocked.Increment(ref _processedCount);
                logger.LogInformation("[PosFileImport] Inserted OK — txId: {TxId} ({Txt})", transactionId, name);
                return true;
            }

            logger.LogWarning("[PosFileImport] Insert failed — txId: {TxId}, reason: {Reason} ({Txt})",
                transactionId, result.Item2, name);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PosFileImport] Error processing '{Txt}'", name);
            return false;
        }
    }

    /// <summary>
    /// Parse tên file (đã bỏ .txt) dạng <c>Type_PosNo_TransactionId</c>. TransactionId có thể chứa
    /// '_' → phần thứ 3 gộp toàn bộ phần còn lại. Trả về false nếu thiếu phần / PosNo/TxId rỗng.
    /// </summary>
    public static bool TryParseFileName(string stem, out string type, out string posNo, out string transactionId)
    {
        type = posNo = transactionId = "";
        if (string.IsNullOrWhiteSpace(stem)) return false;
        var p = stem.Split('_', 3);
        if (p.Length < 3) return false;
        type = p[0];
        posNo = p[1];
        transactionId = p[2];
        return posNo.Length > 0 && transactionId.Length > 0;
    }

    private void MoveToError(string zipPath, string zipName)
    {
        if (string.IsNullOrWhiteSpace(_opt.ErrorFolder))
        {
            logger.LogWarning("[PosFileImport] ErrorFolder not configured — dropping failed '{Zip}'", zipName);
            TryDeleteFile(zipPath);
            return;
        }
        try
        {
            var stem = Path.GetFileNameWithoutExtension(zipName);
            var ext = Path.GetExtension(zipName);
            // Tên unique để không đè zip lỗi trước đó (dùng tick thay Guid cho dễ đọc thứ tự).
            var dest = Path.Combine(_opt.ErrorFolder, $"{stem}_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}{ext}");
            File.Move(zipPath, dest);
            logger.LogWarning("[PosFileImport] Moved failed zip to '{Dest}'", dest);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PosFileImport] Cannot move '{Zip}' to error", zipName);
        }
    }

    private void WriteHeartbeat(string status, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName     = "PosFileImport",
                Status         = status,
                InstanceId     = Environment.MachineName,
                LastBeatUtc    = DateTime.UtcNow,
                QueueName      = "(file)",
                ProcessedCount = Interlocked.Read(ref _processedCount),
            };
            redis.StringSet(RedisKey, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[PosFileImport] Failed to write heartbeat — {Msg}", ex.Message);
        }
    }

    private void TryDeleteFile(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { }
    }

    private void TryDeleteDir(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); } catch { }
    }
}
