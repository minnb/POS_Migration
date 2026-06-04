namespace POS.Application.Shared.Services;

public interface IRedisCacheService
{
    Task<T?> GetAsync<T>(string key) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null) where T : class;
    Task RemoveAsync(string key);
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Cache-aside với distributed lock chống stampede.
    /// Nếu key tồn tại → trả về cached. Không tồn tại → gọi factory, lưu vào cache rồi trả về.
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiry = null) where T : class;

    /// <summary>String key-value thuần (không serialize JSON) — dùng cho flag, counter.</summary>
    Task<string?> GetStringAsync(string key);
    Task SetStringAsync(string key, string value, TimeSpan? expiry = null);

    /// <summary>Hash operations — dùng cho member cache theo phone prefix.</summary>
    Task<T?> HashGetAsync<T>(string key, string field) where T : class;
    Task HashSetAsync<T>(string key, string field, T value, TimeSpan? expiry = null) where T : class;
}
