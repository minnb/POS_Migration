using System.Net;
using Microsoft.Extensions.Configuration;
using Serilog;
using StackExchange.Redis;

namespace VCM.POSBLUE.Infrastructure.Redis;

/// <summary>
/// Quản lý kết nối Redis qua Sentinel — port từ RedisSentinelConnection (static) của bản cũ.
/// Chuyển thành service injectable (đăng ký Singleton để giữ cache connection như static cũ).
/// Giữ NGUYÊN logic: connect master qua Sentinel, fast-fallback replica, circuit-breaker blacklist.
/// Thay System.Web.Configuration/AppGlobals/FileHelper bằng IConfiguration + Serilog.
/// </summary>
public sealed class RedisSentinelConnection
{
    private const string MasterName = "mymaster";
    private const int ConnectTimeout = 4000;
    private const int SyncTimeout = 3000;
    private const int ReplicaConnectTimeout = 1500;
    private const int MaxReplicaWaitTime = 2000;
    private const int ReplicaBlacklistSeconds = 30;

    private readonly string _connectionString;
    private readonly string _redisActive;

    private readonly object _lock = new();
    private readonly object _replicaLock = new();
    private readonly TimeSpan _connectionTtl = TimeSpan.FromSeconds(60);
    private readonly TimeSpan _replicaTtl = TimeSpan.FromSeconds(60);
    private readonly Dictionary<string, DateTime> _failedReplicas = new();

    private ConnectionMultiplexer? _redis;
    private DateTime _connectionTime;
    private ConnectionMultiplexer? _redisReplica;
    private DateTime _replicaConnectionTime;

    public RedisSentinelConnection(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("RedisSentinels") ?? "";
        _redisActive = configuration["AppSettings:RedisActive"] ?? "";
    }

    private string[] Sentinels() =>
        _connectionString.Split(',').Select(s => s.Trim()).Where(s => !string.IsNullOrEmpty(s)).ToArray();

    public bool IsRedisSentinel() =>
        !string.IsNullOrEmpty(_redisActive) && _redisActive.ToUpperInvariant() == "SENTINELS";

    public ConnectionMultiplexer GetConnection()
    {
        if (_redis == null || DateTime.UtcNow - _connectionTime > _connectionTtl || !_redis.IsConnected)
        {
            lock (_lock)
            {
                if (_redis == null || DateTime.UtcNow - _connectionTime > _connectionTtl || !_redis.IsConnected)
                {
                    _redis?.Dispose();
                    _redis = ConnectToMaster();
                    _connectionTime = DateTime.UtcNow;
                }
            }
        }
        return _redis;
    }

    public ConnectionMultiplexer GetConnectionReplica()
    {
        if (_redisReplica == null || DateTime.UtcNow - _replicaConnectionTime > _replicaTtl || !_redisReplica.IsConnected)
        {
            lock (_replicaLock)
            {
                if (_redisReplica == null || DateTime.UtcNow - _replicaConnectionTime > _replicaTtl || !_redisReplica.IsConnected)
                {
                    _redisReplica?.Dispose();
                    _redisReplica = ConnectToReplicaWithFastFallback();
                    _replicaConnectionTime = DateTime.UtcNow;
                }
            }
        }
        return _redisReplica;
    }

    private ConnectionMultiplexer ConnectToReplicaWithFastFallback()
    {
        var replicas = GetReplicaEndpointsFromSentinel(Sentinels(), MasterName);
        if (replicas.Count == 0)
        {
            Log.Information("[RedisSentinel] No replicas found, using master");
            return GetConnection();
        }

        var now = DateTime.UtcNow;
        var availableReplicas = replicas
            .Where(r => !_failedReplicas.ContainsKey(r.ToString()!) ||
                        (now - _failedReplicas[r.ToString()!]).TotalSeconds > ReplicaBlacklistSeconds)
            .ToList();

        if (availableReplicas.Count == 0)
        {
            Log.Information("[RedisSentinel] All replicas blacklisted, using master");
            return GetConnection();
        }

        var tasks = availableReplicas.Select(endpoint => Task.Run(() => TryConnectToReplica(endpoint))).ToArray();
        try
        {
            var timeoutTask = Task.Delay(MaxReplicaWaitTime);
            var completedTask = Task.WhenAny(tasks.Concat(new[] { timeoutTask })).Result;
            if (completedTask == timeoutTask)
            {
                Log.Information("[RedisSentinel] All replicas timeout after {Timeout}ms, using master", MaxReplicaWaitTime);
                return GetConnection();
            }

            foreach (var task in tasks.Where(t => t.IsCompleted))
            {
                var result = task.Result;
                if (result is { IsConnected: true })
                {
                    Log.Information("[RedisSentinel] Connected to replica successfully");
                    return result;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Information("[RedisSentinel] ConnectToReplicaWithFastFallback error: {Message}", ex.Message);
        }

        Log.Information("[RedisSentinel] All replicas failed, using master");
        return GetConnection();
    }

    private ConnectionMultiplexer? TryConnectToReplica(EndPoint endpoint)
    {
        var replicaConfig = new ConfigurationOptions
        {
            EndPoints = { endpoint },
            ConnectTimeout = ReplicaConnectTimeout,
            SyncTimeout = ReplicaConnectTimeout,
            AbortOnConnectFail = true,
            ConnectRetry = 1,
        };
        try
        {
            var conn = ConnectionMultiplexer.Connect(replicaConfig);
            if (conn.IsConnected)
            {
                _failedReplicas.Remove(endpoint.ToString()!);
                return conn;
            }
            conn.Dispose();
            MarkReplicaAsFailed(endpoint);
            return null;
        }
        catch (Exception ex)
        {
            Log.Information("[RedisSentinel] Failed to connect to replica {Endpoint}: {Message}", endpoint, ex.Message);
            MarkReplicaAsFailed(endpoint);
            return null;
        }
    }

    private void MarkReplicaAsFailed(EndPoint endpoint) => _failedReplicas[endpoint.ToString()!] = DateTime.UtcNow;

    private ConnectionMultiplexer ConnectToMaster()
    {
        try
        {
            var sentinelConfig = new ConfigurationOptions
            {
                CommandMap = CommandMap.Sentinel,
                ConnectTimeout = ConnectTimeout,
                AsyncTimeout = SyncTimeout,
                AbortOnConnectFail = false,
                AllowAdmin = true,
            };
            foreach (var endpoint in Sentinels())
                sentinelConfig.EndPoints.Add(endpoint);

            using var sentinel = ConnectionMultiplexer.Connect(sentinelConfig);
            var masterEndpoint = GetSentinelMasterAddressByName(sentinel, MasterName);
            var redisConfig = new ConfigurationOptions
            {
                EndPoints = { masterEndpoint },
                AbortOnConnectFail = false,
            };
            var redis = ConnectionMultiplexer.Connect(redisConfig);
            redis.ConnectionFailed += (_, args) =>
            {
                Log.Information("[RedisSentinel] Connection failed: {Message}", args.Exception?.Message);
                lock (_lock)
                {
                    _redis?.Dispose();
                    _redis = ConnectToMaster();
                    _connectionTime = DateTime.UtcNow;
                }
            };
            return redis;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RedisSentinel] ConnectToMaster error");
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
                if (master != null) return master;
            }
            catch (Exception ex)
            {
                Log.Information("GetSentinelMasterAddressByName.Exception {Message}", ex.Message);
            }
        }
        throw new Exception($"Cannot get master address for '{masterName}' from any sentinel.");
    }

    private List<EndPoint> GetReplicaEndpointsFromSentinel(string[] sentinelEndpoints, string masterName)
    {
        var replicas = new List<EndPoint>();
        var sentinelConfig = new ConfigurationOptions
        {
            CommandMap = CommandMap.Sentinel,
            ConnectTimeout = ConnectTimeout,
            AsyncTimeout = SyncTimeout,
            AbortOnConnectFail = false,
            AllowAdmin = true,
        };
        foreach (var endpoint in sentinelEndpoints)
            sentinelConfig.EndPoints.Add(endpoint);

        using var sentinel = ConnectionMultiplexer.Connect(sentinelConfig);
        foreach (var endpoint in sentinel.GetEndPoints())
        {
            try
            {
                var server = sentinel.GetServer(endpoint);
                var result = server.Execute("SENTINEL", "SLAVES", masterName);
                if (result.Resp2Type == ResultType.Array)
                {
                    foreach (var item in (RedisResult[])result!)
                    {
                        var dict = new Dictionary<string, string>();
                        var slaveData = (RedisResult[])item!;
                        for (int i = 0; i < slaveData.Length; i += 2)
                            dict[slaveData[i].ToString()!] = slaveData[i + 1].ToString()!;

                        if (dict.TryGetValue("ip", out var ip) &&
                            dict.TryGetValue("port", out var portStr) &&
                            int.TryParse(portStr, out var port))
                        {
                            replicas.Add(new DnsEndPoint(ip, port));
                        }
                    }
                }
                if (replicas.Count > 0) break;
            }
            catch (Exception ex)
            {
                Log.Information("GetReplicaEndpointsFromSentinel.Exception {Message}", ex.Message);
            }
        }
        return replicas;
    }
}
