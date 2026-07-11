using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using POS.Application.Features.DataSync;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Cache;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Worker.Workers;

/// <summary>
/// Poll [SyncTable_Get] 'C' theo chu kỳ, so sánh POSLastCounter với watermark lưu trong cột DB
/// SyncTableList.ZipWatermarkCounter (bền vững — xem docs/sql/SyncTableList_AddZipWatermark.sql,
/// thay thế Redis Hash Worker:Watermark:MasterDataZip trước đây để tránh mất dấu watermark khi
/// Redis restart/evict). Bảng nào tăng counter → generate lại master data .zip cho mọi POS terminal
/// đang bật (Status=1). Quarantine pattern: terminal lỗi liên tiếp QuarantineThreshold lần bị bỏ qua
/// các lượt sau (không chặn watermark của các terminal còn lại).
/// </summary>
public sealed class MasterDataZipGeneratorWorker(
    IServiceScopeFactory scopeFactory,
    IRedisService redis,
    IOptions<MasterDataZipGeneratorOptions> options,
    ILogger<MasterDataZipGeneratorWorker> logger) : BackgroundService
{
    private const string QuarantineKey = "Worker:Quarantine:MasterDataZip";
    private const string LockKey = "Worker:Lock:MasterDataZipGenerator";
    private const string HeartbeatKey = "Worker:Heartbeat:MasterDataZipGenerator";
    private const string DetectIsChangeMode = "C";

    private readonly MasterDataZipGeneratorOptions _opt = options.Value;
    private long _processedCycles;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(30, _opt.IntervalSeconds));
        logger.LogInformation("[MasterDataZipGenerator] Started — interval {Sec}s", interval.TotalSeconds);

        using var timer = new PeriodicTimer(interval);
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunCycleAsync(stoppingToken);
        }
        catch (OperationCanceledException) { }
        finally
        {
            WriteHeartbeat("Stopped", 300);
            logger.LogInformation("[MasterDataZipGenerator] Stopped");
        }
    }

    private async Task RunCycleAsync(CancellationToken ct)
    {
        // 1 scope / tick — ISyncRepository, ISyncDataPosService, ICentralMDRepository là Scoped;
        // IRedisManager resolve qua scope cho nhất quán (bản thân là Singleton, không tốn thêm chi phí).
        await using var scope = scopeFactory.CreateAsyncScope();
        var syncRepo = scope.ServiceProvider.GetRequiredService<ISyncRepository>();
        var syncDataPosService = scope.ServiceProvider.GetRequiredService<ISyncDataPosService>();
        var mdRepo = scope.ServiceProvider.GetRequiredService<ICentralMDRepository>();
        var redisManager = scope.ServiceProvider.GetRequiredService<IRedisManager>();

        var lockTtl = TimeSpan.FromMinutes(Math.Max(1, _opt.LockTtlMinutes));
        var lockToken = await redisManager.AcquireLockAsync(LockKey, lockTtl);
        if (lockToken is null)
        {
            logger.LogInformation("[MasterDataZipGenerator] Skip tick — lock held by another instance");
            return;
        }

        try
        {
            var tables = await syncRepo.GetSyncTableCountersAsync(DetectIsChangeMode, ct);
            if (tables.Count == 0)
            {
                logger.LogWarning(
                    "[MasterDataZipGenerator] SyncTable_Get 'C' returned 0 rows — no table has IsOnlyChange=1, nothing to do");
                WriteHeartbeat("Running", (int)lockTtl.TotalSeconds);
                return;
            }

            // Watermark nằm ngay trong `tables` (cột ZipWatermarkCounter, cùng dòng SQL với
            // POSLastCounter) — không cần round-trip Redis riêng, không có khái niệm "watermark
            // chưa tồn tại" (backfill 1 lần trong migration đảm bảo cột luôn có giá trị hợp lệ).
            var changedTables = tables
                .Where(t => !string.IsNullOrWhiteSpace(t.TableName))
                .Where(t => t.POSLastCounter > t.ZipWatermarkCounter)
                .ToList();

            if (changedTables.Count == 0)
            {
                logger.LogDebug("[MasterDataZipGenerator] No counter changes this cycle");
                WriteHeartbeat("Running", (int)lockTtl.TotalSeconds);
                return;
            }

            logger.LogInformation("[MasterDataZipGenerator] {Count} table(s) changed: {Tables}",
                changedTables.Count, string.Join(", ", changedTables.Select(t => t.TableName)));

            var enabledTerminals = (await mdRepo.GetPosTerminalListAsync(storeNo: null, ct))
                .Where(t => t.IsEnabled)
                .ToList();

            var quarantine = await redisManager.HashGetAllAsync<int>(QuarantineKey);
            var eligible = enabledTerminals
                .Where(t => !quarantine.TryGetValue(QuarantineField(t), out var fc) || fc < _opt.QuarantineThreshold)
                .ToList();
            var skippedQuarantined = enabledTerminals.Count - eligible.Count;
            if (skippedQuarantined > 0)
                logger.LogWarning(
                    "[MasterDataZipGenerator] Skipping {Count} quarantined terminal(s) (FailedCount >= {Threshold})",
                    skippedQuarantined, _opt.QuarantineThreshold);

            long successCount = 0;
            long failedCount = 0;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, _opt.MaxParallelTerminals),
                CancellationToken = ct
            };

            await Parallel.ForEachAsync(eligible, parallelOptions, async (terminal, token) =>
            {
                var field = QuarantineField(terminal);
                try
                {
                    var result = await syncDataPosService.PushMasterDataChangeAsync(terminal.StoreNo, terminal.No, token);
                    if (result.Success)
                    {
                        Interlocked.Increment(ref successCount);
                        await redisManager.HashDeleteAsync(QuarantineKey, field); // reset FailedCount về 0
                    }
                    else
                    {
                        Interlocked.Increment(ref failedCount);
                        await BumpQuarantineAsync(redisManager, field);
                        logger.LogError(
                            "[MasterDataZipGenerator] Generate failed for {Store}/{Terminal}: {Msg}",
                            terminal.StoreNo, terminal.No, result.Message);
                    }
                }
                catch (MasterDataThrottleException ex)
                {
                    // Bận nén dữ liệu (throttle cụm, tài nguyên chung) — KHÔNG phải lỗi của riêng
                    // terminal này, không tính vào quarantine.
                    Interlocked.Increment(ref failedCount);
                    logger.LogWarning(
                        "[MasterDataZipGenerator] Throttled for {Store}/{Terminal}: {Msg}",
                        terminal.StoreNo, terminal.No, ex.Message);
                }
                catch (Exception ex)
                {
                    Interlocked.Increment(ref failedCount);
                    await BumpQuarantineAsync(redisManager, field);
                    logger.LogError(ex, "[MasterDataZipGenerator] Generate failed for {Store}/{Terminal}",
                        terminal.StoreNo, terminal.No);
                }
            });

            logger.LogInformation(
                "[MasterDataZipGenerator] Cycle done — {Success} ok, {Failed} failed, {Quarantined} skipped-quarantine",
                successCount, failedCount, skippedQuarantined);

            // ACK: chỉ tịnh tiến watermark khi mọi terminal ĐÃ THỬ (không tính terminal bị quarantine
            // bỏ qua từ đầu) đều thành công — tránh 1 terminal hỏng vĩnh viễn chặn cả fleet, nhưng vẫn
            // giữ nguyên tắc "không đánh dấu đã gửi khi có nơi chưa nhận" cho các terminal đang active.
            // Giá trị ACK LUÔN lấy từ `changedTables` (snapshot POSLastCounter đọc lúc ĐẦU cycle) —
            // KHÔNG re-read DB tại đây, tránh nuốt mất thay đổi mới bump giữa lúc đọc và lúc ACK.
            if (failedCount == 0)
            {
                var snapshot = changedTables.ToDictionary(t => t.TableName!, t => t.POSLastCounter);
                await syncRepo.AckZipWatermarkAsync(snapshot, ct);
                logger.LogInformation("[MasterDataZipGenerator] Watermark advanced for {Count} table(s)",
                    changedTables.Count);
            }
            else
            {
                logger.LogWarning(
                    "[MasterDataZipGenerator] Watermark NOT advanced — {Failed} active terminal(s) failed this cycle",
                    failedCount);
            }

            Interlocked.Increment(ref _processedCycles);
            WriteHeartbeat(failedCount == 0 ? "Running" : "Degraded", (int)lockTtl.TotalSeconds);
        }
        finally
        {
            await redisManager.ReleaseLockAsync(LockKey, lockToken);
        }
    }

    private static string QuarantineField(PosTerminalListDto terminal) => $"{terminal.StoreNo}:{terminal.No}";

    private static async Task BumpQuarantineAsync(IRedisManager redisManager, string field)
    {
        var current = await redisManager.HashGetAsync<int>(QuarantineKey, field);
        await redisManager.HashSetAsync(QuarantineKey, field, current + 1);
    }

    private void WriteHeartbeat(string status, int ttlSeconds)
    {
        try
        {
            var hb = new WorkerHeartbeat
            {
                WorkerName = "MasterDataZipGenerator",
                Status = status,
                InstanceId = Environment.MachineName,
                LastBeatUtc = DateTime.UtcNow,
                QueueName = string.Empty,
                ProcessedCount = Interlocked.Read(ref _processedCycles),
            };
            redis.StringSet(HeartbeatKey, hb, ttlSeconds: ttlSeconds);
        }
        catch (Exception ex)
        {
            // Heartbeat KHÔNG được crash worker — nuốt exception, chỉ log (giống mọi worker khác).
            logger.LogWarning("[MasterDataZipGenerator] Heartbeat write failed — {Msg}", ex.Message);
        }
    }
}
