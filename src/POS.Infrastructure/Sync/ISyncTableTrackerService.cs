namespace POS.Infrastructure.Sync;

/// <summary>
/// Ghi nhận (bất đồng bộ, non-blocking) rằng 1 bảng master data vừa được bump cột Counter tới
/// giá trị nào — dùng để <see cref="SyncTableCounterFlushWorker"/> batch-update
/// SyncTableList.POSLastCounter mà không chặn hot path CRUD.
/// </summary>
public interface ISyncTableTrackerService
{
    /// <summary>
    /// Ghi 1 event (tableName, counter) vào Channel nội bộ. Không async, không throw — nếu Channel
    /// đầy, event cũ nhất bị drop (chấp nhận được, xem <see cref="SyncTableTrackerOptions"/>).
    /// </summary>
    void Track(string tableName, long counter);
}
