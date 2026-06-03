using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Repositories;
using POS.Application.Gift.Repositories;
using POS.Application.Gift.Services;
using POS.Application.Loyalty.Services;
using POS.Application.Payment.Services;
using POS.Application.Shared.Services;
using POS.Infrastructure.External;
using POS.Infrastructure.External.Capillary;
using POS.Infrastructure.External.GotIT;
using POS.Infrastructure.External.OneU;
using POS.Infrastructure.External.Urbox;
using POS.Infrastructure.Persistence;
using POS.Infrastructure.Repositories.Common;
using POS.Infrastructure.Repositories.Gift;
using POS.Infrastructure.Services;

namespace POS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, SqlServerConnectionFactory>();
        services.AddSingleton<IStoreConnectionFactory, StoreConnectionFactory>();

        services.AddMemoryCache();
        services.AddHttpClient();

        services.AddScoped<ISysWebApiConfigService, SysWebApiConfigService>();

        services.AddScoped<ICommonRepository, CommonRepository>();
        services.AddScoped<IGiftRepository, GiftRepository>();
        services.AddScoped<IWinXService, WinXHttpService>();

        services.AddScoped<IUrboxService, UrboxHttpService>();
        services.AddScoped<IGotITService, GotITHttpService>();
        services.AddScoped<IOneUService, OneUHttpService>();
        services.AddScoped<ICapillaryCouponService, CapillaryHttpService>();
        services.AddScoped<ILoyaltyCapillaryService, LoyaltyCapillaryHttpService>();

        return services;
    }
}
