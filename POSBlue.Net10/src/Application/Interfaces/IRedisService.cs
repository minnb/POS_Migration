namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho Redis (port từ RedisManager cũ). Chỉ expose method nghiệp vụ cấp cao,
/// KHÔNG lộ kiểu StackExchange.Redis ra tầng Application/Domain.
/// </summary>
public interface IRedisService
{
    bool Set(string key, string value, int fromSeconds);
    void SetCacheValue(string key, string value, int fromSeconds);
    string? GetCacheValue(string key);
    T? GetCacheValueToObject<T>(string key);
    void Delete(string key);
    void DeleteKeys(List<string> keys);
    List<string> GetAllKeys(string pattern, string keyExclude = "");
    bool CheckKeyExists(string key);

    bool StringSet<T>(string key, T value, long timeSeconds = 0, bool expiry = true);
    bool StringSetString(string key, string value, long timeSeconds = 0, bool expiry = true);
    Task<string?> GetStringAsync(string key);
    Task<T?> StringGetAsync<T>(string key);
    Task<string?> GetIsOfflineCapillary(string key);

    bool HashSet<T>(string hashKey, string hashField, T hashValue, long timeSeconds = 0);
    T? HashGet<T>(string hashKey, string hashField, bool isServerRead = false);
    Task<T?> HashGetAsync<T>(string hashKey, string hashField, bool isServerRead = false);
    bool HashDelete(string hashKey, string hashField);
    bool KeyExists(string hashKey, string hashField);
}
