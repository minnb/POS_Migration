namespace POS.Infrastructure.Redis;

public interface IRedisService
{
    // Hash operations
    Task<T?> HashGetAsync<T>(string key, string field);
    T? HashGet<T>(string key, string field);
    void HashSet<T>(string key, string field, T value, int? ttlSeconds = null);
    void HashDelete(string key, string field);

    // String operations
    Task<T?> StringGetAsync<T>(string key);
    string? StringGetRaw(string key);
    void StringSet<T>(string key, T value, int? ttlSeconds = null);
    void StringSetRaw(string key, string value, TimeSpan? ttl = null);

    // Key operations
    bool KeyExists(string key);
    void Delete(string key);
}
