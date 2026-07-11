using POS.Common.Dtos.DataSync;

namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Truy vấn master data từ CentralMD phục vụ sinh file sync cho POS.
/// </summary>
public interface ISyncRepository
{
    /// <summary>
    /// SP1 [SyncTable_Get] — danh sách bảng cần sync. <paramref name="isChange"/>: "A" (mặc định,
    /// ALL sync) hoặc "W" (Web Sync/push 1 POS — Action trả về luôn "DELETE-INSERT").
    /// </summary>
    Task<List<SyncTableInfo>> GetSyncTablesAsync(string isChange = "A", CancellationToken ct = default);

    /// <summary>
    /// SP1 [SyncTable_Get] — KHÔNG qua Redis cache (khác <see cref="GetSyncTablesAsync"/>), dùng cho
    /// worker cần đọc POSLastCounter mới nhất ngay lập tức để phát hiện thay đổi (MasterDataZipGeneratorWorker,
    /// isChange="C"). Cache 1h của GetSyncTablesAsync sẽ làm việc phát hiện thay đổi bị trễ tới 1 giờ.
    /// </summary>
    Task<List<SyncTableInfo>> GetSyncTableCountersAsync(string isChange = "A", CancellationToken ct = default);

    /// <summary>
    /// SP2 [SyncGetDataByTable] — STREAM SqlDataReader (SequentialAccess) ghi ra một hoặc nhiều file .txt
    /// dạng JSON envelope SyncTableList { ..., Data:[rows] }. KHÔNG nạp DataTable/RAM.
    /// Cứ <paramref name="batchSize"/> dòng tách 1 file mới (batchSize &lt;= 0 → không tách, 1 file).
    /// <paramref name="fileNameFactory"/>(batchNo) trả tên file cho batch (batchNo bắt đầu từ 1);
    /// <paramref name="actionFactory"/>(batchNo) trả Action ghi vào envelope của batch đó.
    /// <paramref name="filterColumn"/>/<paramref name="filterValue"/>: lọc per-store cho bảng IsByStore=1
    /// (WHERE [filterColumn] = filterValue); rỗng → không lọc (bảng global). File ghi vào <paramref name="targetDir"/>.
    /// Trả về (số file, tổng số dòng).
    /// </summary>
    Task<(int FileCount, long RowCount)> StreamTableToFilesAsync(
        SyncTableInfo table, string targetDir,
        Func<int, string> fileNameFactory, Func<int, string> actionFactory,
        string processId, long posLastCounter, int batchSize,
        string filterColumn, string filterValue, CancellationToken ct = default);

    /// <summary>
    /// Ghi 1 dòng log POS tải file vào bảng dbo.MasterDataDownloadLog. (Service gọi bọc try/catch fail-safe.)
    /// </summary>
    Task InsertDownloadLogAsync(
        string? siteCode, string? posTerminal, string? fileName, string? filePath,
        long fileSizeBytes, long durationMs, string status, string? clientIp, CancellationToken ct = default);

    /// <summary>
    /// Cập nhật DeletedAt/DeleteStatus cho bản ghi dbo.MasterDataDownloadLog mới nhất khớp FileName
    /// (+SiteCode/PosTerminal nếu có), gọi sau khi POS xóa file qua DeleteFileFromFTP. Fail-safe: service
    /// gọi bọc try/catch. Trả về true nếu tìm thấy và cập nhật được 1 dòng.
    /// </summary>
    Task<bool> UpdateDeleteLogAsync(
        string? siteCode, string? posTerminal, string? fileName,
        string deleteStatus, DateTime deletedAt, CancellationToken ct = default);

    /// <summary>
    /// ACK watermark của MasterDataZipGeneratorWorker — batch update SyncTableList.ZipWatermarkCounter
    /// qua TVP dbo.TVP_ZipWatermarkUpdate → SP dbo.usp_SyncTableList_BulkUpdateZipWatermark (idempotent,
    /// chỉ ghi đè khi Counter &gt; ZipWatermarkCounter hiện có). <paramref name="snapshotCounters"/>
    /// PHẢI là giá trị POSLastCounter đã đọc lúc đầu cycle (KHÔNG re-read DB tại thời điểm gọi) —
    /// xem docs/sql/SyncTableList_AddZipWatermark.sql.
    /// </summary>
    Task AckZipWatermarkAsync(IReadOnlyDictionary<string, long> snapshotCounters, CancellationToken ct = default);
}
