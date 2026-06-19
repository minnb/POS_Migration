namespace POS.Common.Dtos.Ops;

public sealed class WorkerHeartbeat
{
    public string WorkerName { get; set; } = "";
    public string Status { get; set; } = "";        // Running | Degraded | Stopped
    public string InstanceId { get; set; } = "";    // Environment.MachineName
    public DateTime LastBeatUtc { get; set; }
    public string QueueName { get; set; } = "";
    public long ProcessedCount { get; set; }
}
