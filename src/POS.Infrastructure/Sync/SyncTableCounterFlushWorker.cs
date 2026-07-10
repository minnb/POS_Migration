using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Sync;

/// <summary>
/// Drain <see cref="SyncTableTrackerService"/>.Reader theo chu kỳ, coalesce (giữ Max mỗi bảng),
/// batch-update SyncTableList.POSLastCounter. Đăng ký AddHostedService trực tiếp ở POS.Api/Program.cs
/// và POS.Web/Program.cs (KHÔNG qua AddInfrastructure/WorkerRolesOptions — Channel chỉ sống trong
/// đúng tiến trình ghi dữ liệu, xem .claude/rules/masterdata-sync.md mục "Cập nhật POSLastCounter").
/// </summary>
public sealed class SyncTableCounterFlushWorker(
    SyncTableTrackerService tracker,
    IServiceScopeFactory scopeFactory,
    IRedisService redis,
    IOptions<SyncTableTrackerOptions> options,
    ILogger<SyncTableCounterFlushWorker> logger) : BackgroundService
{
    private const string HeartbeatKeyPrefix = "Worker:Heartbeat:SyncTableCounterFlush-";
    private const int NormalTtlMultiplier = 3;
    private const int StoppedTtlSeconds = 300;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, options.Value.FlushIntervalSeconds));
        var heartbeatKey = HeartbeatKeyPrefix + AppDomain.CurrentDomain.FriendlyName;
        var processed = 0L;

        logger.LogInformation("[SyncTableCounterFlush] Started — interval {Sec}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                var flushed = await FlushOnceAsync(stoppingToken);
                if (flushed > 0) processed += flushed;
                WriteHeartbeat(heartbeatKey, "Running", processed, (int)(interval.TotalSeconds * NormalTtlMultiplier));
            }
        }
        catch (OperationCanceledException) { }
        finally
        {
            WriteHeartbeat(heartbeatKey, "Stopped", processed, StoppedTtlSeconds);
            logger.LogInformation("[SyncTableCounterFlush] Stopped");
        }
    }

    private async Task<int> FlushOnceAsync(CancellationToken ct)
    {
        var reader = tracker.Reader;
        Dictionary<string, long>? batch = null;
        while (reader.TryRead(out var evt))
        {
            batch ??= [];
            if (!batch.TryGetValue(evt.TableName, out var existing) || evt.Counter > existing)
                batch[evt.TableName] = evt.Counter;
        }

        if (batch is null || batch.Count == 0) return 0;

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ISyncTrackerRepository>();
            await repo.BulkUpdateCounterAsync(batch, ct);
            logger.LogInformation("[SyncTableCounterFlush] Updated {Count} table(s): {Tables}",
                batch.Count, string.Join(", ", batch.Keys));
            return batch.Count;
        }
        catch (Exception ex)
        {
            // KHÔNG để 1 lần flush lỗi làm chết worker — log rồi thử lại ở tick kế tiếp.
            logger.LogError(ex, "[SyncTableCounterFlush] Flush failed for {Count} table(s)", batch.Count);
            return 0;
        }
    }

    private void WriteHeartbeat(string key, string status, long processedCount, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName = "SyncTableCounterFlush",
                Status = status,
                InstanceId = Environment.MachineName,
                LastBeatUtc = DateTime.UtcNow,
                QueueName = string.Empty,
                ProcessedCount = processedCount,
            };
            redis.StringSet(key, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            // Heartbeat KHÔNG được crash worker — nuốt exception, chỉ log.
            logger.LogWarning("[SyncTableCounterFlush] Heartbeat write failed — {Msg}", ex.Message);
        }
    }
}
