using Microsoft.Extensions.DependencyInjection;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Application.Services;

namespace VCM.POSBLUE.Application;

/// <summary>
/// Đăng ký các Use Case / Application Service vào DI container.
/// Gọi từ Program.cs: builder.Services.AddApplication();
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommonService, CommonService>();
        services.AddScoped<ICXService, CXService>();
        services.AddScoped<ILoyaltyService, LoyaltyService>();
        services.AddScoped<IOfferService, OfferService>();
        services.AddScoped<IValidateService, ValidateService>();
        services.AddScoped<ISAPService, SAPService>();
        services.AddScoped<IWinLifeService, WinLifeService>();
        services.AddScoped<IVoucherService, VoucherService>();
        services.AddScoped<IVinIdTopUpService, VinIdTopUpService>();
        services.AddScoped<IPLGService, PLGService>();
        services.AddScoped<IPaymentService, PaymentService>();
        return services;
    }
}
