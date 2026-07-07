using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Files;
using POS.Infrastructure.Redis;

namespace POS.Infrastructure.Workers;

/// <summary>
/// Vòng lặp liên tục cho <see cref="PosFileImportService"/> — dùng khi chạy dạng hosted-service dài
/// hạn (dev/Model B nếu bật lại toggle <c>WorkerRoles:EnableFileProcessing</c>). Chế độ cronjob thật
/// (<c>Program.cs --run-once</c>, Model A) gọi trực tiếp <see cref="PosFileImportService.RunOnceAsync"/>,
/// không qua class này.
/// </summary>
public sealed class PosFileImportWorker(
    PosFileImportService fileImportService,
    IOptions<FileImportOptions> options,
    IRedisService redis,
    ILogger<PosFileImportWorker> logger
) : BackgroundService
{
    private const string RedisKey = "Worker:Heartbeat:PosFileImport";

    private readonly FileImportOptions _opt = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(1, _opt.PollIntervalSeconds));
        var ttl = Math.Max(60, _opt.PollIntervalSeconds * 3);
        logger.LogInformation("[PosFileImport] Started — inbox '{Inbox}', interval {Sec}s",
            _opt.InboxFolder, _opt.PollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var status = "Running";
            try
            {
                await fileImportService.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                status = "Degraded";
                logger.LogError(ex, "[PosFileImport] Scan cycle failed — continue");
            }

            WriteHeartbeat(status, ttl);

            try { await Task.Delay(interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }

        WriteHeartbeat("Stopped", 300);
        logger.LogInformation("[PosFileImport] Stopped");
    }

    private void WriteHeartbeat(string status, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName     = "PosFileImport",
                Status         = status,
                InstanceId     = Environment.MachineName,
                LastBeatUtc    = DateTime.UtcNow,
                QueueName      = "(file)",
                ProcessedCount = fileImportService.ProcessedCount,
            };
            redis.StringSet(RedisKey, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            logger.LogWarning("[PosFileImport] Failed to write heartbeat — {Msg}", ex.Message);
        }
    }
}
