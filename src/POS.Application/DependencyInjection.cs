using Microsoft.Extensions.DependencyInjection;
using POS.Application.Features.Common;
using POS.Application.Features.CouponVoucher;
using POS.Application.Features.DataSync;
using POS.Application.Features.Gift;
using POS.Application.Features.Partner;
using POS.Application.Features.Promotion;
using POS.Application.Features.Redis;
using POS.Application.Features.Sap;
using POS.Application.Features.SpAudit;
using POS.Application.Features.StoreActivities;

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
        services.AddScoped<Features.Price.IPriceService, Features.Price.PriceService>();
        services.AddScoped<ISpecialComboService, SpecialComboService>();
        services.AddScoped<ICouponService, CouponService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IVoucherPublishedService, VoucherPublishedService>();
        services.AddScoped<IBusinessDayService, BusinessDayService>();
        services.AddScoped<IRedisManagementService, RedisManagementService>();
        services.AddScoped<ISpAuditService, SpAuditService>();
        return services;
    }
}
