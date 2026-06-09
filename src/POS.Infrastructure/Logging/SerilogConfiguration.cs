using Elastic.Channels;
using Elastic.Ingest.Elasticsearch;
using Elastic.Ingest.Elasticsearch.DataStreams;
using Elastic.Serilog.Sinks;
using Elastic.Transport;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace POS.Infrastructure.Logging;

public static class SerilogConfiguration
{
    /// <summary>
    /// Đăng ký Serilog với Elasticsearch sink vào WebApplicationBuilder.
    /// Gọi trong Program.cs trước builder.Build().
    /// </summary>
    public static WebApplicationBuilder AddSerilogWithElastic(
        this WebApplicationBuilder builder)
    {
        var esOptions = builder.Configuration
            .GetSection(ElasticsearchOptions.SectionName)
            .Get<ElasticsearchOptions>() ?? new ElasticsearchOptions();

        builder.Host.UseSerilog((context, services, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
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

            if (esOptions.Nodes.Length > 0)
            {
                var nodes = esOptions.Nodes.Select(n => new Uri(n)).ToArray();

                // Tên dataset lấy từ IndexFormat (e.g. "posblue-logs-{0:yyyy.MM.dd}" → "posblue-logs")
                // Với Elastic.Serilog.Sinks (ECS), index được quản lý bởi data stream thay vì
                // daily index rotation. Data stream name: logs-{dataset}-default
                var dataset = ParseDataset(esOptions.IndexFormat);

                loggerConfig.WriteTo.Elasticsearch(nodes, opts =>
                {
                    opts.DataStream = new DataStreamName("logs", dataset);
                    // Silent: không crash startup khi ES chưa available (dev/prod cold-start).
                    // Failure chỉ dùng khi cần đảm bảo ES template tồn tại trước khi app nhận traffic.
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
        });

        return builder;
    }

    // "posblue-logs-{0:yyyy.MM.dd}" → "posblue-logs"
    private static string ParseDataset(string indexFormat)
    {
        var dashIndex = indexFormat.IndexOf("-{", StringComparison.Ordinal);
        return dashIndex > 0 ? indexFormat[..dashIndex] : indexFormat;
    }
}
