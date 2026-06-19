using POS.Infrastructure;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddSerilogWithElastic();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PosSalesConsumerWorker>();

var host = builder.Build();
host.Run();
