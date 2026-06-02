using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

public class RedisHelper
{
    private readonly IDatabase _dbCache;
    public RedisHelper()
    {
        _dbCache = Connection.GetDatabase();
    }
    private static readonly Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
    {
        Random rand = new Random();
        int number = rand.Next(2);
        if (number == 1)
        {
            return ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnectionSecond"].ConnectionString);
        }
        return ConnectionMultiplexer.Connect(ConfigurationManager.ConnectionStrings["RedisConnectionDefault"].ConnectionString);
    });

    private static ConnectionMultiplexer Connection
    {
        get
        {
            return lazyConnection.Value;
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
            timeOut = TimeSpan.FromHours(12);
        }

        return _dbCache.StringSet(key, JsonConvert.SerializeObject(value), timeOut) ? value : default;
    }
    public T Get<T>(string key)
    {
        var value = _dbCache.StringGet(key);
        return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
    }
    public T GetAsync<T>(string key)
    {
        var value = _dbCache.StringGetAsync(key).Result;
        return value.IsNull ? default : JsonConvert.DeserializeObject<T>(value);
    }
    public bool Delete(string key)
    {
        return _dbCache.KeyDelete(key);
    }
    public List<T> Find<T>(string patern_key)
    {
        var block_list = new System.Collections.Concurrent.BlockingCollection<T>();
        try
        {
            foreach (var ep in Connection.GetEndPoints())
            {
                var server = Connection.GetServer(ep);
                var keys = server.Keys(pattern: patern_key + "*").ToArray();
                var values = _dbCache.StringGet(keys);

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

            foreach (var ep in Connection.GetEndPoints())
            {
                var server = Connection.GetServer(ep);
                var keys = server.Keys(pattern: patern_key + "*").ToArray();
                if(keys.Count() > 0)
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
}