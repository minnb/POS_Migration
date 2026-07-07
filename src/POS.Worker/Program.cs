using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSerilogWithElastic();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<WorkerHealthState>();
builder.Services.AddSingleton<PosFileImportService>();

var roles = builder.Configuration.GetSection(WorkerRolesOptions.SectionName).Get<WorkerRolesOptions>()
            ?? new WorkerRolesOptions();

// Model A (cronjob trên Ubuntu host): "dotnet POS.Worker.dll --run-once" chạy 1 chu kỳ quét file
// rồi thoát — không đăng ký hosted service nào, không tồn tại process residency.
var runOnce = args.Contains("--run-once")
    || string.Equals(Environment.GetEnvironmentVariable("WORKER_RUN_ONCE"), "true", StringComparison.OrdinalIgnoreCase);

if (!runOnce)
{
    // Model B (Docker container dài hạn) hoặc dev local — bật theo WorkerRoles trong appsettings.
    if (roles.EnableFileProcessing) builder.Services.AddHostedService<PosFileImportWorker>();
    if (roles.EnableRabbitMQConsumer) builder.Services.AddHostedService<PosSalesConsumerWorker>();
    if (roles.EnableSqlReportWorker) builder.Services.AddHostedService<Rpt_ReportSaleDetail_Insert>();
    if (roles.EnableHeartbeat) builder.Services.AddHostedService<WorkerHeartbeatService>();
}

var host = builder.Build();

if (runOnce)
{
    var logger = host.Services.GetRequiredService<ILogger<Program>>();
    try
    {
        var svc = host.Services.GetRequiredService<PosFileImportService>();
        var processed = await svc.RunOnceAsync(CancellationToken.None);
        logger.LogInformation("[Cron] PosFileImport run-once xong — processed={Count}", processed);
        return 0;
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Cron] PosFileImport run-once thất bại");
        return 1;
    }
}

host.Run();
return 0;
