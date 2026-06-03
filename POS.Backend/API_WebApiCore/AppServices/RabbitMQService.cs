using Newtonsoft.Json;
using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TCX.API.Common.Const;
using TCX.API.Common.Dtos;
using TCX.API.Common.Dtos.Loyalty;
using TCX.API.Common.Helpers;
using TCX.WebApiCore.DbContext;

namespace TCX.WebApiCore.AppServices
{
    public static class RabbitMQProducer
    {
        public static void ProducerRabbtMQCluster(KibanaService _kibanaService, string queueName, string messageId, string message, bool isPersistent = true)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" }
                };

                using (var connection = RabbitMQClusterConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: args);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = isPersistent;
                    properties.MessageId = messageId;

                    var body = Encoding.UTF8.GetBytes(message);
                    channel.BasicPublish(exchange: "",
                                         routingKey: queueName,
                                         basicProperties: properties,
                                         body: body);
                    //_kibanaService.LogRequest("RabbtMQCluster", messageId,"", message);
                    StringHelper.Loggingz($"ProducerRabbtMQCluster.RabbitMQ [x] {messageId} Sent {message}");
                }
            }
            catch (Exception ex)
            {
                _kibanaService.LogException("RabbtMQCluster", messageId, 0, "", message);
                StringHelper.Loggingz($"ProducerRabbtMQCluster.Exception {JsonConvert.SerializeObject(ex)}");
            }
        }
        public static void SendMessage(string queueName,string messageId, string message, bool isPersistent = true, int priority = 0)
        {
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "x-max-priority", priority }
                };

                if (priority == 0) 
                {
                    args = null;
                }
                using (var connection = RabbitMQConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    // declare queue
                    channel.QueueDeclare(queue: queueName,
                                         durable: true,
                                         exclusive: false,
                                         autoDelete: false,
                                         arguments: args);

                    var properties = channel.CreateBasicProperties();
                    properties.Persistent = isPersistent;
                    properties.MessageId = messageId;

                    var body = Encoding.UTF8.GetBytes(message);

                    // send message to queue
                    channel.BasicPublish(exchange: "",
                                         routingKey: queueName,
                                         basicProperties: properties,
                                         body: body);

                    FileHelper.WriteLogs($"SendMessage.RabbitMQ [x] Sent {message}");
                }
            }
            catch(Exception ex)
            {
                FileHelper.WriteLogs("RabbitMQProducer.SendMessage.Exception:" + JsonConvert.SerializeObject(ex));
            }
        }
        public static List<string> ConsumerRabbtMQCluster(KibanaService _kibanaService, string queueName, int limit = 2)
        {
            var messages = new List<string>();
            try
            {
                var args = new Dictionary<string, object>
                {
                    { "x-queue-type", "quorum" }
                };
                using (var connection = RabbitMQClusterConnection.GetConnection())
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(queue: queueName,
                                    durable: true,
                                    exclusive: false,
                                    autoDelete: false,
                                    arguments: args);
                    int messageCount = 0;
                    while (messageCount < limit)
                    {
                        var result = channel.BasicGet(queue: queueName, autoAck: true);
                        if (result != null)
                        {
                            messages.Add(Encoding.UTF8.GetString(result.Body.ToArray()));
                        }
                        else
                        {
                            break;
                        }
                        messageCount++;
                    }
                }
                return messages;
            }
            catch (Exception ex)
            {
                FileHelper.WriteExpLogs("ConsumerRabbtMQCluster.Exception", ex);
                _kibanaService.LogException("ConsumerRabbtMQCluster", "", 0, "", $"{queueName} Exception: {JsonConvert.SerializeObject(ex)}");
                return null;
            }
        }
    }
}
