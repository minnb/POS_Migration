using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web.Configuration;
using TCX.API.Common.Helpers;

namespace VCM.POSBLUE.Data.Redis
{
    public class RedisSentinelData
    {
        private static string _masterName = "mymaster";
        private static readonly object _lock = new object();
        private static ConnectionMultiplexer _redis;
        private static DateTime _connectionTime;
        private static TimeSpan _connectionTTL = TimeSpan.FromSeconds(30); // TTL: 30s
        private static string[] _sentinels = ConfigurationManager.ConnectionStrings["RedisSentinels"].ConnectionString
                                                                                        .Split(',')
                                                                                        .Select(s => s.Trim())
                                                                                        .Where(s => !string.IsNullOrEmpty(s)).ToArray();

        private static ConnectionMultiplexer _redisReplica;
        private static DateTime _replicaConnectionTime;
        private static readonly object _replicaLock = new object();
        private static TimeSpan _replicaTTL = TimeSpan.FromSeconds(120);
        public static ConnectionMultiplexer GetConnection()
        {
            if (_redis == null || DateTime.UtcNow - _connectionTime > _connectionTTL || !_redis.IsConnected)
            {
                lock (_lock)
                {
                    if (_redis == null || DateTime.UtcNow - _connectionTime > _connectionTTL || !_redis.IsConnected)
                    {
                        _redis?.Dispose();
                        _redis = ConnectToMaster();
                        _connectionTime = DateTime.UtcNow;
                    }
                }
            }
            return _redis;
        }
        public static ConnectionMultiplexer GetConnectionReplica()
        {
            if (_redisReplica == null || DateTime.UtcNow - _replicaConnectionTime > _replicaTTL || !_redisReplica.IsConnected)
            {
                lock (_replicaLock)
                {
                    if (_redisReplica == null || DateTime.UtcNow - _replicaConnectionTime > _replicaTTL || !_redisReplica.IsConnected)
                    {
                        _redisReplica?.Dispose();
                        var replicas = GetReplicaEndpointsFromSentinel(_sentinels, _masterName);
                        if (replicas.Count > 0)
                        {
                            var random = new Random();
                            for (int i = 0; i < replicas.Count; i++)
                            {
                                var idx = random.Next(replicas.Count);
                                var replicaEndpoint = replicas[idx];

                                var replicaConfig = new ConfigurationOptions
                                {
                                    EndPoints = { replicaEndpoint },
                                    AbortOnConnectFail = false
                                };

                                try
                                {
                                    var conn = ConnectionMultiplexer.Connect(replicaConfig);
                                    if (conn.IsConnected)
                                    {
                                        _redisReplica = conn;
                                        _replicaConnectionTime = DateTime.UtcNow;
                                        return _redisReplica;
                                    }
                                }
                                catch { }
                            }
                        }
                        // not found replica, fallback to master
                        _redisReplica = GetConnection();
                        _replicaConnectionTime = DateTime.UtcNow;
                    }
                }
            }
            return _redisReplica;
        }

        private static ConnectionMultiplexer ConnectToMaster()
        {
            try
            {
                var sentinelConfig = new ConfigurationOptions
                {
                    CommandMap = CommandMap.Sentinel,
                    AbortOnConnectFail = false,
                    AllowAdmin = true
                };
                foreach (var endpoint in _sentinels)
                    sentinelConfig.EndPoints.Add(endpoint);

                using (var sentinel = ConnectionMultiplexer.Connect(sentinelConfig))
                {
                    var masterEndpoint = GetSentinelMasterAddressByName(sentinel, _masterName);
                    var redisConfig = new ConfigurationOptions
                    {
                        EndPoints = { masterEndpoint },
                        AbortOnConnectFail = false
                        //Password = _redisPassword
                    };
                    var redis = ConnectionMultiplexer.Connect(redisConfig);

                    redis.ConnectionFailed += (sender, args) =>
                    {
                        Console.WriteLine("[RedisSentinel] Connection failed: " + args.Exception?.Message);
                        lock (_lock)
                        {
                            _redis?.Dispose();
                            _redis = ConnectToMaster();
                            _connectionTime = DateTime.UtcNow;
                        }
                    };

                    return redis;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs("[RedisSentinel] ConnectToMaster error: " + ex.Message);
                throw;
            }
        }

        private static EndPoint GetSentinelMasterAddressByName(ConnectionMultiplexer sentinel, string masterName)
        {
            foreach (var endpoint in sentinel.GetEndPoints())
            {
                try
                {
                    var server = sentinel.GetServer(endpoint);
                    var master = server.SentinelGetMasterAddressByName(masterName);
                    if (master != null)
                        return master;
                }
                catch (Exception ex)
                {
                    FileHelper.WriteLogs($"GetSentinelMasterAddressByName.Exception {ex.Message}");
                }
            }
            FileHelper.WriteLogs($"Cannot get master address for '{masterName}' from any sentinel.");
            throw new Exception($"Cannot get master address for '{masterName}' from any sentinel.");
        }

        public static List<EndPoint> GetReplicaEndpointsFromSentinel(string[] sentinelEndpoints, string masterName)
        {
            var replicas = new List<EndPoint>();
            var sentinelConfig = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                AbortOnConnectFail = false,
                AllowAdmin = true
            };
            foreach (var endpoint in sentinelEndpoints)
                sentinelConfig.EndPoints.Add(endpoint);

            using (var sentinel = ConnectionMultiplexer.Connect(sentinelConfig))
            {
                foreach (var endpoint in sentinel.GetEndPoints())
                {
                    try
                    {
                        var server = sentinel.GetServer(endpoint);
                        // Thực thi lệnh trực tiếp với Sentinel để lấy thông tin các slave
                        // Lệnh: SENTINEL SLAVES <masterName>
                        var result = server.Execute("SENTINEL", "SLAVES", masterName);
                        if (result.Type == ResultType.MultiBulk)
                        {
                            foreach (var item in (RedisResult[])result)
                            {
                                var dict = new Dictionary<string, string>();
                                var slaveData = (RedisResult[])item;
                                for (int i = 0; i < slaveData.Length; i += 2)
                                {
                                    var key = slaveData[i].ToString();
                                    var val = slaveData[i + 1].ToString();
                                    dict[key] = val;
                                }
                                if (dict.TryGetValue("ip", out string ip) && dict.TryGetValue("port", out string portStr) && int.TryParse(portStr, out int port))
                                {
                                    replicas.Add(new DnsEndPoint(ip, port));
                                }
                            }
                        }
                        if (result.Resp2Type == ResultType.Array)
                            if (replicas.Count > 0)
                                break; // Nếu đã lấy được thì không cần query các Sentinel khác
                    }
                    catch
                    {
                        /* Sentinel có thể offline, thử Sentinel khác */
                    }
                }
            }
            return replicas;
        }
        public static bool IsRedisSentinel()
        {
            try
            {
                if (WebConfigurationManager.AppSettings["RedisActive"].ToUpper() == "SENTINELS")
                {
                    return true;
                }
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
