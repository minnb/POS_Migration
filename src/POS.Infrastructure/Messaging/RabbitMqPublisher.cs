using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using POS.Application.Messaging;
using RabbitMQ.Client;

namespace POS.Infrastructure.Messaging;

public class RabbitMqPublisher : IMessagePublisher, IAsyncDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly ILogger<RabbitMqPublisher> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public RabbitMqPublisher(IConnection connection, ILogger<RabbitMqPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
    }

    public async Task PublishAsync<T>(string queueName, T message) where T : class
    {
        try
        {
            await EnsureQueueDeclaredAsync(queueName);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message, JsonOptions));
            var props = new BasicProperties
            {
                ContentType = "application/json",
                DeliveryMode = DeliveryModes.Persistent
            };

            await _channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: queueName,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation("MQ publish {Queue} {Type}", queueName, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MQ publish lỗi queue={Queue}", queueName);
            throw;
        }
    }

    public void PublishFireAndForget<T>(string queueName, T message) where T : class
    {
        _ = Task.Run(async () =>
        {
            try { await PublishAsync(queueName, message); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MQ fire-and-forget lỗi queue={Queue}", queueName);
            }
        });
    }

    private async Task EnsureQueueDeclaredAsync(string queueName)
    {
        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);
    }

    public async ValueTask DisposeAsync()
    {
        await _channel.DisposeAsync();
        await _connection.DisposeAsync();
    }
}
