namespace POS.Infrastructure.Messaging;

/// <summary>
/// Kafka producer cho pipeline upload sale data (SyncDataPos).
/// Topic = StoreSetConfig.Name (theo storeNo), bootstrap servers từ ConnectionStrings:BootstrapServers.
/// </summary>
public interface IKafkaProducer
{
    Task<bool> PushMessageAsync(string topic, string key, string message, string storeNo = "", string posNo = "");
}
