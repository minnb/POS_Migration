using System.Text.Json;
using Microsoft.Extensions.Logging;
using POS.Application.Shared.Services;
using StackExchange.Redis;

namespace POS.Infrastructure.Cache;

public class RedisCacheService : IRedisCacheService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public RedisCacheService(IConnectionMultiplexer multiplexer, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer;
        _logger = logger;
    }

    private IDatabase Db => _multiplexer.GetDatabase();

    // ── Generic object ──────────────────────────────────────────────────────

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            var str = (string?)value;
            return str != null ? JsonSerializer.Deserialize<T>(str, JsonOptions) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GET lỗi key={Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            if (expiry.HasValue)
                await Db.StringSetAsync(key, json, expiry.Value);
            else
                await Db.StringSetAsync(key, json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis SET lỗi key={Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try { await Db.KeyDeleteAsync(key); }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis DEL lỗi key={Key}", key); }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try { return await Db.KeyExistsAsync(key); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis EXISTS lỗi key={Key}", key);
            return false;
        }
    }

    public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null) where T : class
    {
        var cached = await GetAsync<T>(key);
        if (cached != null) return cached;

        // Distributed lock để tránh cache stampede
        var lockKey = $"{key}:lock";
        var lockToken = Guid.NewGuid().ToString();
        var lockAcquired = await Db.StringSetAsync(lockKey, lockToken, TimeSpan.FromSeconds(10), When.NotExists);

        if (!lockAcquired)
        {
            await Task.Delay(200);
            return await GetAsync<T>(key);
        }

        try
        {
            var value = await factory();
            if (value != null) await SetAsync(key, value, expiry);
            return value;
        }
        finally
        {
            var current = (string?)await Db.StringGetAsync(lockKey);
            if (current == lockToken) await Db.KeyDeleteAsync(lockKey);
        }
    }

    // ── String thuần ────────────────────────────────────────────────────────

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            return (string?)value;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis GETSTRING lỗi key={Key}", key);
            return null;
        }
    }

    public async Task SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            if (expiry.HasValue)
                await Db.StringSetAsync(key, value, expiry.Value);
            else
                await Db.StringSetAsync(key, value);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Redis SETSTRING lỗi key={Key}", key); }
    }

    // ── Hash ─────────────────────────────────────────────────────────────────

    public async Task<T?> HashGetAsync<T>(string key, string field) where T : class
    {
        try
        {
            var value = await Db.HashGetAsync(key, field);
            var str = (string?)value;
            return str != null ? JsonSerializer.Deserialize<T>(str, JsonOptions) : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis HGET lỗi key={Key} field={Field}", key, field);
            return null;
        }
    }

    public async Task HashSetAsync<T>(string key, string field, T value, TimeSpan? expiry = null) where T : class
    {
        try
        {
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await Db.HashSetAsync(key, field, json);
            if (expiry.HasValue)
                await Db.KeyExpireAsync(key, expiry.Value);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis HSET lỗi key={Key} field={Field}", key, field);
        }
    }
}
