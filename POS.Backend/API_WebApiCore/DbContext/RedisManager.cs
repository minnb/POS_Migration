using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TCX.API.Common.Enums;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.DbContext
{
    public class RedisManager
    {
        private static ConnectionMultiplexer _connection;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;
        private static long _requestCounter = 0;
        public RedisManager() { }
        public static ConnectionMultiplexer GetConnection()
        {
            if (RedisSentinelConnection.IsRedisSentinel())
            {
                return RedisSentinelConnection.GetConnection();
            }
            else
            {
                if (!_isInitialized)
                {
                    lock (_lock)
                    {
                        if (!_isInitialized)
                        {
                            _connection = ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnectionDefault"].ConnectionString);
                            _isInitialized = true;
                        }
                    }
                }
                return _connection;
            }
        }
        public static ConnectionMultiplexer GetConnectionDBRead()
        {
            if (RedisSentinelConnection.IsRedisSentinel())
            {
                return RedisSentinelConnection.GetConnectionReplica();
            }
            else
            {
                if (!_isInitialized)
                {
                    lock (_lock)
                    {
                        if (!_isInitialized)
                        {
                            _connection = ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnectionSecond"].ConnectionString);
                            _isInitialized = true;
                        }
                    }
                }
                return _connection;
            }
        }
        private static bool IsReadConnected()
        {
            var counter = Interlocked.Increment(ref _requestCounter);
            return counter % 2 == 0;
        }
        // Trong RedisManager.cs - dòng 70-77
        public static IDatabase GetDatabase(bool isRead = false, int dbIndex = -1)
        {
            if (isRead && RedisSentinelConnection.IsRedisSentinel())
            {
                return GetConnectionDBRead().GetDatabase(dbIndex);
            }
            if (isRead)
            {
                return GetConnectionDBRead().GetDatabase(dbIndex);
            }
            return GetConnection().GetDatabase(dbIndex);
        }
        private IServer GetServer()
        {
            var connection = GetConnection();
            return connection.GetServer(connection.GetEndPoints().First());
        }
        public bool Set(string key, string value, int fromSeconds)
        {
            try
            {
                var db = GetDatabase();
                db.StringSet(key, value, TimeSpan.FromSeconds(fromSeconds));
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.SetCacheValue.Exception: {ex.Message}");
                return false;
            }
        }
        public void SetCacheValue(string key, string value, int fromSeconds)
        {
            try
            {
                var db = GetDatabase();
                db.StringSet(key, value, TimeSpan.FromSeconds(fromSeconds));
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.SetCacheValue.Exception: {ex.Message}");
            }
        }
        public string GetCacheValue(string key)
        {
            try
            {
                var db = GetDatabase(IsReadConnected());
                return db.StringGet(key);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.GetCacheValue.Exception: {ex.Message}");
                return null;
            }
        }
        public T GetCacheValueToObject<T>(string key)
        {
            try
            {
                var data = GetCacheValue(key);
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.GetCacheValue.Exception: {ex.Message}");
                return default;
            }
        }
        public void Delete(string key)
        {
            try
            {
                var db = GetDatabase();
                db.KeyDelete(key);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.DeleteCache.Exception: {ex.Message}");
            }
        }
        public void DeleteKeys(List<string> keys)
        {
            try
            {
                var db = GetDatabase();
                foreach (string key in keys)
                {
                    db.KeyDelete(key);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.DeleteCache.Exception: {ex.Message}");
            }
        }
        public List<string> GetAllKeys(string pattern, string keyExclue = "")
        {
            var keys = new List<string>();
            try
            {
                pattern += ":*";
                var server = GetServer();
                var db = GetDatabase();
                foreach (var key in server.Keys(database: db.Database, pattern: pattern))
                {
                    if (!string.IsNullOrEmpty(keyExclue) && key.ToString().ToUpper() == keyExclue.ToUpper())
                    {
                        continue;
                    }
                    keys.Add(key);
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.GetAllKeys.Exception: {ex.Message}");
            }
            return keys;
        }
        public bool CheckKeyExists(string key)
        {
            try
            {
                return GetDatabase(IsReadConnected()).KeyExists(key);
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"RedisClusterManager.CheckKeyExists.Exception", ex);
                return false;
            }
        }

        //same RedisCluster
        public bool StringSet<T>(string key, T value, long timeSeconds = 0, bool expiry = true)
        {
            try
            {
                if (timeSeconds == 0)
                {
                    timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
                }
                if (expiry && timeSeconds > 0)
                {
                    GetDatabase().StringSet(key, JsonConvert.SerializeObject(value), TimeSpan.FromSeconds(timeSeconds));
                }
                else
                {
                    GetDatabase().StringSet(key, JsonConvert.SerializeObject(value));
                }
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.SetCacheValue.Exception: {ex.Message}");
                SendMessageSMS("Warning", $"RedisManager.StringSet({key}).Exception: {ex.Message}", ex.Message);
                return false;
            }
        }
        public async Task<string> GetStringAsync(string key)
        {
            if (!GetDatabase(IsReadConnected()).KeyExists(key))
            {
                return default;
            }
            RedisValue redisValue;
            if (RedisSentinelConnection.IsRedisSentinel())
            {
                //read from replica
                redisValue = await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica);
            }
            else
            {
                redisValue = GetDatabase(IsReadConnected()).StringGet(key);
            }

            if (!string.IsNullOrEmpty(redisValue))
            {
                return redisValue;
            }
            return default;
        }
        public async Task<T> StringGetAsync<T>(string key)
        {
            try
            {
                if (!GetDatabase(IsReadConnected()).KeyExists(key))
                {
                    return default;
                }
                RedisValue redisValue = default;
                if (RedisSentinelConnection.IsRedisSentinel())
                {
                    //read from replica
                    redisValue = await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica);
                }
                else
                {
                    redisValue = GetDatabase(IsReadConnected()).StringGet(key);
                }

                if (!string.IsNullOrEmpty(redisValue))
                {
                    return ConvertHelper.ToObject<T>(redisValue);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.StringGet.Exception: {ex.Message}");
                SendMessageSMS("Warning", $"RedisManager.StringGet({key}).Exception: {ex.Message}");
                return default;
            }
        }
        public bool StringSetString(string key, string value, long timeSeconds = 0, bool expiry = true)
        {
            try
            {
                if (timeSeconds == 0)
                {
                    timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
                }
                if (expiry && timeSeconds > 0)
                {
                    GetDatabase().StringSet(key, value, TimeSpan.FromSeconds(timeSeconds));
                }
                else
                {
                    GetDatabase().StringSet(key, value);
                }
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.SetCacheValue.Exception: {ex.Message}");
                SendMessageSMS("Warning", $"RedisManager.StringSet({key}).Exception: {ex.Message}", ex.Message);
                return false;
            }
        }
        public async Task<string> GetIsOfflineCapillary(string key)
        {
            try
            {
                if (RedisSentinelConnection.IsRedisSentinel())
                {
                    //read from replica
                    return await GetDatabase(isRead: true).StringGetAsync(key, CommandFlags.PreferReplica);
                }
                return await GetDatabase().StringGetAsync(key);
            }
            catch (Exception ex)
            {
                // Auto fallback
                FileHelper.WriteExpLogs($"GetIsOfflineCapillary: {key}", ex);
                SendMessageSMS(MessageTypeEnum.Error.ToString(), $"RedisManager.GetIsOfflineCapillary({key}).Exception: {ex.Message}", ex.Message);
                return await GetDatabase().StringGetAsync(key);
            }
        }
        public bool HashSet<T>(string hashKey, string hashField, T hashValue, long timeSeconds = 0)
        {
            try
            {
                if (timeSeconds == 0)
                {
                    timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
                }
                var tran = GetDatabase().CreateTransaction();
                var setTask = tran.HashSetAsync(hashKey, hashField, JsonConvert.SerializeObject(hashValue));
                var expireTask = tran.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(timeSeconds));
                var success = tran.ExecuteAsync().Result;
                if (success)
                {
                    var verification = GetDatabase().HashExistsAsync(hashKey, JsonConvert.SerializeObject(hashValue)).Result;
                    if (!verification)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs($"{hashKey}:{hashField} HashSet.Exception:", ex);
                Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message));
                return false;
            }
        }
        public T HashGet<T>(string hashKey, string hashField, bool isServerRead = false)
        {
            try
            {
                if (string.IsNullOrEmpty(hashField))
                {
                    return default;
                }

                RedisValue data = default;
                if (isServerRead && RedisSentinelConnection.IsRedisSentinel())
                {
                    data = GetDatabase(isRead: isServerRead).HashGet(hashKey, hashField, CommandFlags.PreferReplica);
                }
                else
                {
                    data = GetDatabase(isRead: isServerRead).HashGet(hashKey, hashField);
                }
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.HashGet.Exception: {ex.Message}");
                Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message));
                return default;
            }
        }
        public async Task<T> HashGetAsync<T>(string hashKey, string hashField, bool isServerRead = false)
        {
            try
            {
                if (string.IsNullOrEmpty(hashField))
                {
                    return default;
                }
                RedisValue data = default;
                if (isServerRead && RedisSentinelConnection.IsRedisSentinel())
                {
                    data = await GetDatabase(isRead: isServerRead).HashGetAsync(hashKey, hashField, CommandFlags.PreferReplica);
                }
                else
                {
                    data = await GetDatabase(isRead: isServerRead).HashGetAsync(hashKey, hashField);
                }
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisManager.HashGetAsync.Exception: {ex.Message}");
                Task.Run(() => HttpClientService.SendMessageSMS($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis", ex.Message));
                return default;
            }
        }
        public bool HashDelete(string hashKey, string hashField)
        {
            try
            {
                return GetDatabase().HashDelete(hashKey, hashField);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.HashDelete.Exception: {ex.Message}");
                return false;
            }
        }
        public bool KeyExists(string hashKey, string hashField)
        {
            try
            {
                if (!string.IsNullOrEmpty(hashField))
                {
                    return GetDatabase(IsReadConnected()).HashExists(hashKey, hashField);
                }
                return GetDatabase(IsReadConnected()).KeyExists(hashKey);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.KeyExists.Exception: {ex.Message}");
                return false;
            }
        }
        private void SendMessageSMS(string type, string message, string title = "")
        {
            if (string.IsNullOrEmpty(title))
            {
                title = $"WebApi Lỗi kết nối Redis";
            }
            Task.Run(() => HttpClientService.SendMessageSMS(message, type, title, appCode: AppCodeMessageEnum.BLUEPOS_REDIS_CONNECT.ToString()));
        }
    }

}
