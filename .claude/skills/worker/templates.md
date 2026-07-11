---
name: worker-templates
description: Code mẫu copy-paste cho 3 khuôn mẫu POS.Worker — Timer polling (Template A), Message consumer RabbitMQ (Template B), Poll+fan-out song song+quarantine (Pattern C). Đọc sau khi đã chọn đúng pattern ở SKILLS.md.
---

# Worker Templates — code mẫu copy-paste

> Chọn đúng pattern ở [`SKILLS.md`](SKILLS.md) mục "Bốn khuôn mẫu worker" trước khi copy template
> dưới đây. Quy tắc BẮT BUỘC chung (scope DI, try/catch không chết vòng lặp...) đã nêu ở
> `SKILLS.md` — không lặp lại ở đây.

---

## Template A — Timer polling (chạy định kỳ)

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS.Infrastructure.Repositories.Interfaces;

namespace POS.Infrastructure.Workers;

public sealed class MyScheduledWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<MyScheduledWorker> logger,
    WorkerHealthState healthState) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("[MyScheduledWorker] Started — interval {Interval}", Interval);
        healthState.Status = "Running";

        using var timer = new PeriodicTimer(Interval);
        try
        {
            do
            {
                try
                {
                    await using var scope = scopeFactory.CreateAsyncScope();
                    var repo = scope.ServiceProvider.GetRequiredService<IMyRepository>();

                    await repo.DoWorkAsync(stoppingToken);
                    healthState.IncrementProcessed();
                    logger.LogInformation("[MyScheduledWorker] Tick OK");
                }
                catch (Exception ex)
                {
                    healthState.Status = "Degraded";
                    logger.LogError(ex, "[MyScheduledWorker] Tick failed"); // nuốt → loop tiếp
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }
        catch (OperationCanceledException) { }
        finally { logger.LogInformation("[MyScheduledWorker] Stopping"); }
    }
}
```

> Dùng `PeriodicTimer` cho job định kỳ — gọn và chính xác hơn `Task.Delay`.
> Nếu cần "căn mốc thời gian tuyệt đối" (vd 00:00 mỗi ngày), tính `nextTick` thủ công như
> `Rpt_ReportSaleDetail_Insert.cs`.

---

## Template B — Message consumer (RabbitMQ push)

```csharp
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using POS.Infrastructure.Messaging;
using POS.Infrastructure.Repositories.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;

namespace POS.Infrastructure.Workers;

public sealed class MyConsumerWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<MyConsumerWorker> logger,
    WorkerHealthState healthState) : BackgroundService
{
    private const string QueueName = "my_queue";

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = configuration.GetSection(RabbitMQOptions.SectionName).Get<RabbitMQOptions>()
                   ?? new RabbitMQOptions();

        var factory = new ConnectionFactory
        {
            HostName = opts.Host, Port = opts.Port,
            UserName = opts.Username, Password = opts.Password, VirtualHost = opts.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            RequestedHeartbeat = TimeSpan.FromSeconds(opts.RequestedHeartbeat),
            RequestedConnectionTimeout = TimeSpan.FromSeconds(5),
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            IConnection? conn = null;
            IChannel? channel = null;
            try
            {
                conn = await factory.CreateConnectionAsync(stoppingToken);
                channel = await conn.CreateChannelAsync(cancellationToken: stoppingToken);

                await channel.QueueDeclareAsync(QueueName, durable: true, exclusive: false,
                    autoDelete: false, arguments: null, cancellationToken: stoppingToken);
                await channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(channel);
                consumer.ReceivedAsync += async (_, ea) => await HandleMessageAsync(channel, ea);
                await channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken: stoppingToken);

                logger.LogInformation("[MyConsumerWorker] Consuming '{Queue}'", QueueName);
                healthState.Status = "Running";

                // Giữ worker alive: thoát khi app dừng hoặc connection drop
                var connDropped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                conn.ConnectionShutdownAsync += (_, _) => { connDropped.TrySetResult(); return Task.CompletedTask; };
                await Task.WhenAny(connDropped.Task, Task.Delay(Timeout.Infinite, stoppingToken));
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                healthState.Status = "Degraded";
                logger.LogError(ex, "[MyConsumerWorker] Connection error — retry in 10s");
            }
            finally
            {
                try { if (channel is not null) { await channel.CloseAsync(); channel.Dispose(); } } catch { }
                try { if (conn is not null) { await conn.CloseAsync(); conn.Dispose(); } } catch { }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleMessageAsync(IChannel channel, BasicDeliverEventArgs ea)
    {
        var body = Encoding.UTF8.GetString(ea.Body.Span);
        try
        {
            var msg = JsonConvert.DeserializeObject<MyMessageDto>(body);
            if (msg is null)
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<IMyRepository>();

            var ok = await repo.ProcessAsync(msg);
            if (ok)
            {
                await channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                healthState.IncrementProcessed();
            }
            else
            {
                await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[MyConsumerWorker] Unhandled error");
            try { await channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); } catch { }
        }
    }
}
```

**Lưu ý ack/nack:**
- `prefetchCount: 1` → xử lý từng message, tránh overload khi DB chậm.
- `autoAck: false` → tự ack sau khi xử lý OK.
- Lỗi parse / xử lý thất bại → `BasicNackAsync(requeue: false)` (tránh poison-message loop vô hạn).
  Nếu cần retry, dùng dead-letter queue thay vì `requeue: true`.

---

## Pattern C — Poll + fan-out song song + quarantine

> Áp dụng khi: 1 tick phát hiện thay đổi rồi cần áp dụng cho **N đối tượng độc lập** (terminal,
> store, tenant...) song song, và 1 đối tượng lỗi liên tục **không được phép** chặn tiến trình của
> các đối tượng còn lại. Ví dụ đầy đủ: `src/POS.Worker/Workers/MasterDataZipGeneratorWorker.cs`.

```csharp
// 1 scope DUY NHẤT cho cả tick (KHÔNG tạo scope mới mỗi đối tượng)
await using var scope = scopeFactory.CreateAsyncScope();
var redisManager = scope.ServiceProvider.GetRequiredService<IRedisManager>();

// Single-runner across instances: distributed lock qua IRedisManager (KHÔNG dùng ZSET throttle —
// throttle chỉ giới hạn số lượt song song, lock đảm bảo đúng 1 instance chạy 1 lượt)
var token = await redisManager.AcquireLockAsync("Worker:Lock:{Name}", ttl);
if (token is null) return;   // instance khác đang giữ lượt này
try
{
    var quarantine = await redisManager.HashGetAllAsync<int>("Worker:Quarantine:{Name}");
    var eligible = targets.Where(t => !quarantine.TryGetValue(Key(t), out var n) || n < Threshold);

    long failed = 0;
    await Parallel.ForEachAsync(eligible, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct },
        async (target, token) =>
        {
            try
            {
                await DoWorkAsync(target, token);
                await redisManager.HashDeleteAsync("Worker:Quarantine:{Name}", Key(target)); // reset về 0
            }
            catch (Exception ex)
            {
                Interlocked.Increment(ref failed);
                var cur = await redisManager.HashGetAsync<int>("Worker:Quarantine:{Name}", Key(target));
                await redisManager.HashSetAsync("Worker:Quarantine:{Name}", Key(target), cur + 1);
            }
        });

    if (failed == 0) /* ACK watermark — chỉ tịnh tiến khi mọi đối tượng ĐÃ THỬ đều thành công */;
}
finally { await redisManager.ReleaseLockAsync("Worker:Lock:{Name}", token); }
```

**Quy tắc cốt lõi:**
- Watermark (Redis Hash, không TTL) chỉ tịnh tiến khi **toàn bộ** đối tượng đã thử trong lượt này
  thành công — không "ACK một phần" nếu tín hiệu phát hiện thay đổi là giá trị global (không phải
  per-đối-tượng).
- Quarantine (Redis Hash đếm lỗi liên tiếp) khác throttle: throttle giới hạn *số lượt chạy đồng
  thời*, quarantine loại *đối tượng cụ thể* ra khỏi các lượt sau khi nó lỗi ≥ ngưỡng — 2 cơ chế
  độc lập, không thay thế nhau.
- Lỗi hạ tầng dùng chung (throttle cụm hết slot...) **không** tính vào quarantine của 1 đối tượng —
  chỉ lỗi thật của riêng đối tượng đó mới cộng dồn.
- `HashGetAllAsync` trả dict rỗng cả khi lỗi Redis lẫn khi hash rỗng thật (`RedisManager` nuốt
  exception) — luôn `KeyExistsAsync` trước để phân biệt "chưa từng ghi" với "đọc lỗi", tránh
  generate sai/ACK sai khi Redis chập chờn.
