using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;

namespace VCM.POSBLUE.Infrastructure.Messaging;

/// <summary>
/// Quản lý kết nối RabbitMQ — thay RabbitMQConnection + RabbitMQClusterConnection (static) cũ.
/// Dùng API async của RabbitMQ.Client v7. Cache connection (mở lại nếu đóng), tạo channel theo từng thao tác.
///  - Default:  connection string "RabbitMQ" định dạng "host;user;pass".
///  - Cluster:  connection string "RabbitMQCluster" = "node1;node2", user lấy từ AppSettings:RabbitMQClusterUser = "user;pass".
/// </summary>
public sealed class RabbitMqConnectionProvider : IAsyncDisposable
{
    private readonly IConfiguration _configuration;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private IConnection? _default;
    private IConnection? _cluster;

    public RabbitMqConnectionProvider(IConfiguration configuration) => _configuration = configuration;

    public async Task<IConnection> GetDefaultConnectionAsync()
    {
        if (_default is { IsOpen: true }) return _default;
        await _gate.WaitAsync();
        try
        {
            if (_default is { IsOpen: true }) return _default;
            var conn = _configuration.GetConnectionString("RabbitMQ")
                ?? throw new InvalidOperationException("Thiếu connection string 'RabbitMQ'.");
            var arr = conn.Split(';');
            var factory = new ConnectionFactory
            {
                HostName = arr[0],
                Port = 5672,
                UserName = arr.Length > 1 ? arr[1] : "guest",
                Password = arr.Length > 2 ? arr[2] : "guest",
                RequestedHeartbeat = TimeSpan.FromSeconds(30),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            };
            _default = await factory.CreateConnectionAsync();
            return _default;
        }
        finally { _gate.Release(); }
    }

    public async Task<IConnection> GetClusterConnectionAsync()
    {
        if (_cluster is { IsOpen: true }) return _cluster;
        await _gate.WaitAsync();
        try
        {
            if (_cluster is { IsOpen: true }) return _cluster;
            var nodes = (_configuration.GetConnectionString("RabbitMQCluster") ?? "")
                .Split(';', StringSplitOptions.RemoveEmptyEntries);
            var userPass = (_configuration["AppSettings:RabbitMQClusterUser"] ?? "").Split(';');
            var factory = new ConnectionFactory
            {
                UserName = userPass.Length > 0 ? userPass[0] : "guest",
                Password = userPass.Length > 1 ? userPass[1] : "guest",
                VirtualHost = "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
            };
            var endpoints = nodes.Select(ip => new AmqpTcpEndpoint(ip)).ToList();
            _cluster = await factory.CreateConnectionAsync(endpoints);
            return _cluster;
        }
        finally { _gate.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        if (_default is not null) await _default.DisposeAsync();
        if (_cluster is not null) await _cluster.DisposeAsync();
        _gate.Dispose();
    }
}
