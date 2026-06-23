using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Infrastructure.Repositories.Interfaces;
using POS.Infrastructure.Workers;

namespace POS.Infrastructure.Workers;

/// <summary>
/// Hosted service that executes the stored procedure <c>Rpt_ReportSaleDetail_Insert</c>
/// once per minute. It passes the current date (date‑only) as both @FromDate and @ToDate.
/// The service updates <see cref="WorkerHealthState"/> so the existing monitoring UI can
/// display processed count and status.
/// </summary>
public sealed class Rpt_ReportSaleDetail_Insert(
    IServiceScopeFactory scopeFactory,
    ILogger<Rpt_ReportSaleDetail_Insert> logger,
    WorkerHealthState healthState) : BackgroundService
{
    // Default interval – can be overridden by configuration later if needed.
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[Rpt_ReportSaleDetail_Insert] Service started");
        healthState.Status = "Running";

        while (!stoppingToken.IsCancellationRequested)
        {
            var nextTick = DateTime.UtcNow.Add(Interval);
            try
            {
                // Create a fresh scope for the repository.
                await using var scope = scopeFactory.CreateAsyncScope();
                var repo = scope.ServiceProvider
                    .GetRequiredService<IRptReportSaleDetailRepository>();

                var today = DateTime.Now.Date; // date only, as per requirement
                await repo.ExecuteInsertAsync(today, today, stoppingToken);
                logger.LogInformation("[Rpt_ReportSaleDetail_Insert] Exec success for {Date:O}", today);
                healthState.IncrementProcessed();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[Rpt_ReportSaleDetail_Insert] Exec failed");
                healthState.Status = "Degraded";
                // Swallow so the loop continues.
            }

            // Wait until the next schedule (or cancellation).
            var delay = nextTick - DateTime.UtcNow;
            if (delay > TimeSpan.Zero)
                await Task.Delay(delay, stoppingToken);
        }

        logger.LogInformation("[Rpt_ReportSaleDetail_Insert] Service stopping");
    }
}
