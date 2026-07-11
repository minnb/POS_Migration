using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using POS.Application;
using POS.Infrastructure;
using POS.Worker.Workers;

namespace POS.ContractTests;

/// <summary>
/// GUARDRAIL DI — chặn lỗi "quên đăng ký service" lúc build/test thay vì lúc gọi API.
///
/// Dự án đăng ký DI thủ công từng dòng (AddApplication / AddInfrastructure). Quên một
/// dòng AddScoped → InvalidOperationException lúc runtime (xem CLAUDE.md mục A). Test này
/// dựng lại đúng cách compose DI của Program.cs rồi kiểm tra: mọi phụ thuộc kiểu POS.*
/// trong constructor của TẤT CẢ controller và mọi implementation đã đăng ký đều có mặt
/// trong container.
///
/// Chỉ đọc service descriptor — KHÔNG build provider — nên không kết nối Redis/SQL/Rabbit,
/// chạy được trên CI không có hạ tầng.
/// </summary>
public class DependencyInjectionTests
{
    /// <summary>Compose DI giống POS.Api/Program.cs (phần đăng ký service).</summary>
    private static IServiceCollection BuildServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        return services;
    }

    private static bool IsRegistered(HashSet<Type> registered, Type type)
    {
        if (registered.Contains(type)) return true;
        // Open-generic (vd ILogger<T>) — không áp dụng cho type POS.* nhưng giữ cho chắc.
        return type.IsGenericType && registered.Contains(type.GetGenericTypeDefinition());
    }

    /// <summary>Constructor mà DI sẽ chọn = public ctor nhiều tham số nhất.</summary>
    private static ConstructorInfo? GreediestCtor(Type type)
        => type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
               .OrderByDescending(c => c.GetParameters().Length)
               .FirstOrDefault();

    [Fact]
    public void Every_controller_dependency_is_registered()
    {
        var services = BuildServices();
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        var controllers = typeof(POS.Api.Controllers.BaseController).Assembly
            .GetTypes()
            .Where(t => typeof(ControllerBase).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsPublic: true });

        var missing = new List<string>();
        foreach (var controller in controllers)
        {
            var ctor = GreediestCtor(controller);
            if (ctor is null) continue;
            foreach (var p in ctor.GetParameters())
            {
                // Chỉ ràng buộc các phụ thuộc do dự án định nghĩa (POS.*).
                if (p.ParameterType.Namespace?.StartsWith("POS.", StringComparison.Ordinal) != true)
                    continue;
                if (!IsRegistered(registered, p.ParameterType))
                    missing.Add($"{controller.Name} cần {p.ParameterType.Name} (chưa đăng ký DI)");
            }
        }

        Assert.True(missing.Count == 0,
            "Thiếu đăng ký DI cho controller:\n  - " + string.Join("\n  - ", missing));
    }

    [Fact]
    public void Every_registered_service_dependency_is_registered()
    {
        var services = BuildServices();
        var registered = services.Select(d => d.ServiceType).ToHashSet();

        // Chỉ xét registration có ImplementationType cụ thể (bỏ factory/instance —
        // chúng có thể nhận tham số nguyên thuỷ như string baseDirectory).
        var implementations = services
            .Where(d => d.ImplementationType is not null)
            .Select(d => d.ImplementationType!)
            .Distinct();

        var missing = new List<string>();
        foreach (var impl in implementations)
        {
            var ctor = GreediestCtor(impl);
            if (ctor is null) continue;
            foreach (var p in ctor.GetParameters())
            {
                if (p.ParameterType.Namespace?.StartsWith("POS.", StringComparison.Ordinal) != true)
                    continue;
                if (!IsRegistered(registered, p.ParameterType))
                    missing.Add($"{impl.Name} cần {p.ParameterType.Name} (chưa đăng ký DI)");
            }
        }

        Assert.True(missing.Count == 0,
            "Thiếu đăng ký DI cho service:\n  - " + string.Join("\n  - ", missing));
    }

    /// <summary>
    /// GUARDRAIL riêng cho MasterDataZipGeneratorWorker (POS.Worker) — worker này cần
    /// IMasterDataSyncService/ISyncDataPosService (POS.Application) nhưng POS.Worker/Program.cs
    /// trước đây chỉ gọi AddInfrastructure(), KHÔNG gọi AddApplication(). Vì AddHostedService&lt;T&gt;()
    /// không validate constructor của T lúc compile, thiếu AddApplication() chỉ lộ ra lúc runtime
    /// (InvalidOperationException khi host khởi động) — test này bắt lỗi đó lúc build/test.
    /// </summary>
    [Fact]
    public void MasterDataZipGeneratorWorker_dependencies_are_registered()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMemoryCache();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpClient();
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.Configure<MasterDataZipGeneratorOptions>(
            configuration.GetSection(MasterDataZipGeneratorOptions.SectionName));

        var registered = services.Select(d => d.ServiceType).ToHashSet();
        registered.Add(typeof(IServiceScopeFactory)); // luôn có sẵn từ mọi ServiceProvider, không cần đăng ký thủ công

        var ctor = GreediestCtor(typeof(MasterDataZipGeneratorWorker));
        Assert.NotNull(ctor);

        var missing = ctor!.GetParameters()
            .Where(p => !IsRegistered(registered, p.ParameterType))
            .Select(p => $"MasterDataZipGeneratorWorker cần {p.ParameterType.Name} (chưa đăng ký DI)")
            .ToList();

        Assert.True(missing.Count == 0,
            "Thiếu đăng ký DI cho MasterDataZipGeneratorWorker:\n  - " + string.Join("\n  - ", missing));
    }
}
