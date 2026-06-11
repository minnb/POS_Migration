namespace POS.Infrastructure.Messaging;

public interface IRabbitMQProducer
{
    /// <summary>
    /// Gửi message vào queue chỉ định. Fire-and-forget safe.
    /// Giữ nguyên tên method như code cũ để không cần đổi caller.
    /// </summary>
    Task ProducerRabbtMQClusterAsync(string queueName, string message);

    /// <summary>
    /// Overload đồng bộ cho backward compat với code gọi Task.Run(() => ...).
    /// </summary>
    void ProducerRabbtMQCluster(string queueName, string message);

    /// <summary>
    /// Check kết nối broker chủ động (bỏ qua backoff) — dùng cho api/common/CheckConnection.
    /// </summary>
    Task<(bool Ok, string Message)> CheckConnectionAsync(CancellationToken ct = default);
}
