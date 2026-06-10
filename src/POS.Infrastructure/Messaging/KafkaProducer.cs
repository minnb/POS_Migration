using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using POS.Common.Dtos;
using POS.Infrastructure.Logging;

namespace POS.Infrastructure.Messaging;

/// <summary>
/// Migrated từ KafkaProducerFactory (API_WebApiCore/Factory/KafkaProducerFactory.cs).
/// Singleton: IProducer thread-safe, giữ 1 connection cho toàn app (như static cũ).
/// Mỗi message kèm header "Data" = AuthDto { AppCode = POS, StoreNo, PosNo } — contract với consumer.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly Lazy<IProducer<string, string>> _lazyProducer;
    private readonly IFileLogHelper _fileLogHelper;

    public KafkaProducer(IConfiguration configuration, IFileLogHelper fileLogHelper)
    {
        _fileLogHelper = fileLogHelper;
        _lazyProducer = new Lazy<IProducer<string, string>>(() =>
        {
            var bootstrapServers = configuration.GetConnectionString("BootstrapServers");
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            return new ProducerBuilder<string, string>(config)
                .SetValueSerializer(Serializers.Utf8)
                .Build();
        }, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    public async Task<bool> PushMessageAsync(string topic, string key, string message, string storeNo = "", string posNo = "")
    {
        try
        {
            var authDto = new AuthDto
            {
                AppCode = "POS",
                StoreNo = storeNo,
                PosNo = posNo
            };

            await _lazyProducer.Value.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = message,
                Headers = new Headers
                {
                    { "Data", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authDto)) }
                }
            });
            return true;
        }
        catch (Exception ex)
        {
            _fileLogHelper.WriteLogs($"KafkaProducer.PushMessageAsync topic={topic} key={key} ProduceException: {ex.Message}");
            return false;
        }
    }

    public void Dispose()
    {
        if (_lazyProducer.IsValueCreated)
            _lazyProducer.Value.Dispose();
    }
}
