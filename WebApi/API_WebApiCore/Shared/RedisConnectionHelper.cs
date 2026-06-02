using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.DbContext;

namespace TCX.WebApiCore
{
    public class RedisConnectionHelper
    {
        private readonly IDatabase _dbCacheRead;
        private readonly IDatabase _dbCacheWrite;
        public RedisConnectionHelper()
        {
            _dbCacheWrite = RedisManager.GetDatabase();
            _dbCacheRead = RedisManager.GetDatabase();
        }

        private static readonly Lazy<ConnectionMultiplexer> lazyConnectionRead = new Lazy<ConnectionMultiplexer>(() =>
        {
            string connectionString = ConfigurationManager.ConnectionStrings["RedisConnectionDefault"].ConnectionString;
            ConfigurationOptions options = ConfigurationOptions.Parse(connectionString);
            options.SocketManager = new SocketManager("CustomSocketManager", workerCount: 100);

            return ConnectionMultiplexer.Connect(options);
        });
        private static readonly Lazy<ConnectionMultiplexer> lazyConnectionWrite = new Lazy<ConnectionMultiplexer>(() =>
        {
            return ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnectionDefault"].ConnectionString);
        });
        private static ConnectionMultiplexer ConnectionDBWrite
        {
            get
            {
                if (RedisSentinelConnection.IsRedisSentinel())
                {
                    return RedisManager.GetConnection();
                }
                return lazyConnectionWrite.Value;
            }
        }
        private static ConnectionMultiplexer ConnectionRead
        {
            get
            {
                if (RedisSentinelConnection.IsRedisSentinel())
                {
                    return RedisSentinelConnection.GetConnection();
                }
                return lazyConnectionRead.Value;
            }
        }
        public T Set<T>(string key, T value, string prefix = null, TimeSpan? timeOut = null)
        {
            if (!string.IsNullOrEmpty(prefix))
            {
                key = prefix + ":" + key;
            }
            if (timeOut == null)
            {
                timeOut = DateTimeHelper.GetTimeUntilHourInTomorrow(1);
            }
            return _dbCacheWrite.StringSet(key, JsonConvert.SerializeObject(value), timeOut) ? value : default;
        }
        public T Get<T>(string key)
        {
            var value = _dbCacheRead.StringGet(key);
            return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
        }
        public T GetAsync<T>(string key)
        {
            var value = _dbCacheRead.StringGetAsync(key).Result;
            return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
        }
        public T GetLastKey<T>(string patern_key, string keyContain) 
        {
            try
            {
                var keys = GetKeys(patern_key);
                if (keys.Count > 0)
                {
                    var latestKey = keys.OrderByDescending(key => key.ToString().Contains(keyContain)).FirstOrDefault();
                    if (!string.IsNullOrEmpty(latestKey))
                    {
                        return Get<T>(latestKey);
                    }
                    else
                    {
                        return default;
                    }
                }
                return default;
            }
            catch
            {
                return default;
            }
        }
        public bool Delete(string key)
        {
            if (CheckExistsKey(key))
                return _dbCacheWrite.KeyDelete(key);

            return false;
        }
        public bool DeleteWithParent(string key, string parent)
        {
            try
            {
                string key_full = parent + ":" + key;
                if(CheckExistsKey(key_full))
                    return _dbCacheWrite.KeyDelete(key_full);

                return false;
            }
            catch
            {
                return false;
            }
        }
        public void DelAllKeyWithParent(string keyContain, string parent)
        {
            try
            {
                var lstKey = GetKeys(parent);
                if (lstKey.Count > 0)
                {
                    foreach (var key in lstKey)
                    {
                        if (key.Contains(keyContain))
                        {
                            Delete(key);
                        }
                    }
                }
            }
            catch
            {

            }

        }
        public List<T> Find<T>(string patern_key)
        {
            var block_list = new System.Collections.Concurrent.BlockingCollection<T>();
            try
            {
                foreach (var ep in ConnectionDBWrite.GetEndPoints())
                {
                    var server = ConnectionDBWrite.GetServer(ep);
                    var keys = server.Keys(pattern: patern_key + "*").ToArray();
                    var values = _dbCacheRead.StringGet(keys);

                    Parallel.ForEach(values, (value, state) =>
                    {
                        block_list.Add(JsonConvert.DeserializeObject<T>(value));
                    });
                }
                return block_list.ToList();
            }
            finally
            {
                block_list?.Dispose();
                block_list = null;
            }
        }
        public List<string> GetKeys(string patern_key)
        {
            try
            {
                List<string> result = new List<string>();
                foreach (var ep in ConnectionRead.GetEndPoints())
                {
                    var server = ConnectionRead.GetServer(ep);
                    var keys = server.Keys(pattern: patern_key + "*").ToArray();
                    if (keys.Count() > 0)
                    {
                        foreach (var item in keys)
                        {
                            result.Add(item);
                        }
                    }
                }
                return result;
            }
            finally
            {

            }
        }

        //List Index
        public void UpdateDataByIndex(string key, long index, string value)
        {
            _dbCacheWrite.ListSetByIndex(key, index, value);
        }
        public void SetListData(string key, string value)
        {
            _dbCacheWrite.ListRightPush(key, value);
        }
        public RedisValue[] GetListData(string key)
        {
            return _dbCacheRead.ListRange(key);
        }

        //Hash
        public void HashSetIList(string key, long index, string value)
        {
            _dbCacheWrite.HashSet(key,index, value);   
        }
        public T HashGetValueIList<T>(string key, long index)
        {
            var value = _dbCacheRead.HashGet(key, index);
            return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
        }
        public bool HashDeleteByIndex(string key, long index)
        {
           return _dbCacheWrite.HashDelete(key, index);
        }
        public bool CheckExistsKey(string key)
        {
            return _dbCacheWrite.KeyExists(key);
        }
        public bool StringKeyDelete(string key)
        {
            var value = _dbCacheWrite.StringGet(key);
            if (value.IsNull) return false;

            _dbCacheWrite.KeyDelete(key);
            return true;
        }
    }
}
