using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Infrastructure.AppServices;
using POS.Infrastructure.AppServices.Interfaces;
using POS.Infrastructure.Cache;
using POS.Infrastructure.Database;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Đăng ký toàn bộ Infrastructure services vào DI container.
    /// Gọi trong Program.cs: builder.Services.AddInfrastructure(builder.Configuration)
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── Database Factories ────────────────────────────────────────────────
        // Singleton: factory chỉ đọc ConnectionString một lần, thread-safe.
        // Repository inject CONCRETE type (không qua interface) nên đăng ký thẳng.
        services.AddSingleton<CentralMDConnectionFactory>();
        services.AddSingleton<LoyaltyConnectionFactory>();
        services.AddSingleton<StagingDbConnectionFactory>();

        // ── Repositories ──────────────────────────────────────────────────────
        // Scoped: mỗi HTTP request dùng 1 connection riêng (Dapper open/close per call).
        services.AddScoped<ICentralMDRepository, CentralMDRepository>();
        services.AddScoped<IDataRawJsonRepository, DataRawJsonRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<IOfferStaffRepository, OfferStaffRepository>();
        services.AddScoped<IWincodeRepository, WincodeRepository>();

        // ── Redis ─────────────────────────────────────────────────────────────
        // Singleton: ConnectionMultiplexer thread-safe, thiết kế để share toàn app.
        services.AddSingleton<IRedisManager, RedisManager>();
        services.AddSingleton<IRedisService, RedisService>();

        // ── RabbitMQ ──────────────────────────────────────────────────────────
        // Singleton: giữ 1 IConnection, tạo IChannel mới mỗi lần publish.
        // Implements IAsyncDisposable → WebApplication tự gọi khi shutdown.
        services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();

        // ── Logging ───────────────────────────────────────────────────────────
        // FileLogHelper nhận baseDirectory từ config, không inject IConfiguration trực tiếp.
        var logDir = configuration["Logging:FileLogDirectory"] ?? "Logs";
        services.AddSingleton<IFileLogHelper>(_ => new FileLogHelper(logDir));

        // KibanaService dùng Serilog + Elasticsearch — Singleton an toàn vì chỉ
        // inject ILogger<KibanaService> (Singleton-safe).
        services.AddSingleton<IKibanaService, KibanaService>();

        // ── AppServices (HTTP clients) ────────────────────────────────────────
        // Named client "FMV": không set BaseAddress vì URL đọc từ DB (SysWebApi.Host).
        // Timeout cũng đọc từ DB (SysWebApi.Version) nên set per-request trong service.
        // AkaChainLoyaltyAppService: Scoped — inject ICentralMDRepository (Scoped) + IRedisService (Singleton).
        services.AddHttpClient("FMV");
        services.AddScoped<IAkaChainLoyaltyAppService, AkaChainLoyaltyAppService>();

        return services;
    }
}
