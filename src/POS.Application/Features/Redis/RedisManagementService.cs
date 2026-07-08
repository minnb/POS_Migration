using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using POS.Common.Dtos.Redis;
using POS.Infrastructure.Cache;

namespace POS.Application.Features.Redis;

public sealed class RedisManagementService(IRedisManager redisManager) : IRedisManagementService
{
    private const int MaxScanResults = 1000;

    public async Task<RedisKeySearchResultDto> SearchKeysAsync(string pattern, CancellationToken ct = default)
    {
        var (keys, isTruncated) = await redisManager.GetKeysByPatternAsync(pattern, MaxScanResults);

        var infos = new List<RedisKeyInfoDto>(keys.Count);
        foreach (var key in keys)
        {
            ct.ThrowIfCancellationRequested();
            var type = await redisManager.GetKeyTypeAsync(key);
            var ttl = await redisManager.GetKeyTtlSecondsAsync(key);
            infos.Add(new RedisKeyInfoDto { Key = key, Type = type, TtlSeconds = ttl });
        }

        return new RedisKeySearchResultDto { Keys = infos, IsTruncated = isTruncated };
    }

    public async Task<RedisKeyValueDto?> GetKeyValueAsync(string key, CancellationToken ct = default)
    {
        var type = await redisManager.GetKeyTypeAsync(key);
        if (type == "none") return null;

        var ttl = await redisManager.GetKeyTtlSecondsAsync(key);
        var rawValue = await redisManager.GetKeyRawValueAsync(key, type) ?? string.Empty;

        return new RedisKeyValueDto
        {
            Key = key,
            Type = type,
            TtlSeconds = ttl,
            Value = PrettyPrintIfJson(rawValue),
        };
    }

    public async Task<bool> DeleteKeyAsync(string key, CancellationToken ct = default)
        => await redisManager.DeleteAsync(key);

    public async Task<RedisServerStatusDto> GetServerStatusAsync(CancellationToken ct = default)
    {
        var (isOnline, pingMs, endpoint, error) = await redisManager.PingAsync();
        if (!isOnline)
        {
            return new RedisServerStatusDto
            {
                IsOnline = false,
                Target = endpoint ?? string.Empty,
                DatabaseIndex = redisManager.DefaultDatabase,
                ErrorMessage = error,
            };
        }

        var infoTask = redisManager.GetServerInfoAsync();
        var dbSizeTask = redisManager.GetDbSizeAsync();
        await Task.WhenAll(infoTask, dbSizeTask);

        var info = infoTask.Result;
        var totalKeys = dbSizeTask.Result;

        double? hitRate = null;
        if (info.TryGetValue("keyspace_hits", out var hitsStr) &&
            info.TryGetValue("keyspace_misses", out var missesStr) &&
            long.TryParse(hitsStr, out var hits) && long.TryParse(missesStr, out var misses) &&
            hits + misses > 0)
        {
            hitRate = Math.Round(hits * 100.0 / (hits + misses), 1);
        }

        return new RedisServerStatusDto
        {
            IsOnline = true,
            PingMs = pingMs,
            Role = info.TryGetValue("role", out var role) ? role : "unknown",
            Target = endpoint ?? string.Empty,
            DatabaseIndex = redisManager.DefaultDatabase,
            UsedMemoryHuman = info.TryGetValue("used_memory_human", out var mem) ? mem : "—",
            ConnectedClients = info.TryGetValue("connected_clients", out var clientsStr) && int.TryParse(clientsStr, out var clients) ? clients : 0,
            TotalKeys = totalKeys,
            HitRatePercent = hitRate,
            UptimeSeconds = info.TryGetValue("uptime_in_seconds", out var uptimeStr) && long.TryParse(uptimeStr, out var uptime) ? uptime : 0,
        };
    }

    private static string PrettyPrintIfJson(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        try
        {
            var token = JToken.Parse(value);
            return token.ToString(Formatting.Indented);
        }
        catch (JsonException)
        {
            return value;
        }
    }
}
