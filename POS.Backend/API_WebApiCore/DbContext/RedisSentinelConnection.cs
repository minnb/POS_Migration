using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Configuration;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;

namespace TCX.WebApiCore.DbContext
{
    public class RedisSentinelConnection
    {
        private static string _connectionString;
        private static readonly string _masterName = "mymaster";
        private static readonly object _lock = new object();
        private static ConnectionMultiplexer _redis;
        private static DateTime _connectionTime;
        private static TimeSpan _connectionTTL = TimeSpan.FromSeconds(60);
        private static TimeSpan _replicaTTL = TimeSpan.FromSeconds(60);
        private static readonly int connectTimeout = 4000;
        private static readonly int syncTimeout = 3000;

        private static ConnectionMultiplexer _redisReplica;
        private static DateTime _replicaConnectionTime;
        private static readonly object _replicaLock = new object();
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
        private static string[] Sentinels()
        {
            return _connectionString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();
        } 
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
                        _redisReplica = ConnectToReplicaWithFastFallback();
                        _replicaConnectionTime = DateTime.UtcNow;
                    }
                }
            }
            return _redisReplica;
        }

        // Circuit breaker tracking
        private static Dictionary<string, DateTime> _failedReplicas = new Dictionary<string, DateTime>();
        private static readonly int _replicaBlacklistSeconds = 30;
        private static readonly int _replicaConnectTimeout = 1500; // Giảm từ 4000ms → 1500ms
        private static readonly int _maxReplicaWaitTime = 2000; // Tối đa chờ 2 giây

        private static ConnectionMultiplexer ConnectToReplicaWithFastFallback()
        {
            var replicas = GetReplicaEndpointsFromSentinel(Sentinels(), _masterName);
            
            if (replicas.Count == 0)
            {
                FileHelper.WriteLogs("[RedisSentinel] No replicas found, using master");
                return GetConnection();
            }

            // Lọc bỏ các replicas đang bị blacklist
            var now = DateTime.UtcNow;
            var availableReplicas = replicas
                .Where(r => !_failedReplicas.ContainsKey(r.ToString()) || 
                            (now - _failedReplicas[r.ToString()]).TotalSeconds > _replicaBlacklistSeconds)
                .ToList();

            if (availableReplicas.Count == 0)
            {
                FileHelper.WriteLogs("[RedisSentinel] All replicas blacklisted, using master");
                return GetConnection();
            }

            // Thử connect song song tất cả replicas
            var tasks = availableReplicas.Select(endpoint => 
                Task.Run(() => TryConnectToReplica(endpoint))
            ).ToArray();

            try
            {
                // Chờ tối đa 2 giây, lấy task hoàn thành đầu tiên
                var timeoutTask = Task.Delay(_maxReplicaWaitTime);
                var completedTask = Task.WhenAny(tasks.Concat(new[] { timeoutTask })).Result;

                if (completedTask == timeoutTask)
                {
                    FileHelper.WriteLogs($"[RedisSentinel] All replicas timeout after {_maxReplicaWaitTime}ms, using master");
                    return GetConnection();
                }

                // Kiểm tra kết quả của các tasks
                foreach (var task in tasks.Where(t => t.IsCompleted))
                {
                    var result = task.Result;
                    if (result != null && result.IsConnected)
                    {
                        FileHelper.WriteLogs("[RedisSentinel] Connected to replica successfully");
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"[RedisSentinel] ConnectToReplicaWithFastFallback error: {ex.Message}");
            }

            // Fallback về master
            FileHelper.WriteLogs("[RedisSentinel] All replicas failed, using master");
            return GetConnection();
        }

        private static ConnectionMultiplexer TryConnectToReplica(EndPoint endpoint)
        {
            var replicaConfig = new ConfigurationOptions
            {
                EndPoints = { endpoint },
                ConnectTimeout = _replicaConnectTimeout, // 1500ms thay vì 4000ms
                SyncTimeout = _replicaConnectTimeout,
                AbortOnConnectFail = true, // Fail nhanh
                ConnectRetry = 1 // Chỉ retry 1 lần
            };

            try
            {
                var conn = ConnectionMultiplexer.Connect(replicaConfig);
                if (conn.IsConnected)
                {
                    // Xóa khỏi blacklist nếu connect thành công
                    if (_failedReplicas.ContainsKey(endpoint.ToString()))
                    {
                        _failedReplicas.Remove(endpoint.ToString());
                    }
                    return conn;
                }
                else
                {
                    conn?.Dispose();
                    MarkReplicaAsFailed(endpoint);
                    return null;
                }
            }
            catch (Exception ex)
            {
                FileHelper.WriteLogs($"[RedisSentinel] Failed to connect to replica {endpoint}: {ex.Message}");
                MarkReplicaAsFailed(endpoint);
                return null;
            }
        }

        private static void MarkReplicaAsFailed(EndPoint endpoint)
        {
            var key = endpoint.ToString();
            if (!_failedReplicas.ContainsKey(key))
            {
                _failedReplicas[key] = DateTime.UtcNow;
            }
            else
            {
                _failedReplicas[key] = DateTime.UtcNow;
            }
        }

        private static ConnectionMultiplexer ConnectToMaster()
        {
            try
            {
                var sentinelConfig = new ConfigurationOptions
                {
                    CommandMap = CommandMap.Sentinel,
                    ConnectTimeout = connectTimeout,
                    AsyncTimeout = syncTimeout,
                    AbortOnConnectFail = false,
                    AllowAdmin = true
                };
                foreach (var endpoint in Sentinels())
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

        [Obsolete]
        public static List<EndPoint> GetReplicaEndpointsFromSentinel(string[] sentinelEndpoints, string masterName)
        {
            var replicas = new List<EndPoint>();
            var sentinelConfig = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                ConnectTimeout = connectTimeout,
                AsyncTimeout = syncTimeout,
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
                            break;
                    }
                    catch (Exception ex)
                    {
                        /* Sentinel offline, try Sentinel other */
                        FileHelper.WriteExpLogs("GetReplicaEndpointsFromSentinel", ex);
                    }
                }
            }
            return replicas;
        }
        public static bool IsRedisSentinel()
        {
            try
            {
                if (!string.IsNullOrEmpty(AppGlobals.RedisActive) && AppGlobals.RedisActive.ToUpper() == "SENTINELS")
                {
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("IsRedisSentinel.Exception:", ex);
                return false;
            }
        }
    }
}
