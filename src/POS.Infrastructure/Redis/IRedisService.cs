namespace POS.Infrastructure.Redis;

public interface IRedisService
{
    // Hash operations
    Task<T?> HashGetAsync<T>(string key, string field);
    T? HashGet<T>(string key, string field);
    Task HashSetAsync<T>(string key, string field, T value, int? ttlSeconds = null);
    void HashSet<T>(string key, string field, T value, int? ttlSeconds = null);
    void HashDelete(string key, string field);

    // String operations
    Task<T?> StringGetAsync<T>(string key);
    string? StringGetRaw(string key);
    void StringSet<T>(string key, T value, int? ttlSeconds = null);
    Task StringSetAsync<T>(string key, T value, int? ttlSeconds = null);
    void StringSetRaw(string key, string value, TimeSpan? ttl = null);

    // Key operations
    bool KeyExists(string key);
    void Delete(string key);

    /// <summary>
    /// SCAN keys theo pattern (vd "GetFileFromFTP*") — thay RedisCacheService.GetKeys cũ.
    /// Chỉ dùng với pattern hẹp (queue/retry keys).
    /// </summary>
    List<string> GetKeysByPattern(string pattern);

    // Distributed throttle (sliding-window ZSET, atomic)
    /// <summary>Thử giữ 1 slot trong sliding-window đếm tại <paramref name="setKey"/> — tối đa
    /// <paramref name="maxSlots"/> slot đồng thời, slot quá hạn <paramref name="staleAfter"/> tự bị dọn ở lượt
    /// acquire kế tiếp (chữa lành khi process giữ slot crash). Trả true nếu giữ được.</summary>
    Task<bool> TryAcquireSlotAsync(string setKey, string slotId, int maxSlots, TimeSpan staleAfter);

    /// <summary>Nhả slot đã giữ — gọi trong <c>finally</c>, luôn thực hiện được kể cả khi request đã bị hủy.</summary>
    Task ReleaseSlotAsync(string setKey, string slotId);
}
