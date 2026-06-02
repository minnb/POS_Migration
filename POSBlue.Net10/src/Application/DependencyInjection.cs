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
        // TODO (các phase sau): đăng ký thêm các I[Feature]Service → [Feature]Service.
        return services;
    }
}
