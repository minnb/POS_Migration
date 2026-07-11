using System.Data;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using POS.Common.Dtos.DataSync;
using POS.Infrastructure.Database;
using POS.Infrastructure.Files;
using POS.Infrastructure.Redis;

namespace POS.Infrastructure.Repositories;

/// <inheritdoc cref="POS.Infrastructure.Repositories.Interfaces.ISyncRepository"/>
public sealed class SyncRepository(
    CentralMDConnectionFactory connectionFactory,
    IOptions<MasterDataSyncOptions> options,
    IRedisService redis)
    : BaseRepository(connectionFactory), Interfaces.ISyncRepository
{
    private readonly MasterDataSyncOptions _opt = options.Value;

    // Redis key cho danh sách bảng master data (metadata nhỏ, thay đổi khi DBA đổi SyncTableList).
    // TTL 1h: đủ dài để tránh SP1 lặp lại mỗi request; đủ ngắn để nhận cập nhật cấu hình trong ngày.
    // Tách theo isChange ("A"/"W") vì Action trả về khác nhau giữa 2 nhánh SP.
    private const string SyncTableListKey = "MD:SyncTableList";
    private const int SyncTableListTtlSeconds = 3600;

    public async Task<List<SyncTableInfo>> GetSyncTablesAsync(string isChange = "A", CancellationToken ct = default)
    {
        // Cache SP1 metadata trong Redis — dữ liệu nhỏ (~85 record), hiếm thay đổi.
        // Invalidate thủ công: DEL MD:SyncTableList:A / DEL MD:SyncTableList:W khi DBA cập nhật SyncTableList.
        var cacheKey = $"{SyncTableListKey}:{isChange}";
        var cached = await redis.StringGetAsync<List<SyncTableInfo>>(cacheKey);
        if (cached?.Count > 0) return cached;

        // @IsChange='A' (ALL sync) hoặc 'W' (Web Sync/push 1 POS) → bỏ qua @IsByStore/@GroupName (dùng default của SP).
        var rows = await QueryAsync<SyncTableInfo>(
            "[dbo].[SyncTable_Get] @IsChange",
            new { IsChange = isChange },
            commandTimeout: _opt.SqlCommandTimeoutSeconds,
            ct: ct);
        var list = rows.ToList();
        if (list.Count > 0)
            redis.StringSet(cacheKey, list, ttlSeconds: SyncTableListTtlSeconds);
        return list;
    }

    public async Task<List<SyncTableInfo>> GetSyncTableCountersAsync(string isChange = "A", CancellationToken ct = default)
    {
        // KHÔNG cache — dùng cho worker cần POSLastCounter mới nhất để phát hiện thay đổi ngay lập tức.
        // GetSyncTablesAsync (có cache 1h) vẫn giữ nguyên cho luồng generation hiện hữu, không đổi.
        var rows = await QueryAsync<SyncTableInfo>(
            "[dbo].[SyncTable_Get] @IsChange",
            new { IsChange = isChange },
            commandTimeout: _opt.SqlCommandTimeoutSeconds,
            ct: ct);
        return rows.ToList();
    }

    public async Task<(int FileCount, long RowCount)> StreamTableToFilesAsync(
        SyncTableInfo table, string targetDir,
        Func<int, string> fileNameFactory, Func<int, string> actionFactory,
        string processId, long posLastCounter, int batchSize,
        string filterColumn, string filterValue, CancellationToken ct = default)
    {
        long totalRows = 0;
        var batchNo = 0;
        var rowsInBatch = 0;

        await using var conn = (SqlConnection)await _connectionFactory.CreateOpenConnectionAsync(ct);
        await using var cmd = new SqlCommand("[dbo].[SyncGetDataByTable]", conn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = _opt.SqlCommandTimeoutSeconds
        };
        // LƯU Ý: OrderByName trong SyncTableList là SỐ THỨ TỰ NẠP BẢNG (SP1 ORDER BY OrderByName),
        // KHÔNG phải cột sắp xếp dòng. SP2 lại ghép "ORDER BY <@ColumnOrderBy>" → "ORDER BY 11" (positional)
        // gây lỗi với bảng < 11 cột. TRUNC-INSERT không cần thứ tự dòng → truyền '' để SP2 bỏ ORDER BY
        // (rẽ nhánh IF @ColumnOrderBy='' → WHERE [Counter] > 0 OR 0=0 → trả mọi dòng).
        // PHẢI là chuỗi rỗng "" (KHÔNG DBNull): NULL='' là false → SP rơi vào ELSE ghép ORDER BY NULL → @Query NULL.
        // Guard phòng thủ: chỉ giữ giá trị nếu là tên cột phi-số (dữ liệu hiện tại 100% là số → luôn "").
        var orderBy = table.OrderByName?.Trim() ?? string.Empty;
        var columnOrderBy = orderBy.Length > 0 && !orderBy.All(char.IsDigit) ? orderBy : string.Empty;

        cmd.Parameters.AddWithValue("@TableName", (object?)table.TableName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@ColumnOrderBy", columnOrderBy);
        cmd.Parameters.AddWithValue("@POSLastCounter", posLastCounter);
        // Filter per-store (bảng IsByStore=1): SP2 ghép "AND [@FilterColumn] = @pFilterValue" khi cả 2 khác rỗng.
        // Rỗng → SP bỏ qua filter (bảng global). Giá trị parameterized trong SP → an toàn injection.
        cmd.Parameters.AddWithValue("@FilterColumn", filterColumn ?? string.Empty);
        cmd.Parameters.AddWithValue("@FilterValue", filterValue ?? string.Empty);

        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess, ct);
        var fieldCount = reader.FieldCount;

        FileStream? fs = null;
        StreamWriter? sw = null;
        JsonTextWriter? jw = null;

        // Mở 1 file batch mới: ghi phần header envelope SyncTableList tới mảng "Data".
        async Task OpenBatchAsync()
        {
            batchNo++;
            var fileName = fileNameFactory(batchNo);
            var action = actionFactory(batchNo);
            var path = Path.Combine(targetDir, fileName);

            // Encoding.UTF8 + StreamWriter khớp convention CreateFileSODFakeAsync (có BOM).
            fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 65536, useAsync: true);
            sw = new StreamWriter(fs, Encoding.UTF8);
            jw = new JsonTextWriter(sw) { CloseOutput = false };

            // Envelope: { FileName, TableName, Action, ProcedureName, ProcessID, Data:[...] }
            await jw.WriteStartObjectAsync(ct);
            await jw.WritePropertyNameAsync("FileName", ct);
            await jw.WriteValueAsync(fileName, ct);
            await jw.WritePropertyNameAsync("TableName", ct);
            await jw.WriteValueAsync(table.TableName, ct);
            await jw.WritePropertyNameAsync("Action", ct);
            await jw.WriteValueAsync(action, ct);
            await jw.WritePropertyNameAsync("ProcedureName", ct);
            await jw.WriteValueAsync(table.Procedure ?? string.Empty, ct);
            await jw.WritePropertyNameAsync("ProcessID", ct);
            await jw.WriteValueAsync(processId, ct);
            await jw.WritePropertyNameAsync("Data", ct);
            await jw.WriteStartArrayAsync(ct);
            rowsInBatch = 0;
        }

        // Đóng mảng "Data" + object rồi flush/đóng file hiện tại.
        async Task CloseBatchAsync()
        {
            if (jw == null) return;
            await jw.WriteEndArrayAsync(ct);
            await jw.WriteEndObjectAsync(ct);
            await jw.FlushAsync(ct);
            jw.Close();
            if (sw != null) await sw.DisposeAsync();
            if (fs != null) await fs.DisposeAsync();
            jw = null; sw = null; fs = null;
        }

        try
        {
            // Lazy open: chỉ tạo file khi SP2 trả ít nhất 1 dòng — bảng rỗng không tạo file (trả (0,0)).
            // Lý do: eager open ghi TRUNC-INSERT vào header trước khi biết có dữ liệu không → POS nhận
            // TRUNC-INSERT + Data:[] → truncate bảng nội bộ dù không có dòng nào → mất data POS.
            // SequentialAccess: phải đọc cột theo thứ tự ordinal tăng dần — vòng lặp 0..n-1 thỏa điều kiện.
            while (await reader.ReadAsync(ct))
            {
                if (jw == null)
                {
                    // Row đầu tiên → mở batch 1
                    await OpenBatchAsync();
                }
                else if (batchSize > 0 && rowsInBatch >= batchSize)
                {
                    await CloseBatchAsync();
                    await OpenBatchAsync();
                }

                await jw!.WriteStartObjectAsync(ct);
                for (var i = 0; i < fieldCount; i++)
                {
                    await jw.WritePropertyNameAsync(reader.GetName(i), ct);
                    if (await reader.IsDBNullAsync(i, ct))
                        await jw.WriteNullAsync(ct);
                    else
                        await jw.WriteValueAsync(reader.GetValue(i), ct);
                }
                await jw.WriteEndObjectAsync(ct);
                rowsInBatch++;
                totalRows++;
            }

            if (jw != null)
                await CloseBatchAsync();
            // jw == null → SP2 trả 0 dòng → không tạo file → return (0, 0)
        }
        catch
        {
            // Lỗi giữa chừng: đóng writer đang mở (file dở sẽ bị xóa cùng tmpDir ở service).
            try { jw?.Close(); } catch { /* ignore */ }
            if (sw != null) await sw.DisposeAsync();
            if (fs != null) await fs.DisposeAsync();
            throw;
        }

        return (batchNo, totalRows);
    }

    public Task InsertDownloadLogAsync(
        string? siteCode, string? posTerminal, string? fileName, string? filePath,
        long fileSizeBytes, long durationMs, string status, string? clientIp, CancellationToken ct = default)
    {
        const string sql = @"
INSERT INTO dbo.MasterDataDownloadLog
    (SiteCode, PosTerminal, FileName, FilePath, FileSizeBytes, DurationMs, Status, ClientIp, DownloadedAt)
VALUES
    (@SiteCode, @PosTerminal, @FileName, @FilePath, @FileSizeBytes, @DurationMs, @Status, @ClientIp, @DownloadedAt)";
        return ExecuteAsync(sql, new
        {
            SiteCode = siteCode,
            PosTerminal = posTerminal,
            FileName = fileName,
            FilePath = filePath,
            FileSizeBytes = fileSizeBytes,
            DurationMs = durationMs,
            Status = status,
            ClientIp = clientIp,
            DownloadedAt = DateTime.Now
        }, ct: ct);
    }

    public async Task<bool> UpdateDeleteLogAsync(
        string? siteCode, string? posTerminal, string? fileName,
        string deleteStatus, DateTime deletedAt, CancellationToken ct = default)
    {
        const string sql = @"
UPDATE t
SET t.DeletedAt = @DeletedAt, t.DeleteStatus = @DeleteStatus
FROM dbo.MasterDataDownloadLog t
INNER JOIN (
    SELECT TOP (1) Id
    FROM dbo.MasterDataDownloadLog
    WHERE FileName = @FileName
      AND (@SiteCode IS NULL OR SiteCode = @SiteCode)
      AND (@PosTerminal IS NULL OR PosTerminal = @PosTerminal)
    ORDER BY DownloadedAt DESC
) x ON t.Id = x.Id";

        var rows = await ExecuteAsync(sql, new
        {
            SiteCode = siteCode,
            PosTerminal = posTerminal,
            FileName = fileName,
            DeleteStatus = deleteStatus,
            DeletedAt = deletedAt
        }, ct: ct);

        return rows > 0;
    }

    public async Task AckZipWatermarkAsync(
        IReadOnlyDictionary<string, long> snapshotCounters, CancellationToken ct = default)
    {
        if (snapshotCounters.Count == 0) return;

        var table = new DataTable();
        table.Columns.Add("TableName", typeof(string));
        table.Columns.Add("Counter", typeof(long));
        foreach (var (tableName, counter) in snapshotCounters)
            table.Rows.Add(tableName, counter);

        var p = new DynamicParameters();
        p.Add("@Counters", table.AsTableValuedParameter("dbo.TVP_ZipWatermarkUpdate"));

        using var conn = await _connectionFactory.CreateOpenConnectionAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("dbo.usp_SyncTableList_BulkUpdateZipWatermark", p,
            commandType: CommandType.StoredProcedure, commandTimeout: 30, cancellationToken: ct));
    }
}
