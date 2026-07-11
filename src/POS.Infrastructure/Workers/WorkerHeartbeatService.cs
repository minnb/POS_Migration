using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Redis;

namespace POS.Infrastructure.Workers;

public sealed class WorkerHeartbeatService(
    IRedisService redis,
    WorkerHealthState healthState,
    IOptions<WorkerHeartbeatOptions> options,
    ILogger<WorkerHeartbeatService> logger
) : BackgroundService
{
    private readonly WorkerHeartbeatOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var redisKey = $"Worker:Heartbeat:{_options.WorkerName}";
        logger.LogInformation("[WorkerHeartbeat] Started — interval {Sec}s, key {Key}", _options.IntervalSeconds, redisKey);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_options.IntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                WriteHeartbeat(redisKey, healthState.Status, _options.NormalTtlSeconds);
        }
        catch (OperationCanceledException) { }
        finally
        {
            // Ghi nhịp cuối — báo hiệu worker đã dừng có chủ ý
            try { WriteHeartbeat(redisKey, "Stopped", _options.StoppedTtlSeconds); }
            catch { }
            logger.LogInformation("[WorkerHeartbeat] Stopped");
        }
    }

    private void WriteHeartbeat(string redisKey, string status, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName     = _options.WorkerName,
                Status         = status,
                InstanceId     = Environment.MachineName,
                LastBeatUtc    = DateTime.UtcNow,
                QueueName      = _options.QueueName,
                ProcessedCount = healthState.ProcessedCount,
            };
            redis.StringSet(redisKey, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            // Heartbeat KHÔNG được crash worker — nuốt exception, chỉ log
            logger.LogWarning("[WorkerHeartbeat] Failed to write — {Msg}", ex.Message);
        }
    }
}
