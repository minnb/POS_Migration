using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace POS.Infrastructure.Persistence;

public class StoreConnectionFactory : IStoreConnectionFactory
{
    private readonly IConfiguration _configuration;
    private readonly IDbConnectionFactory _centralMdFactory;

    // Simple in-memory cache: storeNo → serverIp (refresh mỗi 30 phút)
    private static readonly Dictionary<string, (string Ip, DateTime CachedAt)> _ipCache = new();
    private static readonly SemaphoreSlim _lock = new(1, 1);

    public StoreConnectionFactory(IConfiguration configuration, IDbConnectionFactory centralMdFactory)
    {
        _configuration = configuration;
        _centralMdFactory = centralMdFactory;
    }

    public async Task<string> GetServerIpAsync(string storeNo)
    {
        var key = storeNo.ToUpper();
        if (_ipCache.TryGetValue(key, out var cached) && DateTime.UtcNow - cached.CachedAt < TimeSpan.FromMinutes(30))
            return cached.Ip;

        await _lock.WaitAsync();
        try
        {
            if (_ipCache.TryGetValue(key, out cached) && DateTime.UtcNow - cached.CachedAt < TimeSpan.FromMinutes(30))
                return cached.Ip;

            // Fallback: query CentralGeneral.StoreSetServer
            using var conn = _centralMdFactory.CreateConnectionByName("CentralGeneral");
            var ip = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 ServerIP FROM StoreSetServer WHERE UPPER(StoreNo) = @StoreNo",
                new { StoreNo = key });

            _ipCache[key] = (ip ?? string.Empty, DateTime.UtcNow);
            return ip ?? string.Empty;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IDbConnection> CreateSaleConnectionAsync(string storeNo)
    {
        var serverIp = await GetServerIpAsync(storeNo);
        var template = _configuration.GetConnectionString("CentralSale_Template")
            ?? throw new InvalidOperationException("Connection string 'CentralSale_Template' not found");
        return new SqlConnection(string.Format(template, serverIp));
    }

    public async Task<IDbConnection> CreateSaleStagingConnectionAsync(string storeNo)
    {
        var serverIp = await GetServerIpAsync(storeNo);
        var template = _configuration.GetConnectionString("SaleStaging_Template")
            ?? throw new InvalidOperationException("Connection string 'SaleStaging_Template' not found");
        return new SqlConnection(string.Format(template, serverIp));
    }

    public async Task<IDbConnection> CreateKiosConnectionAsync(string storeNo)
    {
        var serverIp = await GetServerIpAsync(storeNo);
        var template = _configuration.GetConnectionString("KIOS_Template")
            ?? throw new InvalidOperationException("Connection string 'KIOS_Template' not found");
        return new SqlConnection(string.Format(template, serverIp));
    }
}
