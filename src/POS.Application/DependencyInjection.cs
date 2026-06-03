using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using POS.Application.Common.Services;
using POS.Application.Gift.Services;
using POS.Application.Payment.Services;

namespace POS.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommonService, CommonService>();
        services.AddScoped<IGiftService, GiftService>();
        services.AddScoped<IPartnerVoucherService, PartnerVoucherService>();

        // FluentValidation — đăng ký tất cả validators trong assembly này
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
