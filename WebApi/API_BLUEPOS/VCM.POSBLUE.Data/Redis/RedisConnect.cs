using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;

namespace VCM.POSBLUE.Data.Redis
{
    public class RedisConnect
    {
        private static ConnectionMultiplexer _connection;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;
        public static ConnectionMultiplexer GetConnection()
        {
            if (RedisSentinelData.IsRedisSentinel())
            {
                return RedisSentinelData.GetConnection();
            }
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
        public static ConnectionMultiplexer GetConnectionDBRead()
        {
            if (RedisSentinelData.IsRedisSentinel())
            {
                return RedisSentinelData.GetConnectionReplica();
            }
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
        public static IDatabase GetDatabase(bool isRead = false, int dbIndex = -1)
        {
            if (isRead)
            {
                return GetConnectionDBRead().GetDatabase(dbIndex);
            }
            return GetConnection().GetDatabase(dbIndex);
        }
        public T HashGet<T>(string hashKey, string hashField, bool isServerRead = false)
        {
            try
            {
                if (string.IsNullOrEmpty(hashField))
                {
                    return default;
                }
                var data = GetDatabase(isRead: isServerRead).HashGet(hashKey, hashField);
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"{hashKey} RedisConnect.HashGet.Exception: {ex.Message}");
                return default;
            }
        }
        public bool HashSet<T>(string hashKey, string hashField, T hashValue, bool isExpire = true, long timeSeconds = 0)
        {
            try
            {
                if (timeSeconds == 0)
                {
                    timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
                }
                var tran = GetDatabase().CreateTransaction();
                var setTask = tran.HashSetAsync(hashKey, hashField, JsonConvert.SerializeObject(hashValue));
                if (isExpire)
                {
                    tran.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(timeSeconds));
                }
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
                FileHelper.WriteExpLogs($"{hashKey}:{hashField} RedisConnect.HashSet.Exception:", ex);
                return false;
            }
        }
    }
}
