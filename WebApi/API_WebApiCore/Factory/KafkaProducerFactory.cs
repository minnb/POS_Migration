using Confluent.Kafka;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Dtos;
using TCX.API.Common.Helpers;

public static class KafkaProducerFactory
{
    private static IProducer<string, string> _producer;
    public static IProducer<string, string> GetProducer(string bootstrapServers)
    {
        if (_producer == null)
        {
            var config = new ProducerConfig { BootstrapServers = bootstrapServers };
            _producer = new ProducerBuilder<string, string>(config).SetValueSerializer(Serializers.Utf8).Build();
        }
        return _producer;
    }
    public async static Task<bool> PushMessage(string bootstrapServers, string topic, string message, string key, string storeNo = "", string posNo = "")
    {
        try
        {
            var authDto = new AuthDto()
            {
                AppCode = "POS",
                StoreNo = storeNo,
                PosNo = posNo
            };
            
            var stopwatch = Stopwatch.StartNew();
            var producer = GetProducer(bootstrapServers);
            var deliveryResult = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = message,
                Headers = new Headers
                            {
                                { "Data", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(authDto)) }
                             }
            });
            stopwatch.Stop();
            return true;
        }
        catch (ProduceException<Null, string> e)
        {
            FileHelper.WriteLogs($"ProduceException: {JsonConvert.SerializeObject(e)}");
            return false;
        }
    }
}
