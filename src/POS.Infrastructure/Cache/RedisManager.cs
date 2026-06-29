using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace POS.Infrastructure.Cache;

public sealed class RedisManager : IRedisManager
{
    private readonly RedisOptions _options;
    private readonly ILogger<RedisManager> _logger;
    private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

    private IDatabase Db => _lazyConnection.Value.GetDatabase(_options.DefaultDatabase);

    public RedisManager(IConfiguration configuration, ILogger<RedisManager> logger)
    {
        _options = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>() ?? new RedisOptions();
        _logger = logger;
        _lazyConnection = new Lazy<ConnectionMultiplexer>(CreateConnection, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    private ConnectionMultiplexer CreateConnection()
    {
        return _options.Mode.Equals("Sentinel", StringComparison.OrdinalIgnoreCase)
            ? CreateSentinelConnection()
            : CreateStandaloneConnection();
    }

    private ConnectionMultiplexer CreateSentinelConnection()
    {
        // Step 1: Connect to sentinel cluster to discover master
        var sentinelConfig = new ConfigurationOptions
        {
            CommandMap = CommandMap.Sentinel,
            AbortOnConnectFail = false,
            ConnectTimeout = _options.ConnectTimeout,
            AsyncTimeout = _options.SyncTimeout,
            AllowAdmin = true,
        };
        foreach (var host in _options.SentinelHosts)
            sentinelConfig.EndPoints.Add(host);

        using var sentinel = ConnectionMultiplexer.Connect(sentinelConfig);

        // Step 2: Query each sentinel for master address
        EndPoint? masterEndpoint = null;
        foreach (var ep in sentinel.GetEndPoints())
        {
            try
            {
                masterEndpoint = sentinel.GetServer(ep).SentinelGetMasterAddressByName(_options.MasterName);
                if (masterEndpoint != null) break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("[Redis] Sentinel {Endpoint} unavailable: {Message}", ep, ex.Message);
            }
        }

        if (masterEndpoint is null)
            throw new InvalidOperationException(
                $"[Redis] Cannot resolve master '{_options.MasterName}' from any sentinel.");

        _logger.LogInformation("[Redis] Sentinel resolved master at {Endpoint}", masterEndpoint);

        // Step 3: Connect directly to master
        var masterConfig = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = _options.ConnectTimeout,
            SyncTimeout = _options.SyncTimeout,
            DefaultDatabase = _options.DefaultDatabase,
        };
        if (!string.IsNullOrEmpty(_options.Password))
            masterConfig.Password = _options.Password;
        masterConfig.EndPoints.Add(masterEndpoint);

        var conn = ConnectionMultiplexer.Connect(masterConfig);
        WireEvents(conn);
        return conn;
    }

    private ConnectionMultiplexer CreateStandaloneConnection()
    {
        var host = _options.SentinelHosts.FirstOrDefault() ?? "localhost:6379";
        var config = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            ConnectTimeout = _options.ConnectTimeout,
            SyncTimeout = _options.SyncTimeout,
            DefaultDatabase = _options.DefaultDatabase,
        };
        if (!string.IsNullOrEmpty(_options.Password))
            config.Password = _options.Password;
        config.EndPoints.Add(host);

        var conn = ConnectionMultiplexer.Connect(config);
        WireEvents(conn);
        return conn;
    }

    private void WireEvents(ConnectionMultiplexer conn)
    {
        conn.ConnectionFailed += (_, args) =>
            _logger.LogWarning("[Redis] Connection failed — endpoint: {Endpoint}, failure: {Failure}, error: {Error}",
                args.EndPoint, args.FailureType, args.Exception?.Message);

        conn.ConnectionRestored += (_, args) =>
            _logger.LogInformation("[Redis] Connection restored — endpoint: {Endpoint}", args.EndPoint);

        conn.ErrorMessage += (_, args) =>
            _logger.LogError("[Redis] Server error — endpoint: {Endpoint}, message: {Message}",
                args.EndPoint, args.Message);
    }

    // ──────────────────────────────────────────
    // String operations
    // ──────────────────────────────────────────

    public async Task<string?> GetStringAsync(string key)
    {
        try
        {
            var value = await Db.StringGetAsync(key, CommandFlags.PreferReplica).ConfigureAwait(false);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] GetStringAsync failed — key: {Key}", key);
            return null;
        }
    }

    public async Task<bool> SetStringAsync(string key, string value, TimeSpan? expiry = null)
    {
        try
        {
            Expiration exp = expiry.HasValue ? expiry.Value : default;
            return await Db.StringSetAsync(key, value, exp, flags: CommandFlags.PreferMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] SetStringAsync failed — key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string key)
    {
        try
        {
            return await Db.KeyDeleteAsync(key, CommandFlags.PreferMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] DeleteAsync failed — key: {Key}", key);
            return false;
        }
    }

    // ──────────────────────────────────────────
    // Hash operations
    // ──────────────────────────────────────────

    public async Task<T?> HashGetAsync<T>(string hashKey, string hashField)
    {
        try
        {
            if (string.IsNullOrEmpty(hashField)) return default;
            var data = await Db.HashGetAsync(hashKey, hashField, CommandFlags.PreferReplica).ConfigureAwait(false);
            if (data.IsNullOrEmpty) return default;
            return JsonConvert.DeserializeObject<T>(data!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] HashGetAsync failed — {HashKey}:{HashField}", hashKey, hashField);
            return default;
        }
    }

    public async Task<bool> HashSetAsync<T>(string hashKey, string hashField, T value, int ttlSeconds = 0)
    {
        try
        {
            var db = Db;
            await db.HashSetAsync(hashKey, hashField, JsonConvert.SerializeObject(value),
                flags: CommandFlags.PreferMaster).ConfigureAwait(false);
            if (ttlSeconds > 0)
                await db.KeyExpireAsync(hashKey, TimeSpan.FromSeconds(ttlSeconds),
                    flags: CommandFlags.PreferMaster).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] HashSetAsync failed — {HashKey}:{HashField}", hashKey, hashField);
            return false;
        }
    }

    public async Task<bool> HashDeleteAsync(string hashKey, string hashField)
    {
        try
        {
            return await Db.HashDeleteAsync(hashKey, hashField, CommandFlags.PreferMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] HashDeleteAsync failed — {HashKey}:{HashField}", hashKey, hashField);
            return false;
        }
    }

    public async Task<IDictionary<string, T>> HashGetAllAsync<T>(string hashKey)
    {
        try
        {
            var entries = await Db.HashGetAllAsync(hashKey, CommandFlags.PreferReplica).ConfigureAwait(false);
            return entries.ToDictionary(
                e => e.Name.ToString(),
                e => JsonConvert.DeserializeObject<T>(e.Value!)!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] HashGetAllAsync failed — {HashKey}", hashKey);
            return new Dictionary<string, T>();
        }
    }

    // ──────────────────────────────────────────
    // List operations
    // ──────────────────────────────────────────

    public async Task<long> ListRightPushAsync(string key, string value)
    {
        try
        {
            return await Db.ListRightPushAsync(key, value, flags: CommandFlags.PreferMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] ListRightPushAsync failed — key: {Key}", key);
            return -1;
        }
    }

    // ──────────────────────────────────────────
    // Utility
    // ──────────────────────────────────────────

    public async Task<bool> KeyExistsAsync(string key)
    {
        try
        {
            return await Db.KeyExistsAsync(key, CommandFlags.PreferReplica).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] KeyExistsAsync failed — key: {Key}", key);
            return false;
        }
    }

    public async Task<bool> KeyExpireAsync(string key, TimeSpan expiry)
    {
        try
        {
            return await Db.KeyExpireAsync(key, expiry, flags: CommandFlags.PreferMaster).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] KeyExpireAsync failed — key: {Key}", key);
            return false;
        }
    }

    public Task<List<string>> GetKeysByPatternAsync(string pattern)
    {
        try
        {
            // SERVER-side SCAN (không phải KEYS) — StackExchange.Redis tự dùng SCAN khi server hỗ trợ.
            // Chỉ dùng cho pattern hẹp (queue keys, retry keys) — không quét pattern rộng trong production.
            var conn = _lazyConnection.Value;
            var result = new List<string>();
            foreach (var endpoint in conn.GetEndPoints())
            {
                var server = conn.GetServer(endpoint);
                if (server.IsReplica) continue;
                foreach (var key in server.Keys(_options.DefaultDatabase, pattern))
                    result.Add(key.ToString());
            }
            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Redis] GetKeysByPatternAsync failed — pattern: {Pattern}", pattern);
            return Task.FromResult(new List<string>());
        }
    }
}
