using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

namespace POS.Infrastructure.Logging;

public static class SerilogConfiguration
{
    /// <summary>
    /// Đăng ký Serilog với Elasticsearch sink vào WebApplicationBuilder (POS.Api, POS.Web).
    /// </summary>
    public static WebApplicationBuilder AddSerilogWithElastic(
        this WebApplicationBuilder builder)
    {
        var esOptions = builder.Configuration
            .GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        builder.Host.UseSerilog((context, services, loggerConfig) =>
            ConfigureSerilogCore(loggerConfig, services, context.Configuration, esOptions));

        return builder;
    }

    /// <summary>
    /// Đăng ký Serilog với Elasticsearch sink vào HostApplicationBuilder (POS.Worker).
    /// </summary>
    public static HostApplicationBuilder AddSerilogWithElastic(
        this HostApplicationBuilder builder)
    {
        var esOptions = builder.Configuration
            .GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        builder.Services.AddSerilog((services, loggerConfig) =>
            ConfigureSerilogCore(loggerConfig, services, builder.Configuration, esOptions));

        return builder;
    }

    private static void ConfigureSerilogCore(
        LoggerConfiguration loggerConfig,
        IServiceProvider services,
        IConfiguration configuration,
        ElasticsearchOptions esOptions)
    {
        loggerConfig
            .ReadFrom.Configuration(configuration)
            .ReadFrom.Services(services)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");

        // File sink (rolling theo ngày) — bật khi có Logging:FileLogDirectory.
        // Console sink khi stdout bị redirect ra file (Task Scheduler) bị block-buffer → không flush;
        // file sink dưới đây flush đều nên log luôn đọc được, độc lập với Elasticsearch.
        var fileLogDir = configuration["Logging:FileLogDirectory"];
        if (!string.IsNullOrWhiteSpace(fileLogDir))
        {
            loggerConfig.WriteTo.File(
                path: Path.Combine(fileLogDir, "pos-.log"),      // → pos-yyyyMMdd.log
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                shared: true,                                    // nhiều instance ghi chung an toàn
                flushToDiskInterval: TimeSpan.FromSeconds(1),
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
        }

        var nodes = esOptions.Nodes
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => new Uri(n))
            .ToArray();

        if (nodes.Length > 0)
        {
            var dataset = ParseDataset(esOptions.IndexFormat);

            loggerConfig.WriteTo.Elasticsearch(nodes, opts =>
            {
                opts.DataStream = new DataStreamName("logs", dataset);
                opts.BootstrapMethod = BootstrapMethod.Silent;
                opts.ConfigureChannel = channelOpts =>
                {
                    channelOpts.BufferOptions = new BufferOptions
                    {
                        ExportMaxConcurrency = 10
                    };
                };
            }, transport =>
            {
                if (!string.IsNullOrEmpty(esOptions.Username))
                    transport.Authentication(new BasicAuthentication(esOptions.Username, esOptions.Password));
            });
        }
    }

    // "posblue-logs-{0:yyyy.MM.dd}" → "posblue-logs"
    private static string ParseDataset(string indexFormat)
    {
        var dashIndex = indexFormat.IndexOf("-{", StringComparison.Ordinal);
        return dashIndex > 0 ? indexFormat[..dashIndex] : indexFormat;
    }
}
