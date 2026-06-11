using System.Collections.Concurrent;
using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using POS.Infrastructure.Redis;

namespace POS.Infrastructure.Database;

/// <summary>
/// Routing DB per-store — thay ServerIPConnection + CentralSaleContainer(ipServer) cũ.
/// CentralSales/CentralSalesStaging/KIOS bị shard theo store: bảng RPOSCentralGeneral.dbo.StoreSetServer
/// map StoreNo → ServerIP; connection string build từ template (chỉ khác Data Source).
///
/// Cache 3 tầng (mapping store→IP gần như bất biến):
///   L1 in-memory (TTL 10 phút, hot path 0 I/O) → L2 Redis hash MD:StoreSetServer:IP (TTL 12h) → L3 SQL StoreSetServer.
/// Redis lỗi vẫn rơi xuống SQL — không vì Redis outage mà route nhầm về default.
/// Không tìm thấy ServerIP → fallback connection default (khớp behavior CentralSaleContainer() cũ).
/// </summary>
public sealed class StoreRoutedConnectionFactory(
    IConfiguration configuration,
    IRedisService redis,
    ILogger<StoreRoutedConnectionFactory> logger)
{
    private const string RedisKeyStoreSetServer = "MD:StoreSetServer:IP";
    private const int RedisTtlSeconds = 43200;
    private static readonly TimeSpan LocalTtl = TimeSpan.FromMinutes(30);

    /// <summary>Template key cho CentralSales — pass sau thêm "CentralSaleStaging"/"Kios" không cần sửa factory.</summary>
    public const string CentralSaleDb = "CentralSale";

    // L1: storeNo → (ServerIP, hạn). Cache cả kết quả rỗng để store không có mapping
    // không hammer Redis/SQL mỗi call. Singleton DI nên dictionary sống cùng app.
    private readonly ConcurrentDictionary<string, (string ServerIP, DateTime ExpiresAt)> _serverIpCache = new();

    // Connection string build sẵn theo "{templateKey}|{serverIP}" — tránh Replace lặp lại trên hot path.
    private readonly ConcurrentDictionary<string, string> _connectionStringCache = new();

    /// <summary>
    /// Thay ServerIPConnection.GetIPServerByStore cũ. Trả "" khi không tìm thấy/lỗi (→ fallback default).
    /// </summary>
    public async Task<string> GetServerByStoreAsync(string storeNo, CancellationToken ct = default)
    {
        if (_serverIpCache.TryGetValue(storeNo, out var entry) && entry.ExpiresAt > DateTime.UtcNow)
            return entry.ServerIP;

        var serverIP = "";
        var resolved = false;

        // L2 Redis — lỗi chỉ log rồi rơi xuống SQL, không được nuốt luôn cả routing
        try
        {
            var cached = await redis.HashGetAsync<string>(RedisKeyStoreSetServer, storeNo);
            if (!string.IsNullOrEmpty(cached))
            {
                serverIP = cached;
                resolved = true;
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[StoreRouting] Redis HashGet failed — storeNo: {StoreNo}, thử SQL", storeNo);
        }

        // L3 SQL StoreSetServer
        if (!resolved)
        {
            try
            {
                var centralGeneralConn = configuration.GetConnectionString("CentralGeneral");
                if (string.IsNullOrEmpty(centralGeneralConn)) return "";

                await using var conn = new SqlConnection(centralGeneralConn);
                await conn.OpenAsync(ct);
                await using var cmd = new SqlCommand(
                    "SELECT TOP 1 ServerIP FROM StoreSetServer (NOLOCK) WHERE UPPER(StoreNo) = UPPER(@storeNo);", conn);
                cmd.Parameters.AddWithValue("@storeNo", storeNo);
                serverIP = (await cmd.ExecuteScalarAsync(ct)) as string ?? "";
                resolved = true;

                if (!string.IsNullOrEmpty(serverIP))
                {
                    try
                    {
                        await redis.HashSetAsync(RedisKeyStoreSetServer, storeNo, serverIP, ttlSeconds: RedisTtlSeconds);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "[StoreRouting] Redis HashSet backfill failed — storeNo: {StoreNo}", storeNo);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "[StoreRouting] GetServerByStoreAsync failed — storeNo: {StoreNo}", storeNo);
                return ""; // lỗi thật sự → không cache, lần sau thử lại
            }
        }

        // Chỉ cache khi resolve thành công (kể cả "" = store không có mapping)
        if (resolved)
            _serverIpCache[storeNo] = (serverIP, DateTime.UtcNow.Add(LocalTtl));
        return serverIP;
    }

    /// <summary>
    /// Mở connection tới DB routed theo store.
    /// templateKey: "CentralSale" → dùng ConnectionStrings:CentralSaleTemplate (placeholder {server})
    /// và fallback ConnectionStrings:CentralSale khi không có ServerIP.
    /// </summary>
    public async Task<IDbConnection> CreateOpenConnectionAsync(
        string storeNo, string templateKey = CentralSaleDb, CancellationToken ct = default)
    {
        var serverIP = await GetServerByStoreAsync(storeNo, ct);

        var connectionString = _connectionStringCache.GetOrAdd($"{templateKey}|{serverIP}", _ =>
        {
            if (string.IsNullOrEmpty(serverIP))
            {
                return configuration.GetConnectionString(templateKey)
                    ?? throw new InvalidOperationException($"ConnectionString '{templateKey}' không tìm thấy.");
            }

            var template = configuration.GetConnectionString($"{templateKey}Template")
                ?? throw new InvalidOperationException($"ConnectionString '{templateKey}Template' không tìm thấy.");
            return template.Replace("{server}", serverIP);
        });

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
