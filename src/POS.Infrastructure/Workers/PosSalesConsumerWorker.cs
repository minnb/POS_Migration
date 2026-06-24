using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Common.Dtos.POS;
using POS.Common.Helpers;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Repositories.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace POS.Infrastructure.Workers;

public sealed class PosSalesConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PosSalesConsumerWorker> logger,
    WorkerHealthState healthState
) : BackgroundService
{
    private const string QueueName = "pos_sales";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = configuration.GetSection(RabbitMQOptions.SectionName).Get<RabbitMQOptions>()
                   ?? new RabbitMQOptions();

        var factory = new ConnectionFactory
        {
            HostName                 = opts.Host,
            Port                     = opts.Port,
            UserName                 = opts.Username,
            Password                 = opts.Password,
            VirtualHost              = opts.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval  = TimeSpan.FromSeconds(10),
            RequestedHeartbeat       = TimeSpan.FromSeconds(opts.RequestedHeartbeat),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? conn    = null;
            IChannel?    channel = null;

            try
            {
                conn    = await factory.CreateConnectionAsync(stoppingToken);
                channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(
                    queue: QueueName, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);

                // Nhận từng message một — tránh overload nếu re-insert chậm
                await channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false,
                    cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);

                await channel.BasicConsumeAsync(
                    queue: QueueName, autoAck: false, consumer: consumer,
                    cancellationToken: stoppingToken);

                logger.LogInformation("[PosSalesWorker] Consuming queue '{Queue}'", QueueName);
                healthState.Status = "Running";

                // Giữ worker alive: thoát khi app dừng hoặc connection drop
                var connDropped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                conn.ConnectionShutdownAsync += (_, _) =>
                {
                    connDropped.TrySetResult();
                    return Task.CompletedTask;
                };

                await Task.WhenAny(connDropped.Task, Task.Delay(Timeout.Infinite, stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                healthState.Status = "Degraded";
                logger.LogError(ex, "[PosSalesWorker] Connection error — retry in 10s");
            }
            finally
            {
                try { if (channel is not null) { await channel.CloseAsync(); channel.Dispose(); } } catch { }
                try { if (conn    is not null) { await conn.CloseAsync();    conn.Dispose();    } } catch { }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        KafkaMessageDto? msg = null;

        try
        {
            msg = JsonConvert.DeserializeObject<KafkaMessageDto>(body);
            if (msg is null)
            {
                logger.LogWarning("[PosSalesWorker] Cannot deserialize message — discarding");
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ICentralSaleRepository>();

            var result = await repo.InInsertToTableByJson(
                StringHelper.Left(msg.TransactionId, 4),
                StringHelper.Left(msg.TransactionId, 6),
                msg.TransactionId,
                msg.Message ?? "", "WORKER");

            if (result.Item1)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                healthState.IncrementProcessed();
                logger.LogInformation("[PosSalesWorker] Re-inserted OK — txId: {TxId}", msg.TransactionId);
            }
            else
            {
                logger.LogWarning("[PosSalesWorker] Re-insert failed — txId: {TxId}, reason: {Reason}",
                    msg.TransactionId, result.Item2);
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[PosSalesWorker] Unhandled error — txId: {TxId}", msg?.TransactionId);
            try { await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); } catch { }
        }
    }
}
