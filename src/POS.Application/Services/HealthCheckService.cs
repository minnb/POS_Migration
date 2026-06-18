using System.Diagnostics;
using System.Globalization;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using POS.Application.Interfaces;
using POS.Common.Dtos.Ops;
using POS.Infrastructure.Database;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Redis;

namespace POS.Application.Services;

/// <summary>
/// Check kết nối hạ tầng từ trong container — phục vụ chẩn đoán môi trường
/// (Redis, RabbitMQ, SQL, POS.Api HTTP health, Worker heartbeat).
/// </summary>
public sealed class HealthCheckService(
    IConfiguration configuration,
    IRedisService redis,
    IRabbitMQProducer rabbitMQProducer,
    IHttpClientFactory httpClientFactory,
    StoreRoutedConnectionFactory storeRoutedFactory) : IHealthCheckService
{
    private const string RedisProbeKey = "HealthCheck:Ping";
    private const int SqlConnectTimeoutSeconds = 5;

    public async Task<List<HealthCheckItemDto>> CheckAllAsync(string? storeNo, CancellationToken ct = default)
    {
        var workerName = configuration["HealthCheck:WorkerName"] ?? "DataSync";
        var results = await Task.WhenAll(
            CheckRedisAsync(ct),
            CheckRabbitMQAsync(ct),
            CheckSqlAsync("SQL:CentralMD",      configuration.GetConnectionString("CentralMD"),      ct),
            CheckSqlAsync("SQL:CentralGeneral", configuration.GetConnectionString("CentralGeneral"), ct),
            CheckSqlAsync("SQL:CentralSale",    configuration.GetConnectionString("CentralSale"),    ct),
            CheckCentralSaleTemplateAsync(storeNo, ct),
            CheckWebApiAsync(ct),
            CheckWorkerHeartbeatAsync(workerName, ct)
        );
        return [.. results];
    }

    // ── Redis: round-trip write/read ──────────────────────────────────────

    private async Task<HealthCheckItemDto> CheckRedisAsync(CancellationToken ct)
    {
        var target = string.Join(",", configuration.GetSection("Redis:SentinelHosts").Get<string[]>() ?? []);
        var sw = Stopwatch.StartNew();
        try
        {
            var probe = DateTime.UtcNow.Ticks.ToString();
            redis.StringSet(RedisProbeKey, probe, ttlSeconds: 60);
            var readBack = await redis.StringGetAsync<string>(RedisProbeKey).WaitAsync(ct);
            sw.Stop();

            // RedisService nuốt exception bên trong (trả null) → đối chiếu round-trip
            return readBack == probe
                ? Item("Redis", target, true, sw.ElapsedMilliseconds, "OK (write/read round-trip)")
                : Item("Redis", target, false, sw.ElapsedMilliseconds,
                    "Write/read round-trip thất bại — không kết nối được Redis (xem log [Redis])");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Item("Redis", target, false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    // ── RabbitMQ ──────────────────────────────────────────────────────────

    private async Task<HealthCheckItemDto> CheckRabbitMQAsync(CancellationToken ct)
    {
        var target = $"{configuration["RabbitMQ:Host"]}:{configuration["RabbitMQ:Port"]}";
        var sw = Stopwatch.StartNew();
        var (ok, message) = await rabbitMQProducer.CheckConnectionAsync(ct);
        sw.Stop();
        return Item("RabbitMQ", target, ok, sw.ElapsedMilliseconds, message);
    }

    // ── SQL: connection string cố định ────────────────────────────────────

    private static async Task<HealthCheckItemDto> CheckSqlAsync(
        string component, string? connectionString, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(connectionString))
            return Item(component, "", false, 0, "Connection string không tồn tại trong cấu hình");

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            ConnectTimeout = SqlConnectTimeoutSeconds
        };
        var target = $"{builder.DataSource}/{builder.InitialCatalog}";

        var sw = Stopwatch.StartNew();
        try
        {
            await using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync(ct);
            await using var cmd = new SqlCommand("SELECT DB_NAME();", conn);
            var dbName = (await cmd.ExecuteScalarAsync(ct)) as string;
            sw.Stop();
            return Item(component, target, true, sw.ElapsedMilliseconds, $"OK (DB_NAME() = {dbName})");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Item(component, target, false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    // ── SQL: CentralSaleTemplate (routing theo store) ─────────────────────

    private async Task<HealthCheckItemDto> CheckCentralSaleTemplateAsync(string? storeNo, CancellationToken ct)
    {
        const string component = "SQL:CentralSaleTemplate";
        var template = configuration.GetConnectionString("CentralSaleTemplate");
        if (string.IsNullOrEmpty(template))
            return Item(component, "", false, 0, "Connection string 'CentralSaleTemplate' không tồn tại");

        string server;
        string resolvedBy;
        if (!string.IsNullOrEmpty(storeNo))
        {
            // Đường đi thật của production: RPOSCentralGeneral.dbo.StoreSetServer (cache Redis)
            server = await storeRoutedFactory.GetServerByStoreAsync(storeNo, ct);
            if (string.IsNullOrEmpty(server))
                return Item(component, $"{{server}} (storeNo={storeNo})", false, 0,
                    $"Không tìm thấy ServerIP trong StoreSetServer cho store '{storeNo}' " +
                    "— runtime sẽ fallback connection 'CentralSale'");
            resolvedBy = $"StoreSetServer[{storeNo}]";
        }
        else
        {
            server = configuration["SetDb:DB1"] ?? "";
            if (string.IsNullOrEmpty(server))
                return Item(component, "{server}", false, 0,
                    "Không truyền storeNo và SetDb:DB1 trống — không có server để test template");
            resolvedBy = "SetDb:DB1";
        }

        var result = await CheckSqlAsync(component, template.Replace("{server}", server), ct);
        result.Message = $"[{resolvedBy} → {server}] {result.Message}";
        return result;
    }

    // ── POS.Api HTTP health endpoint ──────────────────────────────────────

    private async Task<HealthCheckItemDto> CheckWebApiAsync(CancellationToken ct)
    {
        var baseUrl = (configuration["HealthCheck:PosApiBaseUrl"] ?? "").TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
            return Item("POS.Api", "(không cấu hình)", false, 0,
                "HealthCheck:PosApiBaseUrl chưa được cấu hình trong appsettings");

        var url = $"{baseUrl}/health";
        var sw = Stopwatch.StartNew();
        try
        {
            using var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var resp = await client.GetAsync(url, ct);
            sw.Stop();
            return Item("POS.Api", url, resp.IsSuccessStatusCode, sw.ElapsedMilliseconds,
                $"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase}");
        }
        catch (Exception ex)
        {
            sw.Stop();
            return Item("POS.Api", url, false, sw.ElapsedMilliseconds, ex.Message);
        }
    }

    // ── Worker IHostedService — heartbeat qua Redis ───────────────────────

    private async Task<HealthCheckItemDto> CheckWorkerHeartbeatAsync(string workerName, CancellationToken ct)
    {
        var key = $"Worker:{workerName}:Heartbeat";
        var component = $"Worker:{workerName}";
        var raw = await redis.StringGetAsync<string>(key);

        if (raw == null)
            return Item(component, $"Redis key: {key}", false, 0,
                "Key không tồn tại — worker chưa chạy hoặc TTL đã hết");

        if (!DateTime.TryParse(raw, null, DateTimeStyles.RoundtripKind, out var ts))
            return Item(component, $"Redis key: {key}", false, 0,
                $"Không parse được timestamp: {raw}");

        var age = DateTime.UtcNow - ts;
        var ok = age.TotalSeconds < 120;
        return Item(component, $"Redis key: {key}", ok, (long)age.TotalSeconds * 1000,
            ok ? $"Alive — heartbeat {(int)age.TotalSeconds}s trước"
               : $"Stale — {(int)age.TotalMinutes}m trước (ngưỡng 2 phút)");
    }

    private static HealthCheckItemDto Item(
        string component, string target, bool ok, long latencyMs, string message)
        => new() { Component = component, Target = target, Ok = ok, LatencyMs = latencyMs, Message = message };
}
