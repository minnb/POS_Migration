namespace POS.Infrastructure.Workers;

/// <summary>
/// Cấu hình <see cref="WorkerHeartbeatService"/>. Bind section "WorkerHeartbeat" trong appsettings.
/// </summary>
public sealed class WorkerHeartbeatOptions
{
    public const string SectionName = "WorkerHeartbeat";

    /// <summary>Tên worker ghi vào Redis key "Worker:Heartbeat:{WorkerName}" — HealthCheckService đọc theo tên này.</summary>
    public string WorkerName { get; set; } = "PosSalesConsumer";

    /// <summary>Tên queue hiển thị trên trang /ops/health, không ảnh hưởng logic consume thật.</summary>
    public string QueueName { get; set; } = "pos_sales";

    /// <summary>Chu kỳ ghi heartbeat — giây.</summary>
    public int IntervalSeconds { get; set; } = 15;

    /// <summary>TTL Redis khi worker đang chạy bình thường — giây (~4× IntervalSeconds).</summary>
    public int NormalTtlSeconds { get; set; } = 60;

    /// <summary>TTL Redis khi worker dừng có chủ ý — giây (đủ để ops team thấy).</summary>
    public int StoppedTtlSeconds { get; set; } = 300;
}
