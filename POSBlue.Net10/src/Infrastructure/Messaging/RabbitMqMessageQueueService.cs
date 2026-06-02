using System.Text;
using RabbitMQ.Client;
using VCM.POSBLUE.Application.Interfaces;

namespace VCM.POSBLUE.Infrastructure.Messaging;

/// <summary>
/// Triển khai IMessageQueueService bằng RabbitMQ.Client v7 (async).
/// Port logic từ RabbitMQProducer cũ (SendMessage / ProducerRabbtMQCluster / ConsumerRabbtMQCluster).
/// </summary>
public sealed class RabbitMqMessageQueueService : IMessageQueueService
{
    private readonly RabbitMqConnectionProvider _provider;
    private readonly IRequestLogger _logger;

    public RabbitMqMessageQueueService(RabbitMqConnectionProvider provider, IRequestLogger logger)
    {
        _provider = provider;
        _logger = logger;
    }

    public async Task SendMessageAsync(string queueName, string messageId, string message, bool isPersistent = true, int priority = 0)
    {
        try
        {
            IDictionary<string, object?>? args = priority > 0
                ? new Dictionary<string, object?> { { "x-max-priority", priority } }
                : null;

            var connection = await _provider.GetDefaultConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

            var props = new BasicProperties { Persistent = isPersistent, MessageId = messageId };
            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: props, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogException("RabbitMQ.SendMessage", queueName, 0, "", ex.ToString());
        }
    }

    public async Task ProduceClusterAsync(string queueName, string messageId, string message, bool isPersistent = true)
    {
        try
        {
            IDictionary<string, object?> args = new Dictionary<string, object?> { { "x-queue-type", "quorum" } };

            var connection = await _provider.GetClusterConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

            var props = new BasicProperties { Persistent = isPersistent, MessageId = messageId };
            var body = Encoding.UTF8.GetBytes(message);
            await channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: false, basicProperties: props, body: body);
        }
        catch (Exception ex)
        {
            _logger.LogException("RabbitMQ.ProduceCluster", messageId, 0, "", ex.ToString());
        }
    }

    public async Task<List<string>> ConsumeClusterAsync(string queueName, int limit = 2)
    {
        var messages = new List<string>();
        try
        {
            IDictionary<string, object?> args = new Dictionary<string, object?> { { "x-queue-type", "quorum" } };

            var connection = await _provider.GetClusterConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();
            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);

            int count = 0;
            while (count < limit)
            {
                var result = await channel.BasicGetAsync(queueName, autoAck: true);
                if (result is null) break;
                messages.Add(Encoding.UTF8.GetString(result.Body.ToArray()));
                count++;
            }
            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogException("RabbitMQ.ConsumeCluster", queueName, 0, "", ex.ToString());
            return messages;
        }
    }
}
