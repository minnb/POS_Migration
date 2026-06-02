using Microsoft.Extensions.DependencyInjection;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Infrastructure.Authentication;
using VCM.POSBLUE.Infrastructure.Logging;
using VCM.POSBLUE.Infrastructure.Persistence;

namespace VCM.POSBLUE.Infrastructure;

/// <summary>
/// Đăng ký toàn bộ dịch vụ tầng Infrastructure vào DI container.
/// Gọi từ Program.cs: builder.Services.AddInfrastructure();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddMemoryCache();

        // Persistence
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        // Cross-cutting
        services.AddSingleton<IRequestLogger, SerilogRequestLogger>();
        services.AddScoped<IPosAuthenticationService, PosAuthenticationService>();

        // Repositories
        services.AddScoped<IPosTerminalRepository, PosTerminalRepository>();
        services.AddScoped<ILoyaltyRepository, LoyaltyRepository>();
        services.AddScoped<ICommonRepository, CommonRepository>();
        services.AddScoped<IOfferRepository, OfferRepository>();
        services.AddScoped<IValidateRepository, ValidateRepository>();

        // Messaging (RabbitMQ + SMS)
        services.AddSingleton<Messaging.RabbitMqConnectionProvider>();
        services.AddScoped<IMessageQueueService, Messaging.RabbitMqMessageQueueService>();
        services.AddScoped<ISmsService, Messaging.SmsService>();

        // Redis
        services.AddSingleton<Redis.RedisSentinelConnection>();
        services.AddScoped<IRedisService, Redis.RedisManager>();

        // Memory cache (master-data)
        services.AddScoped<IMemoryCacheService, Caching.MemoryCacheService>();

        // SOAP gateways
        services.AddScoped<IOneFlexiAxisClient, Gateways.OneFlexiAxis.OneFlexiAxisGateway>();

        // HTTP keystone (partner API calls)
        services.AddHttpClient();
        services.AddScoped<IApiCallClient, Http.ApiCallClient>();
        // SyncDataPos services
        services.AddScoped<ISyncDataPosService, SyncDataPosService>();
        services.AddScoped<ISyncDataToPosClient, LegacySyncDataToPosClient>();
        services.AddScoped<IDataRawService, DataRawService>();
        services.AddScoped<IWinpayService, Application.Services.WinpayService>();

        // SAP / ROP
        services.AddScoped<ISapVoucherClient, Gateways.SAP.SapVoucherSoapClient>();
        services.AddScoped<IRopVoucherService, Services.RopVoucherService>();

        // WinLife / WinCode
        services.AddScoped<IWinCodeRepository, WinCodeRepository>();

        return services;
    }
}
