namespace POS.Infrastructure.Repositories.Interfaces;

/// <summary>
/// Batch-update SyncTableList.POSLastCounter — gọi bởi SyncTableCounterFlushWorker, KHÔNG bao giờ
/// gọi trực tiếp trên hot path CRUD.
/// </summary>
public interface ISyncTrackerRepository
{
    /// <summary>
    /// Cập nhật POSLastCounter cho nhiều bảng cùng lúc qua TVP. Idempotent — SP chỉ ghi đè khi
    /// giá trị mới lớn hơn giá trị hiện có (WHERE Counter > POSLastCounter).
    /// </summary>
    Task BulkUpdateCounterAsync(IReadOnlyDictionary<string, long> counters, CancellationToken ct = default);
}
