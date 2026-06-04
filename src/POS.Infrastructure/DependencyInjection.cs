using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Repositories;
using POS.Application.Gift.Repositories;
using POS.Application.Gift.Services;
using POS.Application.Loyalty.Services;
using POS.Application.Messaging;
using POS.Application.Payment.Services;
using POS.Application.Shared.Services;
using POS.Infrastructure.Cache;
using POS.Infrastructure.External;
using POS.Infrastructure.External.Capillary;
using POS.Infrastructure.External.GotIT;
using POS.Infrastructure.External.OneU;
using POS.Infrastructure.External.Urbox;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Repositories.Common;
using POS.Infrastructure.Repositories.Gift;
using POS.Infrastructure.Services;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // ── Database ─────────────────────────────────────────────────────────
        services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();
        services.AddSingleton<IStoreConnectionFactory, StoreConnectionFactory>();
        services.AddMemoryCache();

        // ── HTTP Clients + Resilience ─────────────────────────────────────────
        services.AddHttpClient();
        services.ConfigureHttpClientDefaults(b =>
            b.AddStandardResilienceHandler(o =>
            {
                o.Retry.MaxRetryAttempts = 2;
                o.Retry.Delay = TimeSpan.FromMilliseconds(300);
                o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                o.CircuitBreaker.FailureRatio = 0.5;
                o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(90);
            }));

        // ── Redis (Sentinel hoặc Standalone — đọc Redis:Mode) ────────────────
        services.AddSingleton<IConnectionMultiplexer>(sp =>
            CreateRedisConnection(sp.GetRequiredService<IConfiguration>()));
        services.AddSingleton<IRedisCacheService, RedisCacheService>();

        // ── RabbitMQ ──────────────────────────────────────────────────────────
        var mqEnabled = bool.TryParse(configuration["RabbitMQ:Enabled"], out var en) && en;
        if (mqEnabled)
        {
            services.AddSingleton<IConnection>(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var factory = new ConnectionFactory
                {
                    HostName = cfg["RabbitMQ:Host"] ?? "localhost",
                    Port = int.TryParse(cfg["RabbitMQ:Port"], out var p) ? p : 5672,
                    VirtualHost = cfg["RabbitMQ:VirtualHost"] ?? "/",
                    UserName = cfg["RabbitMQ:Username"] ?? "guest",
                    Password = cfg["RabbitMQ:Password"] ?? "guest",
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    AutomaticRecoveryEnabled = true,
                };
                return factory.CreateConnectionAsync().GetAwaiter().GetResult();
            });
            services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();
        }
        else
        {
            // Stub (dev / nếu không cần MQ)
            services.AddSingleton<IMessagePublisher, NullMessagePublisher>();
        }

        // ── Application Services ──────────────────────────────────────────────
        services.AddScoped<ISysWebApiConfigService, SysWebApiConfigService>();

        // ── Repositories ──────────────────────────────────────────────────────
        services.AddScoped<ICommonRepository, CommonRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();

        // ── External HTTP Services ────────────────────────────────────────────
        services.AddScoped<IWinXService, WinXHttpService>();
        services.AddScoped<IUrboxService, UrboxHttpService>();
        services.AddScoped<IGotITService, GotITHttpService>();
        services.AddScoped<IOneUService, OneUHttpService>();
        services.AddScoped<ICapillaryCouponService, CapillaryHttpService>();
        services.AddScoped<ILoyaltyCapillaryService, LoyaltyCapillaryHttpService>();

        return services;
    }

    // ── Redis connection factory ──────────────────────────────────────────────
    // Đọc Redis:Mode ("Sentinel" | "Standalone") để tạo đúng topology.
    private static IConnectionMultiplexer CreateRedisConnection(IConfiguration cfg)
    {
        var mode = cfg["Redis:Mode"] ?? "Standalone";

        var options = new ConfigurationOptions
        {
            Password          = cfg["Redis:Password"] ?? string.Empty,
            AbortOnConnectFail = bool.TryParse(cfg["Redis:AbortOnConnectFail"], out var abort) && abort,
            ConnectTimeout    = int.TryParse(cfg["Redis:ConnectTimeout"],    out var ct) ? ct : 5000,
            SyncTimeout       = int.TryParse(cfg["Redis:SyncTimeout"],       out var st) ? st : 5000,
            DefaultDatabase   = int.TryParse(cfg["Redis:DefaultDatabase"],   out var db) ? db : 0,
        };

        if (mode.Equals("Sentinel", StringComparison.OrdinalIgnoreCase))
        {
            // Sentinel HA — nhiều sentinel hosts, ServiceName bắt buộc
            options.ServiceName = cfg["Redis:ServiceName"] ?? "mymaster";
            var hosts = cfg.GetSection("Redis:SentinelHosts").Get<string[]>()
                        ?? throw new InvalidOperationException("Redis:SentinelHosts không được để trống khi Mode=Sentinel");
            foreach (var host in hosts)
                options.EndPoints.Add(host);
        }
        else
        {
            // Standalone — 1 host duy nhất, không cần ServiceName
            var host = cfg["Redis:Host"] ?? "localhost";
            var port = int.TryParse(cfg["Redis:Port"], out var p) ? p : 6379;
            options.EndPoints.Add(host, port);
        }

        return ConnectionMultiplexer.Connect(options);
    }
}
