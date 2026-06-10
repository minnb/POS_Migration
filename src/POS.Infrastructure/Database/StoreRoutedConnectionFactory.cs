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
/// ServerIP cache Redis hash MD:StoreSetServer:IP (TTL 12h).
/// Không tìm thấy ServerIP → fallback connection default (khớp behavior CentralSaleContainer() cũ).
/// </summary>
public sealed class StoreRoutedConnectionFactory(
    IConfiguration configuration,
    IRedisService redis,
    ILogger<StoreRoutedConnectionFactory> logger)
{
    private const string RedisKeyStoreSetServer = "MD:StoreSetServer:IP";

    /// <summary>Template key cho CentralSales — pass sau thêm "CentralSaleStaging"/"Kios" không cần sửa factory.</summary>
    public const string CentralSaleDb = "CentralSale";

    /// <summary>
    /// Thay ServerIPConnection.GetIPServerByStore cũ. Trả "" khi không tìm thấy/lỗi (→ fallback default).
    /// </summary>
    public async Task<string> GetServerByStoreAsync(string storeNo, CancellationToken ct = default)
    {
        try
        {
            var cached = redis.HashGet<string>(RedisKeyStoreSetServer, storeNo);
            if (!string.IsNullOrEmpty(cached)) return cached;

            var centralGeneralConn = configuration.GetConnectionString("CentralGeneral");
            if (string.IsNullOrEmpty(centralGeneralConn)) return "";

            await using var conn = new SqlConnection(centralGeneralConn);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand(
                "SELECT TOP 1 ServerIP FROM StoreSetServer (NOLOCK) WHERE UPPER(StoreNo) = UPPER(@storeNo);", conn);
            cmd.Parameters.AddWithValue("@storeNo", storeNo);
            var serverIP = (await cmd.ExecuteScalarAsync(ct)) as string ?? "";

            if (!string.IsNullOrEmpty(serverIP))
                redis.HashSet(RedisKeyStoreSetServer, storeNo, serverIP, ttlSeconds: 43200);
            return serverIP;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[StoreRouting] GetServerByStoreAsync failed — storeNo: {StoreNo}", storeNo);
            return "";
        }
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

        string connectionString;
        if (string.IsNullOrEmpty(serverIP))
        {
            connectionString = configuration.GetConnectionString(templateKey)
                ?? throw new InvalidOperationException($"ConnectionString '{templateKey}' không tìm thấy.");
        }
        else
        {
            var template = configuration.GetConnectionString($"{templateKey}Template")
                ?? throw new InvalidOperationException($"ConnectionString '{templateKey}Template' không tìm thấy.");
            connectionString = template.Replace("{server}", serverIP);
        }

        var conn = new SqlConnection(connectionString);
        await conn.OpenAsync(ct);
        return conn;
    }
}
