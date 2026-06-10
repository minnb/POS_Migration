namespace POS.Application.Interfaces;

/// <summary>
/// Pipeline xử lý file sale data từ POS → Kafka → StagingDB.
/// Migrated từ TCX.WebApiCore.DataRawService (chỉ các method SyncDataPosController dùng).
/// </summary>
public interface IDataRawService
{
    /// <summary>
    /// Tạo file SOD "fake" (chỉ chứa marker CXOfflinePercent) khi server đang quá tải queue.
    /// Returns: (Success, Message).
    /// </summary>
    Task<(bool Success, string Message)> CreateFileSODFakeAsync(string storeNo, string localPath, CancellationToken ct = default);

    /// <summary>
    /// Đọc file zip sale → push từng entry vào Kafka (topic theo StoreSetConfig) → move file sang backup.
    /// Không throw — lỗi được log (Kibana + file).
    /// </summary>
    Task ProcessFileToStagingDBAsync(string pathFile, string fileName, string? extension, string pathBackup, CancellationToken ct = default);

    /// <summary>
    /// Retry insert các DataRawJson còn treo trong Redis (key DATA_RAW:*) vào StagingDB.
    /// Returns: danh sách key đã xử lý.
    /// </summary>
    Task<List<string>> RetryInsDataRawToDBAsync(CancellationToken ct = default);
}
