using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using POS.Infrastructure.Persistence;
using StackExchange.Redis;

namespace POS.API.HealthChecks;

// Endpoint: GET /health
// Kiểm tra tất cả: DB, Redis, RabbitMQ.
// Trả về 200 Healthy / 503 Unhealthy / 200 Degraded (1 service down).
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbConnectionFactory _connectionFactory;

    public DatabaseHealthCheck(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            using var conn = _connectionFactory.CreateConnection();
            await conn.ExecuteScalarAsync<int>("SELECT 1");
            return HealthCheckResult.Healthy("DB OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("DB FAIL", ex);
        }
    }
}

public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _multiplexer;

    public RedisHealthCheck(IConnectionMultiplexer multiplexer)
    {
        _multiplexer = multiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var db = _multiplexer.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis OK");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis FAIL", ex);
        }
    }
}
