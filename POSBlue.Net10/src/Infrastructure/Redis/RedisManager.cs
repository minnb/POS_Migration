using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using VCM.POSBLUE.Application.Interfaces;
using VCM.POSBLUE.Shared.Helpers;

namespace VCM.POSBLUE.Infrastructure.Redis;

/// <summary>
/// Triển khai IRedisService — port từ RedisManager cũ.
/// Dùng RedisSentinelConnection (inject) khi RedisActive=Sentinels, ngược lại connect trực tiếp theo config.
/// Thay FileHelper→Serilog, HttpClientService.SendMessageSMS→ISmsService, ConfigurationManager→IConfiguration.
/// </summary>
public sealed class RedisManager : IRedisService
{
    private readonly RedisSentinelConnection _sentinel;
    private readonly IConfiguration _configuration;
    private readonly ISmsService _sms;

    private readonly object _lock = new();
    private ConnectionMultiplexer? _connection;
    private bool _isInitialized;
    private long _requestCounter;

    public RedisManager(RedisSentinelConnection sentinel, IConfiguration configuration, ISmsService sms)
    {
        _sentinel = sentinel;
        _configuration = configuration;
        _sms = sms;
    }

    private ConnectionMultiplexer GetConnection()
    {
        if (_sentinel.IsRedisSentinel()) return _sentinel.GetConnection();
        if (!_isInitialized)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    _connection = ConnectionMultiplexer.Connect(_configuration.GetConnectionString("RedisConnectionDefault")!);
                    _isInitialized = true;
                }
            }
        }
        return _connection!;
    }

    private ConnectionMultiplexer GetConnectionDbRead()
    {
        if (_sentinel.IsRedisSentinel()) return _sentinel.GetConnectionReplica();
        if (!_isInitialized)
        {
            lock (_lock)
            {
                if (!_isInitialized)
                {
                    _connection = ConnectionMultiplexer.Connect(_configuration.GetConnectionString("RedisConnectionSecond")!);
                    _isInitialized = true;
                }
            }
        }
        return _connection!;
    }

    private bool IsReadConnected() => Interlocked.Increment(ref _requestCounter) % 2 == 0;

    private IDatabase GetDatabase(bool isRead = false, int dbIndex = -1)
    {
        if (isRead) return GetConnectionDbRead().GetDatabase(dbIndex);
        return GetConnection().GetDatabase(dbIndex);
    }

    private IServer GetServer()
    {
        var connection = GetConnection();
        return connection.GetServer(connection.GetEndPoints().First());
    }

    public bool Set(string key, string value, int fromSeconds)
    {
        try { GetDatabase().StringSet(key, value, TimeSpan.FromSeconds(fromSeconds)); return true; }
        catch (Exception ex) { Log.Information("RedisManager.Set.Exception: {Message}", ex.Message); return false; }
    }

    public void SetCacheValue(string key, string value, int fromSeconds)
    {
        try { GetDatabase().StringSet(key, value, TimeSpan.FromSeconds(fromSeconds)); }
        catch (Exception ex) { Log.Information("RedisManager.SetCacheValue.Exception: {Message}", ex.Message); }
    }

    public string? GetCacheValue(string key)
    {
        try { return GetDatabase(IsReadConnected()).StringGet(key); }
        catch (Exception ex) { Log.Information("RedisManager.GetCacheValue.Exception: {Message}", ex.Message); return null; }
    }

    public T? GetCacheValueToObject<T>(string key)
    {
        try
        {
            var data = GetCacheValue(key);
            return !string.IsNullOrEmpty(data) ? ConvertHelper.ToObject<T>(data) : default;
        }
        catch (Exception ex) { Log.Information("RedisManager.GetCacheValueToObject.Exception: {Message}", ex.Message); return default; }
    }

    public void Delete(string key)
    {
        try { GetDatabase().KeyDelete(key); }
        catch (Exception ex) { Log.Information("RedisManager.Delete.Exception: {Message}", ex.Message); }
    }

    public void DeleteKeys(List<string> keys)
    {
        try { var db = GetDatabase(); foreach (var key in keys) db.KeyDelete(key); }
        catch (Exception ex) { Log.Information("RedisManager.DeleteKeys.Exception: {Message}", ex.Message); }
    }

    public List<string> GetAllKeys(string pattern, string keyExclude = "")
    {
        var keys = new List<string>();
        try
        {
            pattern += ":*";
            var server = GetServer();
            var db = GetDatabase();
            foreach (var key in server.Keys(database: db.Database, pattern: pattern))
            {
                if (!string.IsNullOrEmpty(keyExclude) && key.ToString().ToUpper() == keyExclude.ToUpper()) continue;
                keys.Add(key!);
            }
        }
        catch (Exception ex) { Log.Information("RedisManager.GetAllKeys.Exception: {Message}", ex.Message); }
        return keys;
    }

    public bool CheckKeyExists(string key)
    {
        try { return GetDatabase(IsReadConnected()).KeyExists(key); }
        catch (Exception ex) { Log.Information("RedisManager.CheckKeyExists.Exception: {Message}", ex.Message); return false; }
    }

    public bool StringSet<T>(string key, T value, long timeSeconds = 0, bool expiry = true)
    {
        try
        {
            if (timeSeconds == 0) timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
            if (expiry && timeSeconds > 0)
                GetDatabase().StringSet(key, JsonConvert.SerializeObject(value), TimeSpan.FromSeconds(timeSeconds));
            else
                GetDatabase().StringSet(key, JsonConvert.SerializeObject(value));
            return true;
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.StringSet.Exception: {Message}", ex.Message);
            SendMessageSms("Warning", $"RedisManager.StringSet({key}).Exception: {ex.Message}", ex.Message);
            return false;
        }
    }

    public bool StringSetString(string key, string value, long timeSeconds = 0, bool expiry = true)
    {
        try
        {
            if (timeSeconds == 0) timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
            if (expiry && timeSeconds > 0)
                GetDatabase().StringSet(key, value, TimeSpan.FromSeconds(timeSeconds));
            else
                GetDatabase().StringSet(key, value);
            return true;
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.StringSetString.Exception: {Message}", ex.Message);
            SendMessageSms("Warning", $"RedisManager.StringSet({key}).Exception: {ex.Message}", ex.Message);
            return false;
        }
    }

    public async Task<string?> GetStringAsync(string key)
    {
        if (!GetDatabase(IsReadConnected()).KeyExists(key)) return default;
        RedisValue redisValue = _sentinel.IsRedisSentinel()
            ? await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica)
            : GetDatabase(IsReadConnected()).StringGet(key);
        return !string.IsNullOrEmpty(redisValue) ? redisValue.ToString() : default;
    }

    public async Task<T?> StringGetAsync<T>(string key)
    {
        try
        {
            if (!GetDatabase(IsReadConnected()).KeyExists(key)) return default;
            RedisValue redisValue = _sentinel.IsRedisSentinel()
                ? await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica)
                : GetDatabase(IsReadConnected()).StringGet(key);
            return !string.IsNullOrEmpty(redisValue) ? ConvertHelper.ToObject<T>(redisValue!) : default;
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.StringGetAsync.Exception: {Message}", ex.Message);
            SendMessageSms("Warning", $"RedisManager.StringGet({key}).Exception: {ex.Message}");
            return default;
        }
    }

    public async Task<string?> GetIsOfflineCapillary(string key)
    {
        try
        {
            if (_sentinel.IsRedisSentinel())
                return await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica);
            return await GetDatabase().StringGetAsync(key);
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.GetIsOfflineCapillary({Key}).Exception: {Message}", key, ex.Message);
            SendMessageSms("Error", $"RedisManager.GetIsOfflineCapillary({key}).Exception: {ex.Message}", ex.Message);
            return await GetDatabase().StringGetAsync(key);
        }
    }

    public bool HashSet<T>(string hashKey, string hashField, T hashValue, long timeSeconds = 0)
    {
        try
        {
            if (timeSeconds == 0) timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
            var tran = GetDatabase().CreateTransaction();
            _ = tran.HashSetAsync(hashKey, hashField, JsonConvert.SerializeObject(hashValue));
            _ = tran.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(timeSeconds));
            return tran.ExecuteAsync().Result;
        }
        catch (Exception ex)
        {
            Log.Information("{HashKey}:{HashField} HashSet.Exception: {Message}", hashKey, hashField, ex.Message);
            _ = _sms.SendMessageSmsAsync($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message);
            return false;
        }
    }

    public T? HashGet<T>(string hashKey, string hashField, bool isServerRead = false)
    {
        try
        {
            if (string.IsNullOrEmpty(hashField)) return default;
            RedisValue data = isServerRead && _sentinel.IsRedisSentinel()
                ? GetDatabase(isRead: true).HashGet(hashKey, hashField, CommandFlags.PreferReplica)
                : GetDatabase(isRead: isServerRead).HashGet(hashKey, hashField);
            return !string.IsNullOrEmpty(data) ? ConvertHelper.ToObject<T>(data!) : default;
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.HashGet.Exception: {Message}", ex.Message);
            _ = _sms.SendMessageSmsAsync($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message);
            return default;
        }
    }

    public async Task<T?> HashGetAsync<T>(string hashKey, string hashField, bool isServerRead = false)
    {
        try
        {
            if (string.IsNullOrEmpty(hashField)) return default;
            RedisValue data = isServerRead && _sentinel.IsRedisSentinel()
                ? await GetDatabase(isRead: true).HashGetAsync(hashKey, hashField, CommandFlags.PreferReplica)
                : await GetDatabase(isRead: isServerRead).HashGetAsync(hashKey, hashField);
            return !string.IsNullOrEmpty(data) ? ConvertHelper.ToObject<T>(data!) : default;
        }
        catch (Exception ex)
        {
            Log.Information("RedisManager.HashGetAsync.Exception: {Message}", ex.Message);
            _ = _sms.SendMessageSmsAsync($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message);
            return default;
        }
    }

    public bool HashDelete(string hashKey, string hashField)
    {
        try { return GetDatabase().HashDelete(hashKey, hashField); }
        catch (Exception ex) { Log.Information("RedisManager.HashDelete.Exception: {Message}", ex.Message); return false; }
    }

    public bool KeyExists(string hashKey, string hashField)
    {
        try
        {
            return !string.IsNullOrEmpty(hashField)
                ? GetDatabase(IsReadConnected()).HashExists(hashKey, hashField)
                : GetDatabase(IsReadConnected()).KeyExists(hashKey);
        }
        catch (Exception ex) { Log.Information("RedisManager.KeyExists.Exception: {Message}", ex.Message); return false; }
    }

    private void SendMessageSms(string type, string message, string title = "")
    {
        if (string.IsNullOrEmpty(title)) title = "WebApi Lỗi kết nối Redis";
        _ = _sms.SendMessageSmsAsync(message, type, title, appCode: "BLUEPOS_REDIS_CONNECT");
    }
}
