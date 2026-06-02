using System.Data;
using Dapper;
using Microsoft.Extensions.Caching.Memory;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Infrastructure.Persistence;
using VCM.POSBLUE.Shared.DTOs;

namespace VCM.POSBLUE.Infrastructure.Authentication;

/// <summary>
/// Triển khai IPosAuthenticationService — GIỮ NGUYÊN logic của MemoryCacheService cũ:
///  - GetConfiguredXApiAsync: SELECT * FROM POSDataSetup (NOLOCK), lấy record Code == "X-API".
///  - ValidateWebApiUserAsync: SELECT * FROM SysWebApiUser NOLOCK WHERE Blocked = 0.
/// Có cache (IMemoryCache) như bản cũ để tránh query DB mỗi request.
/// </summary>
public sealed class PosAuthenticationService : IPosAuthenticationService
{
    private const string CacheKeySysWebApiUser = "MemoryCacheSysWebUserApi";
    private const string CacheKeyPosDataSetup = "MemoryGetPOSDataSetup";
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(10);

    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly IMemoryCache _cache;

    public PosAuthenticationService(ISqlConnectionFactory connectionFactory, IMemoryCache cache)
    {
        _connectionFactory = connectionFactory;
        _cache = cache;
    }

    public async Task<string?> GetConfiguredXApiAsync()
    {
        var setups = await GetPosDataSetupAsync();
        return setups.FirstOrDefault(x =>
            string.Equals(x.Code, "X-API", StringComparison.OrdinalIgnoreCase))?.Value;
    }

    public async Task<bool> ValidateWebApiUserAsync(string username, string password)
    {
        var users = await GetSysWebApiUsersAsync();
        return users.Any(x => x.UserName == username && x.Password == password);
    }

    private async Task<List<PosDataSetupDto>> GetPosDataSetupAsync()
    {
        if (_cache.TryGetValue(CacheKeyPosDataSetup, out List<PosDataSetupDto>? cached) && cached is not null)
            return cached;

        using IDbConnection db = _connectionFactory.CreateConnection(ConnectionNames.CentralMD);
        var result = (await db.QueryAsync<PosDataSetupDto>("SELECT * FROM POSDataSetup (NOLOCK)")).ToList();
        _cache.Set(CacheKeyPosDataSetup, result, CacheTtl);
        return result;
    }

    private async Task<List<SysWebApiUserDto>> GetSysWebApiUsersAsync()
    {
        if (_cache.TryGetValue(CacheKeySysWebApiUser, out List<SysWebApiUserDto>? cached) && cached is not null)
            return cached;

        using IDbConnection db = _connectionFactory.CreateConnection(ConnectionNames.CentralMD);
        var result = (await db.QueryAsync<SysWebApiUserDto>(
            "SELECT * FROM SysWebApiUser WITH (NOLOCK) WHERE Blocked = 0;")).ToList();
        if (result.Count > 0)
            _cache.Set(CacheKeySysWebApiUser, result, CacheTtl);
        return result;
    }
}
