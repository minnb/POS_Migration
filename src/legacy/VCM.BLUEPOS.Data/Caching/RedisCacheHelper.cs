using Newtonsoft.Json;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Configuration;

namespace VCM.BLUEPOS.Data.Caching
{
    public class RedisCacheHelper
    {
        private static Lazy<ConnectionMultiplexer> lazyConnection;
        private static readonly string envPrefix;

        static RedisCacheHelper()
        {
            // Default connection string if not found in AppSettings
            string redisConfiguration = ConfigurationManager.AppSettings["RedisCache"] ?? "10.235.52.189:6379,defaultDatabase=6";
            
            lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                return ConnectionMultiplexer.Connect(redisConfiguration);
            });

            // Get Environment (default to DEV)
            string env = ConfigurationManager.AppSettings["Environment"] ?? "DEV";
            
            // Format: [Environment]_[ProjectAbbreviation]
            envPrefix = $"PPOS_{env}_";
        }

        public static ConnectionMultiplexer Connection => lazyConnection.Value;

        public static IDatabase GetDatabase()
        {
            return Connection.GetDatabase();
        }

        public static string BuildKey(string cacheKey)
        {
            return envPrefix + cacheKey;
        }

        public static T Get<T>(string key)
        {
            try
            {
                var db = GetDatabase();
                var fullKey = BuildKey(key);
                var value = db.StringGet(fullKey);

                if (value.HasValue)
                {
                    return JsonConvert.DeserializeObject<T>(value);
                }
            }
            catch
            {
                // Redis unavailable � fall through to DB
            }

            return default(T);
        }

        public static void Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            try
            {
                var db = GetDatabase();
                var fullKey = BuildKey(key);
                var json = JsonConvert.SerializeObject(value);

                if (expiry.HasValue)
                    db.StringSet(fullKey, json, expiry.Value);
                else
                    db.StringSet(fullKey, json);
            }
            catch
            {
                // Redis unavailable � skip caching
            }
        }
    }

    public static class CacheKeys { public const string GrabFoodCategories = "GrabFoodCategories"; public const string GrabFoodToppings = "GrabFoodToppings"; public const string UOMs = "SetupItem_UOMs"; public const string Combos = "SetupItem_Combos"; public const string PosGroups = "SetupItem_PosGroups"; public const string POSVATCodes = "SetupItem_POSVATCodes"; public static string GrabFoodCheck(string itemNo) => "GrabFoodCheck_"; }
}

