using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace POS.Infrastructure.Messaging;

public sealed class RabbitMQProducer : IRabbitMQProducer, IAsyncDisposable
{
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQProducer> _logger;
    private IConnection? _connection;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private static readonly Dictionary<string, object?> QuorumQueueArgs =
        new() { { "x-queue-type", "quorum" } };

    public RabbitMQProducer(IConfiguration configuration, ILogger<RabbitMQProducer> logger)
    {
        _options = configuration.GetSection(RabbitMQOptions.SectionName).Get<RabbitMQOptions>()
                   ?? new RabbitMQOptions();
        _logger = logger;
    }

    // ──────────────────────────────────────────
    // Connection management (singleton, lazy, thread-safe)
    // ──────────────────────────────────────────

    private async Task<IConnection?> GetConnectionAsync(CancellationToken ct = default)
    {
        // Fast path: reuse open connection
        if (_connection?.IsOpen == true) return _connection;

        await _lock.WaitAsync(ct);
        try
        {
            // Double-check after acquiring lock
            if (_connection?.IsOpen == true) return _connection;

            var factory = new ConnectionFactory
            {
                UserName = _options.Username,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost,
                Port = _options.Port,
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                RequestedHeartbeat = TimeSpan.FromSeconds(_options.RequestedHeartbeat),
            };

            var endpoints = _options.Hosts
                .Select(h => new AmqpTcpEndpoint(h, _options.Port))
                .ToList();

            _connection = await factory.CreateConnectionAsync(endpoints, ct);

            _connection.ConnectionShutdownAsync += (_, args) =>
            {
                _logger.LogWarning("[RabbitMQ] Connection shutdown: {ReplyText}", args.ReplyText);
                return Task.CompletedTask;
            };

            _logger.LogInformation("[RabbitMQ] Connected to broker");
            return _connection;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RabbitMQ] Failed to connect to broker — messages will be dropped until connection is restored");
            return null;
        }
        finally
        {
            _lock.Release();
        }
    }

    // ──────────────────────────────────────────
    // Publish
    // ──────────────────────────────────────────

    public async Task ProducerRabbtMQClusterAsync(string queueName, string message)
    {
        try
        {
            var connection = await GetConnectionAsync();
            if (connection is null)
            {
                _logger.LogWarning("[RabbitMQ] Skipping publish — no connection. Queue: {Queue}", queueName);
                return;
            }

            await using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(
                queue: queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: QuorumQueueArgs);

            var props = new BasicProperties
            {
                Persistent = true,
                MessageId = Guid.NewGuid().ToString("N")
            };

            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: Encoding.UTF8.GetBytes(message));

            _logger.LogDebug("[RabbitMQ] Published to {Queue}", queueName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[RabbitMQ] ProducerRabbtMQClusterAsync failed — queue: {Queue}", queueName);
        }
    }

    // Sync wrapper — safe to call inside Task.Run(() => ...)
    public void ProducerRabbtMQCluster(string queueName, string message)
        => ProducerRabbtMQClusterAsync(queueName, message).GetAwaiter().GetResult();

    // ──────────────────────────────────────────
    // Dispose
    // ──────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.CloseAsync();
            _connection.Dispose();
        }
        _lock.Dispose();
    }
}
