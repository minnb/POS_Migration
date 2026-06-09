using Newtonsoft.Json;
using POS.Infrastructure.Cache;

namespace POS.Infrastructure.Redis;

public sealed class RedisService(IRedisManager manager) : IRedisService
{
    // ──────────────────────────────────────────
    // Hash operations
    // ──────────────────────────────────────────

    public Task<T?> HashGetAsync<T>(string key, string field)
        => manager.HashGetAsync<T>(key, field);

    public T? HashGet<T>(string key, string field)
        => manager.HashGetAsync<T>(key, field).GetAwaiter().GetResult();

    public void HashSet<T>(string key, string field, T value, int? ttlSeconds = null)
        => manager.HashSetAsync(key, field, value, ttlSeconds ?? 0).GetAwaiter().GetResult();

    public void HashDelete(string key, string field)
        => manager.HashDeleteAsync(key, field).GetAwaiter().GetResult();

    // ──────────────────────────────────────────
    // String operations
    // ──────────────────────────────────────────

    public async Task<T?> StringGetAsync<T>(string key)
    {
        var raw = await manager.GetStringAsync(key);
        if (string.IsNullOrEmpty(raw)) return default;
        return JsonConvert.DeserializeObject<T>(raw);
    }

    public string? StringGetRaw(string key)
        => manager.GetStringAsync(key).GetAwaiter().GetResult();

    public void StringSet<T>(string key, T value, int? ttlSeconds = null)
    {
        var expiry = ttlSeconds.HasValue ? TimeSpan.FromSeconds(ttlSeconds.Value) : (TimeSpan?)null;
        manager.SetStringAsync(key, JsonConvert.SerializeObject(value), expiry).GetAwaiter().GetResult();
    }

    public void StringSetRaw(string key, string value, TimeSpan? ttl = null)
        => manager.SetStringAsync(key, value, ttl).GetAwaiter().GetResult();

    // ──────────────────────────────────────────
    // Key operations
    // ──────────────────────────────────────────

    public bool KeyExists(string key)
        => manager.KeyExistsAsync(key).GetAwaiter().GetResult();

    public void Delete(string key)
        => manager.DeleteAsync(key).GetAwaiter().GetResult();
}
