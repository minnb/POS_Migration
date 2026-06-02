using Confluent.Kafka;
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Diagnostics;
using System.Text;
using TCX.API.Common.Helpers;
using TCX.API.Common.Shared;

namespace TCX.WebApiCore
{
    public class KafkaService
    {
        private readonly string _bootstrapServers;
        private readonly KibanaService _kibanaService;
        public KafkaService()
        {
            _bootstrapServers = ConfigurationManager.ConnectionStrings["BootstrapServers"].ConnectionString;
            _kibanaService = new KibanaService();
        }
        public void Produce(string topic, string message, string key = "")
        {
            try
            {
                //var config = new ProducerConfig { BootstrapServers = _bootstrapServers };
                var headers = new Headers
                {
                    { "Key", Encoding.UTF8.GetBytes(key)},
                    { "HostName", Encoding.UTF8.GetBytes(ComputerHelper.GetHostName())}
                };

                //using (var producer = new ProducerBuilder<Null, string>(config).Build())
                using (var producer = KafkaProducerFactory.GetProducer(_bootstrapServers))
                {
                    try
                    {
                        producer.Produce(topic, new Message<string, string> { Value = message, Headers = headers });
                        producer.Flush(TimeSpan.FromSeconds(10));
                    }
                    catch (ProduceException<Null, string> ex)
                    {
                        FileHelper.WriteExpLogs("Kafka.Produce.ProduceException", ex);
                        _kibanaService.LogException("Kafka.Produce.ProduceException", topic, 0, "", message);
                    }
                }
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("Kafka.Produce.Exception", ex);
                _kibanaService.LogException("Kafka.Produce.Exception", topic, 0, "", message);
            }
        }
        public bool ProduceService(object message, string posNo)
        {
            try
            {
                var getKafkaInfo = StringHelper.ConverStringToArray(_bootstrapServers, ';');
                if(getKafkaInfo.Length > 0) 
                {
                    long milliseconds = 0;
                    var st1 = new Stopwatch();
                    st1.Start();
                    _kibanaService.LogRequest(getKafkaInfo[0], posNo, "", JsonConvert.SerializeObject(message));
                    var api = new ApiHelper(
                                getKafkaInfo[0],
                                "Basic",
                                getKafkaInfo[1], 
                                "POST", 
                                JsonConvert.SerializeObject(message), 
                                true, null, null, posNo);

                    var response = api.InteractWithApi();
                    milliseconds = st1.ElapsedMilliseconds;
                    st1.Stop();
                    _kibanaService.LogResponse(getKafkaInfo[0], posNo, milliseconds, "", response);
                    return true; 
                }

                FileHelper.WriteExpLogs("KafkaService.Produce.ConverStringToArray.Exception", null);
                return false;
            }
            catch(Exception ex) 
            {
                FileHelper.WriteExpLogs("KafkaService.Produce.Exception", ex);
                return false;
            }
        }
    }
}
