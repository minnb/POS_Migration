namespace POS.Application.Messaging;

public interface IMessagePublisher
{
    /// <summary>
    /// Publish message lên 1 queue cụ thể (direct routing).
    /// routingKey = tên queue (ví dụ: "BLUEPOS:Loyalty:MemberPoints").
    /// </summary>
    Task PublishAsync<T>(string queueName, T message) where T : class;

    /// <summary>Fire-and-forget — không await, exception bị log nhưng không ném ra.</summary>
    void PublishFireAndForget<T>(string queueName, T message) where T : class;
}
