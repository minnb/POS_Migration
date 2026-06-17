using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Infrastructure.AppServices;
using POS.Infrastructure.AppServices.Interfaces;
using POS.Infrastructure.Cache;
using POS.Infrastructure.Database;
using POS.Infrastructure.Files;
using POS.Infrastructure.Logging;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Redis;
using POS.Infrastructure.Repositories;
using POS.Infrastructure.Repositories.Interfaces;
using POS.Infrastructure.Workers;

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
        services.AddSingleton<CentralSaleConnectionFactory>();
        // Routing per-store cho CentralSales (+ Staging/KIOS pass sau) — cache ServerIP vào Redis.
        services.AddSingleton<StoreRoutedConnectionFactory>();

        // ── Repositories ──────────────────────────────────────────────────────
        // Scoped: mỗi HTTP request dùng 1 connection riêng (Dapper open/close per call).
        services.AddScoped<ICentralMDRepository, CentralMDRepository>();
        services.AddScoped<ICentralSaleRepository, CentralSaleRepository>();
        services.AddScoped<IDataRawJsonRepository, DataRawJsonRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<IOfferStaffRepository, OfferStaffRepository>();
        services.AddScoped<IWincodeRepository, WincodeRepository>();
        services.AddScoped<ISAPVoucherRepository, SAPVoucherRepository>();

        // ── Redis ─────────────────────────────────────────────────────────────
        // Singleton: ConnectionMultiplexer thread-safe, thiết kế để share toàn app.
        services.AddSingleton<IRedisManager, RedisManager>();
        services.AddSingleton<IRedisService, RedisService>();

        // ── RabbitMQ ──────────────────────────────────────────────────────────
        // Singleton: giữ 1 IConnection, tạo IChannel mới mỗi lần publish.
        // Implements IAsyncDisposable → WebApplication tự gọi khi shutdown.
        services.AddSingleton<IRabbitMQProducer, RabbitMQProducer>();
        // PosSalesConsumerWorker KHÔNG đăng ký ở đây — đăng ký riêng trong POS.Api/Program.cs.
        // POS.Web cũng gọi AddInfrastructure() và không cần chạy worker này.

        // ── Kafka ─────────────────────────────────────────────────────────────
        // Singleton: IProducer thread-safe, 1 connection cho toàn app (như static factory cũ).
        // Dùng cho pipeline upload sale data (SyncDataPosController).
        services.AddSingleton<IKafkaProducer, KafkaProducer>();

        // ── File transfer (SyncDataPos) ───────────────────────────────────────
        services.AddSingleton<IFtpFileTransfer, WinScpFileTransfer>();

        // ── Logging ───────────────────────────────────────────────────────────
        // FileLogHelper nhận baseDirectory từ config, không inject IConfiguration trực tiếp.
        var logDir = configuration["Logging:FileLogDirectory"] ?? "Logs";
        services.AddSingleton<IFileLogHelper>(_ => new FileLogHelper(logDir));

        // KibanaService dùng Serilog + Elasticsearch — Singleton an toàn vì chỉ
        // inject ILogger<KibanaService> (Singleton-safe).
        services.AddSingleton<IKibanaService, KibanaService>();

        // ── Named HttpClients ─────────────────────────────────────────────────
        // BaseAddress và Timeout KHÔNG set ở đây — đọc từ DB (SysWebApi.Host) per-request.
        // Timeout set per-call trong service sau khi CreateClient().

        // FMV: AkaChain Loyalty — timeout đọc từ DB.
        // UseCookies = false: tắt cookie jar của handler (handler share giữa mọi request FMV)
        // → server cookie không bị lưu lại rồi replay sang request sau (tránh cookie bleed).
        // LƯU Ý: KHÔNG gửi cookie __tenant tới token endpoint — ABP tra tenant theo
        // giá trị cookie và trả 500 "Tenant not found" (xem GetAccessTokenAsync).
        services.AddHttpClient("FMV")
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                UseCookies = false
            });

        // GotIT: 35s timeout (set per-call). Prod certs → không bypass SSL.
        services.AddHttpClient("GotIT");

        // ── AppServices (HTTP clients) ────────────────────────────────────────
        // Scoped: inject ICentralMDRepository (Scoped).
        services.AddScoped<IAkaChainLoyaltyAppService, AkaChainLoyaltyAppService>();

        // ── Partner AppServices (HTTP clients) ───────────────────────────────
        // Scoped: inject ICentralMDRepository (Scoped).
        // UrboxService tạo HttpClient riêng per-call (cần per-request Signature header).
        services.AddScoped<IGotITAppService, GotITService>();
        services.AddScoped<IUrboxAppService, UrboxService>();
        services.AddScoped<IKafkaAppService, KafkaAppService>();
        return services;
    }
}
