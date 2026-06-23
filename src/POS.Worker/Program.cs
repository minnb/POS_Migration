using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSerilogWithElastic();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<WorkerHealthState>();
builder.Services.AddHostedService<PosSalesConsumerWorker>();
builder.Services.AddHostedService<WorkerHeartbeatService>();
builder.Services.AddHostedService<Rpt_ReportSaleDetail_Insert>();

var host = builder.Build();
host.Run();
