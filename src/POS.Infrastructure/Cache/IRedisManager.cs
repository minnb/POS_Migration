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

    /// <summary>SCAN theo pattern, dừng sớm khi đủ <paramref name="maxResults"/> — dùng cho dashboard quản trị
    /// (tránh quét toàn bộ DB khi pattern rộng). Trả kèm cờ đã bị cắt bớt hay chưa.</summary>
    Task<(List<string> Keys, bool IsTruncated)> GetKeysByPatternAsync(string pattern, int maxResults);

    /// <summary>TTL còn lại (giây) — null nếu key không hết hạn hoặc không tồn tại.</summary>
    Task<long?> GetKeyTtlSecondsAsync(string key);

    /// <summary>Kiểu dữ liệu Redis của key: string/hash/list/set/zset/stream/none.</summary>
    Task<string> GetKeyTypeAsync(string key);

    /// <summary>Đọc giá trị thô của key theo đúng <paramref name="redisType"/> (đã biết trước qua
    /// <see cref="GetKeyTypeAsync"/>), serialize JSON (hash/list/set/zset) hoặc trả string thô.</summary>
    Task<string?> GetKeyRawValueAsync(string key, string redisType);

    // ──────────────────────────────────────────
    // Server diagnostics (dùng cho Redis Management Dashboard)
    // ──────────────────────────────────────────

    /// <summary>PING tới server (endpoint non-replica đầu tiên). Trả latency (ms) + endpoint dạng
    /// "host:port" — null message nếu OK, message lỗi nếu không kết nối được.</summary>
    Task<(bool IsOnline, long PingMs, string? Endpoint, string? Error)> PingAsync();

    /// <summary>INFO command, flatten các section cần dùng (Memory/Clients/Server/Stats/Replication)
    /// thành 1 dictionary field→value.</summary>
    Task<IDictionary<string, string>> GetServerInfoAsync();

    /// <summary>DBSIZE — tổng số key trong DB hiện tại (<see cref="RedisOptions.DefaultDatabase"/>).</summary>
    Task<long> GetDbSizeAsync();

    /// <summary>Index DB hiện tại đang thao tác (<see cref="RedisOptions.DefaultDatabase"/>).</summary>
    int DefaultDatabase { get; }

    // Distributed lock (SET NX + TTL, release an toàn bằng so khớp token qua Lua script)
    /// <summary>Thử lấy khóa <paramref name="key"/> (atomic SET NX PX). Trả token nếu thành công, null nếu đang bị giữ.</summary>
    Task<string?> AcquireLockAsync(string key, TimeSpan ttl);

    /// <summary>Nhả khóa — chỉ xoá nếu <paramref name="token"/> khớp token đang giữ (tránh xoá nhầm lock của process khác).</summary>
    Task<bool> ReleaseLockAsync(string key, string token);

    // Distributed throttle (sliding-window đếm bằng Sorted Set, atomic qua Lua)
    /// <summary>Thử giữ 1 "slot" trong sliding-window đếm bằng ZSET tại <paramref name="setKey"/>: dọn slot quá
    /// hạn (score &lt; now-<paramref name="staleAfter"/>, tự chữa lành khi process giữ slot bị crash — Set/ZSET
    /// không hỗ trợ TTL theo từng member), đếm ZCARD (O(log N)), nếu &lt; <paramref name="maxSlots"/> thì ZADD
    /// <paramref name="slotId"/>=now. Toàn bộ atomic trong 1 lệnh EVAL — không có race condition (TOCTOU) giữa
    /// đếm và ghi khi nhiều request tới đồng thời. Trả true nếu giữ được slot.</summary>
    Task<bool> TryAcquireSlotAsync(string setKey, string slotId, int maxSlots, TimeSpan staleAfter);

    /// <summary>Nhả slot đã giữ (ZREM). Gọi trong khối <c>finally</c>, không có tham số CancellationToken để
    /// đảm bảo nhả được kể cả khi request đã bị hủy giữa chừng.</summary>
    Task ReleaseSlotAsync(string setKey, string slotId);
}
