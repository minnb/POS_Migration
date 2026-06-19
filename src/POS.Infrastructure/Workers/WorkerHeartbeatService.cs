using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Redis;

namespace POS.Infrastructure.Workers;

public sealed class WorkerHeartbeatService(
    IRedisService redis,
    WorkerHealthState healthState,
    ILogger<WorkerHeartbeatService> logger
) : BackgroundService
{
    private const string RedisKey  = "Worker:Heartbeat:PosSalesConsumer";
    private const string QueueName = "pos_sales";
    private const int    IntervalSeconds = 15;
    private const int    NormalTtl      = 60;   // ~4× interval
    private const int    StoppedTtl     = 300;  // 5 phút — đủ để ops team thấy

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[WorkerHeartbeat] Started — interval {Sec}s, key {Key}", IntervalSeconds, RedisKey);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                WriteHeartbeat(healthState.Status, NormalTtl);
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Ghi nhịp cuối — báo hiệu worker đã dừng có chủ ý
            try { WriteHeartbeat("Stopped", StoppedTtl); }
            catch { }
            logger.LogInformation("[WorkerHeartbeat] Stopped");
        }
    }

    private void WriteHeartbeat(string status, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName     = "PosSalesConsumer",
                Status         = status,
                InstanceId     = Environment.MachineName,
                LastBeatUtc    = DateTime.UtcNow,
                QueueName      = QueueName,
                ProcessedCount = healthState.ProcessedCount,
            };
            redis.StringSet(RedisKey, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            // Heartbeat KHÔNG được crash worker — nuốt exception, chỉ log
            logger.LogWarning("[WorkerHeartbeat] Failed to write — {Msg}", ex.Message);
        }
    }
}
