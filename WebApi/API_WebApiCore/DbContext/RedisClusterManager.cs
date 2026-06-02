using Confluent.Kafka;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.DbContext
{
    public class RedisClusterManager
    {
        private static ConnectionMultiplexer _connection;
        private static readonly object _lock = new object();
        private static bool _isInitialized = false;
        private readonly KibanaService _kibanaService;
        public RedisClusterManager() 
        {
            _kibanaService = new KibanaService();
        }
        public static ConnectionMultiplexer GetConnection()
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
            //if (!_isInitialized)
            //{
            //    lock (_lock)
            //    {
            //        if (!_isInitialized)
            //        {
            //            //var configurationOptions = new ConfigurationOptions();
            //            //var endpoints = ConfigurationManager.ConnectionStrings["RedisCluster"].ConnectionString;
            //            //foreach (var endpoint in endpoints.Split(','))
            //            //{
            //            //    configurationOptions.EndPoints.Add(endpoint);
            //            //}
            //            //configurationOptions.Password = "Msn@2024#redis";
            //            //configurationOptions.AllowAdmin = true;
            //            //configurationOptions.AbortOnConnectFail = false;
            //            //configurationOptions.ConnectTimeout = 5000;
            //            //configurationOptions.SyncTimeout = 5000;
            //            //configurationOptions.ConnectRetry = 3;

            //            //configurationOptions.CommandMap = CommandMap.Create(new HashSet<string>
            //            //    {
            //            //        "CLUSTER", "READONLY", "READWRITE", "ASKING"
            //            //    }, available: true);

            //            //_connection = ConnectionMultiplexer.Connect(configurationOptions);
            //        }
            //    }
            //}
            //return _connection;
        }
        public IDatabase GetDatabase(int dbIndex = -1)
        {
            try
            {
                if (RedisSentinelConnection.IsRedisSentinel())
                {
                    return RedisSentinelConnection.GetConnection().GetDatabase(dbIndex);
                }
                return GetConnection().GetDatabase(dbIndex);
            }
            catch (Exception ex)
            {
                _kibanaService.SendMessageSMS($"RedisClusterManager.GetDatabase() **lỗi kết nối Redis**: {ex.Message}", "Error", $"**WebApi BLUEPOS** lỗi kết nối Redis");
                return null;
            }
        }
        private IServer GetServer()
        {
            var connection = GetConnection();
            return connection.GetServer(connection.GetEndPoints().First());
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
                FileHelper.WriteLogs($"RedisClusterManager.GetAllKeys.Exception: {ex.Message}");
            }
            return keys;
        }
        public bool StringSet<T>(string key, T value, long timeSeconds = 0)
        {
            try
            {
                if (timeSeconds == 0) 
                {
                    timeSeconds = DateTimeHelper.GetSecondsUntilMidnight();
                }
                GetDatabase().StringSet(key, JsonConvert.SerializeObject(value), TimeSpan.FromSeconds(timeSeconds));
                return true;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.StringSet.Exception: {ex.Message}");
                _kibanaService.SendMessageSMS($"RedisClusterManager.Exception: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis");
                return false;
            }
        }
        public T StringGet<T>(string key)
        {
            try
            {
                var data = GetDatabase().StringGet(key);
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.StringGet.Exception: {JsonConvert.SerializeObject(ex)}");
                _kibanaService.SendMessageSMS($"Lỗi kết nối Redis: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis");
                return default;
            }
        }
        public bool StringDelete(string key)
        {
            try
            {
                return GetDatabase().KeyDelete(key);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.Delete.Exception: {ex.Message}");
                return false;
            }
        }
        public bool HashSet<T>(string hashKey, string hashField, T hashValue, long timeSeconds = 0, bool isExpire = true)
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
                FileHelper.WriteExpLogs($"{hashKey}:{hashField} HashSet.Exception:", ex);
                _kibanaService.SendMessageSMS($"RedisClusterManager.HashSet.Exception: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis");
                return false;
            }
        }
        public T HashGet<T>(string hashKey, string hashField)
        {
            try
            {
                var data = GetDatabase().HashGet(hashKey, hashField);
                if (!string.IsNullOrEmpty(data))
                {
                    return ConvertHelper.ToObject<T>(data);
                }
                return default;
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.HashGet.Exception: {ex.Message}");
                _kibanaService.SendMessageSMS($"RedisClusterManager.HashGet.Exception: {ex.Message}", "Error", "WebApi BluePOS lỗi kết nối Redis");
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
                if(!string.IsNullOrEmpty(hashField))
                {
                    return GetDatabase().HashExists(hashKey, hashField);
                }
                return GetDatabase().KeyExists(hashKey);
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"RedisClusterManager.KeyExists.Exception: {ex.Message}");
                return false;
            }
        }
    }
}