using Microsoft.Extensions.DependencyInjection;
using POS.Application.Interfaces;
using POS.Application.Services;

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
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        return services;
    }
}
