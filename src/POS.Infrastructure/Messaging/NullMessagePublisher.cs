using Microsoft.Extensions.Logging;
using POS.Application.Messaging;

namespace POS.Infrastructure.Messaging;

// Dùng khi RabbitMQ:Enabled = false (local dev, staging không có MQ).
// Chỉ log thay vì publish thật.
public class NullMessagePublisher : IMessagePublisher
{
    private readonly ILogger<NullMessagePublisher> _logger;

    public NullMessagePublisher(ILogger<NullMessagePublisher> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(string queueName, T message) where T : class
    {
        _logger.LogDebug("NullMQ publish {Queue} {Type}", queueName, typeof(T).Name);
        return Task.CompletedTask;
    }

    public void PublishFireAndForget<T>(string queueName, T message) where T : class
    {
        _logger.LogDebug("NullMQ fire-and-forget {Queue} {Type}", queueName, typeof(T).Name);
    }
}
