namespace VCM.POSBLUE.Application.Interfaces;

/// <summary>
/// Abstraction cho RabbitMQ. Thay thế static RabbitMQProducer cũ (đã chuyển sang API async của RabbitMQ.Client v7).
/// </summary>
public interface IMessageQueueService
{
    /// <summary>Publish vào connection mặc định (tương đương RabbitMQProducer.SendMessage).</summary>
    Task SendMessageAsync(string queueName, string messageId, string message, bool isPersistent = true, int priority = 0);

    /// <summary>Publish vào cluster quorum queue (tương đương ProducerRabbtMQCluster).</summary>
    Task ProduceClusterAsync(string queueName, string messageId, string message, bool isPersistent = true);

    /// <summary>Đọc tối đa <paramref name="limit"/> message từ cluster queue (tương đương ConsumerRabbtMQCluster).</summary>
    Task<List<string>> ConsumeClusterAsync(string queueName, int limit = 2);
}
