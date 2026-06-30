using Microsoft.Extensions.DependencyInjection;
using POS.Application.Features.Common;
using POS.Application.Features.DataSync;
using POS.Application.Features.Gift;
using POS.Application.Features.Partner;
using POS.Application.Features.Promotion;
using POS.Application.Features.Sap;

namespace POS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommonService, CommonService>();
        services.AddScoped<IAkaChainLoyaltyService, AkaChainLoyaltyService>();
        services.AddScoped<IGotITService, GotITService>();
        services.AddScoped<IUrboxService, UrboxService>();
        services.AddScoped<IDataRawService, DataRawService>();
        services.AddScoped<ISyncDataPosService, SyncDataPosService>();
        services.AddScoped<IMasterDataSyncService, MasterDataSyncService>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        services.AddScoped<IKafkaService, KafkaService>();
        services.AddScoped<IGiftService, GiftService>();
        services.AddScoped<ISAPService, SAPService>();
        services.AddScoped<IPromotionService, PromotionService>();
        services.AddScoped<ISpecialComboService, SpecialComboService>();
        return services;
    }
}
