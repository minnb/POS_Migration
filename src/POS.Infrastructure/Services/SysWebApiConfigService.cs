using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using POS.Application.Shared.DTOs;
using POS.Application.Shared.Services;
using POS.Infrastructure.Persistence;

namespace POS.Infrastructure.Services;

public class SysWebApiConfigService : ISysWebApiConfigService
{
    private const string CacheKey = "BLUEPOS:SysWebApiConfig";
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SysWebApiConfigService> _logger;

    public SysWebApiConfigService(IDbConnectionFactory connectionFactory, IMemoryCache cache, ILogger<SysWebApiConfigService> logger)
    {
        _connectionFactory = connectionFactory;
        _cache = cache;
        _logger = logger;
    }

    public async Task<SysWebApiDto?> GetByAppCodeAsync(string appCode)
    {
        var all = await GetAllAsync();
        return all.FirstOrDefault(x => x.AppCode.Equals(appCode, StringComparison.OrdinalIgnoreCase));
    }

    private async Task<List<SysWebApiDto>> GetAllAsync()
    {
        if (_cache.TryGetValue<List<SysWebApiDto>>(CacheKey, out var cached) && cached?.Count > 0)
            return cached!;

        try
        {
            using var conn = _connectionFactory.CreateConnection();
            var apis = (await conn.QueryAsync<SysWebApiRecord>("SELECT * FROM SysWebApi (NOLOCK) WHERE Blocked = 0")).ToList();
            var routes = (await conn.QueryAsync<SysWebApiRouteRecord>("SELECT * FROM SysWebApiRoute (NOLOCK) WHERE Blocked = 0")).ToList();

            var result = apis.Select(api => new SysWebApiDto
            {
                AppCode = api.AppCode ?? string.Empty,
                Host = api.Host ?? string.Empty,
                Version = api.Version ?? string.Empty,
                Authorization = api.Authorization ?? string.Empty,
                UserName = api.UserName ?? string.Empty,
                Password = api.Password ?? string.Empty,
                PrivateKey = api.PrivateKey ?? string.Empty,
                PublicKey = api.PublicKey ?? string.Empty,
                Description = api.Description ?? string.Empty,
                HttpProxy = api.HttpProxy,
                Bypasslist = api.Bypasslist,
                Routes = routes
                    .Where(r => string.Equals(r.AppCode, api.AppCode, StringComparison.OrdinalIgnoreCase))
                    .Select(r => new SysWebApiRouteDto
                    {
                        AppCode = r.AppCode ?? string.Empty,
                        Name = r.Name ?? string.Empty,
                        Route = r.Route ?? string.Empty,
                        Notes = r.Notes
                    }).ToList()
            }).ToList();

            var ttl = DateTime.Today.AddDays(1) - DateTime.Now;
            _cache.Set(CacheKey, result, ttl > TimeSpan.Zero ? ttl : TimeSpan.FromHours(1));

            _logger.LogInformation("SysWebApiConfig loaded from DB: {Count} entries", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load SysWebApiConfig from DB");
            return new List<SysWebApiDto>();
        }
    }

    // Internal Dapper mapping records
    private sealed class SysWebApiRecord
    {
        public string? AppCode { get; set; }
        public string? Host { get; set; }
        public string? Version { get; set; }
        public string? Authorization { get; set; }
        public string? UserName { get; set; }
        public string? Password { get; set; }
        public string? PrivateKey { get; set; }
        public string? PublicKey { get; set; }
        public string? Description { get; set; }
        public string? HttpProxy { get; set; }
        public string? Bypasslist { get; set; }
        public int Blocked { get; set; }
    }

    private sealed class SysWebApiRouteRecord
    {
        public string? AppCode { get; set; }
        public string? Name { get; set; }
        public string? Route { get; set; }
        public string? Notes { get; set; }
        public int Blocked { get; set; }
    }
}
