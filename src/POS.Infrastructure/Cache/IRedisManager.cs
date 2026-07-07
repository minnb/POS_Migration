namespace POS.Infrastructure.Cache;

public interface IRedisManager
{
    // String operations
    Task<string?> GetStringAsync(string key);
    Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null);
    Task<bool> DeleteAsync(string key);

    // Hash operations
    Task<T?> HashGetAsync<T>(string hashKey, string hashField);
    Task<bool> HashSetAsync<T>(string hashKey, string hashField, T value, int ttlSeconds = 0);
    Task<bool> HashDeleteAsync(string hashKey, string hashField);
    Task<IDictionary<string, T>> HashGetAllAsync<T>(string hashKey);

    // List operations
    Task<long> ListRightPushAsync(string key, string value);

    // Utility
    Task<bool> KeyExistsAsync(string key);
    Task<bool> KeyExpireAsync(string key, TimeSpan expiry);
    Task<List<string>> GetKeysByPatternAsync(string pattern);

    // Distributed lock (SET NX + TTL, release an toàn bằng so khớp token qua Lua script)
    /// <summary>Thử lấy khóa <paramref name="key"/> (atomic SET NX PX). Trả token nếu thành công, null nếu đang bị giữ.</summary>
    Task<string?> AcquireLockAsync(string key, TimeSpan ttl);

    /// <summary>Nhả khóa — chỉ xoá nếu <paramref name="token"/> khớp token đang giữ (tránh xoá nhầm lock của process khác).</summary>
    Task<bool> ReleaseLockAsync(string key, string token);
}
