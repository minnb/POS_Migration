namespace POS.Infrastructure.Workers;

/// <summary>
/// Singleton chia sẻ trạng thái giữa PosSalesConsumerWorker (writer) và WorkerHeartbeatService (reader).
/// </summary>
public sealed class WorkerHealthState
{
    private long _processedCount;

    // string reference assignment là atomic trong .NET — không cần lock
    public string Status { get; set; } = "Running";

    public long ProcessedCount => Interlocked.Read(ref _processedCount);

    public void IncrementProcessed() => Interlocked.Increment(ref _processedCount);
}
