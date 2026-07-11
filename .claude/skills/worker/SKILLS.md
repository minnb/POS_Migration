# Skill: Background Worker — POS.Worker / Hosted Services

> **Áp dụng khi:** tạo scheduled job, message consumer, hoặc bất kỳ tác vụ chạy nền nào
> (timer polling, Kafka/RabbitMQ consumer...) trong `POS.Worker`. Đọc file này TRƯỚC khi
> tạo worker mới.

---

## Nguyên tắc cốt lõi — đặt code ở đâu

> **POS.Worker chỉ là host mỏng.** KHÔNG viết logic worker trong project `POS.Worker`.

| Thành phần | Vị trí | Ghi chú |
|---|---|---|
| Bootstrap / DI registration | `src/POS.Worker/Program.cs` | Chỉ đăng ký hosted service + config |
| **Implementation worker** | `src/POS.Infrastructure/Workers/` | `{Name}Worker.cs` — namespace `POS.Infrastructure.Workers` |
| State chia sẻ | `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton |
| Heartbeat | `src/POS.Infrastructure/Workers/WorkerHeartbeatService.cs` | Ghi Redis cho Ops monitoring |

**Lý do:** worker tái dùng repository/service/messaging đã có trong `POS.Infrastructure`;
đặt cùng layer tránh circular reference và cho phép POS.Web/Ops đọc `WorkerHealthState`,
heartbeat key cùng convention.

`POS.Worker.csproj` tham chiếu `POS.Application` + `POS.Infrastructure` — đủ để resolve mọi DI.

> **Ngoại lệ đã có tiền lệ**: nếu worker cần gọi trực tiếp 1 Application service (vd
> `IMasterDataSyncService`/`ISyncDataPosService`) chứ không chỉ Infrastructure repository, đặt
> worker đó ở `src/POS.Worker/Workers/` (namespace `POS.Worker.Workers`) thay vì
> `POS.Infrastructure/Workers/` — vì `POS.Infrastructure` **không được** reference
> `POS.Application` (ngược dependency flow). Khi đó `POS.Worker/Program.cs` phải thêm
> `builder.Services.AddApplication()` (nếu chưa có) — dễ quên vì trước giờ `Program.cs` chỉ gọi
> `AddInfrastructure()`. Ví dụ: `MasterDataZipGeneratorWorker.cs`. Nên thêm 1 test DI-resolution
> trong `tests/POS.ContractTests/DependencyInjectionTests.cs` (compose lại `AddInfrastructure()+
> AddApplication()`, assert constructor params resolve) để bắt lỗi thiếu `AddApplication()` lúc
> build/test thay vì lúc host khởi động thật.

---

## Bốn khuôn mẫu worker

| Pattern | Khi nào dùng | Ví dụ tham chiếu |
|---|---|---|
| **Message consumer (push)** | Nhận message từ RabbitMQ/Kafka liên tục | `PosSalesConsumerWorker.cs` |
| **Timer polling** | Chạy định kỳ (gọi SP, sync, cleanup) | `Rpt_ReportSaleDetail_Insert.cs` |
| **One-shot / cron-triggered** | Chạy 1 chu kỳ rồi thoát, lịch do crontab/systemd timer ngoài process quyết định — dùng khi worker cần chạy như 1 tiến trình riêng biệt trên host (không phải container dài hạn), vd tách theo mô hình deploy | `PosFileImportService.RunOnceAsync` (gọi từ `Program.cs --run-once`) |
| **Poll + fan-out song song + quarantine** | Phát hiện thay đổi (watermark) rồi áp dụng cho N đối tượng độc lập (terminal/store/tenant...) song song, cần cô lập đối tượng lỗi liên tục để không chặn cả lô | `MasterDataZipGeneratorWorker.cs` — xem pattern chi tiết bên dưới |

> Pattern thứ 3 **không kế thừa `BackgroundService`** — tách phần logic "1 chu kỳ" ra 1 class
> service thường (constructor injection, không cần `IHostedService`), rồi gọi trực tiếp từ
> `Program.cs` (thoát bằng `return <exit-code>` sau khi xong) HOẶC bọc trong vòng lặp
> `BackgroundService` nếu vẫn muốn chạy liên tục ở môi trường khác — cùng 1 class logic dùng được
> cho cả 2 cách gọi. Xem `PosFileImportWorker.cs` (wrapper vòng lặp) +
> `PosFileImportService.cs` (logic 1 chu kỳ, dùng chung).

---

## Quy tắc BẮT BUỘC chung cho mọi worker

1. Kế thừa `BackgroundService`, override `ExecuteAsync(CancellationToken)`.
2. Dùng **primary constructor** để inject — đồng nhất với code hiện có.
3. Worker là **singleton** → KHÔNG inject trực tiếp repository/service scoped.
   Inject `IServiceScopeFactory`, mỗi lần xử lý tạo `await using var scope = scopeFactory.CreateAsyncScope();`
   rồi `scope.ServiceProvider.GetRequiredService<I{X}Repository>()`.
   - Ngoại lệ: `IRedisService`, `IConfiguration`, `ILogger<T>`, `WorkerHealthState` là singleton → inject thẳng.
4. **Vòng lặp KHÔNG được chết:** bọc try/catch quanh phần xử lý, nuốt exception, set
   `healthState.Status = "Degraded"`, log error, rồi tiếp tục lặp.
5. Tôn trọng `stoppingToken`: bắt `OperationCanceledException` để thoát sạch khi app dừng.
6. Serialize/deserialize bằng **Newtonsoft.Json** (`JsonConvert.*`) — KHÔNG dùng `System.Text.Json`.
7. Cập nhật `healthState.IncrementProcessed()` sau mỗi item xử lý thành công.
8. Logging: prefix `[{WorkerName}]` trong mọi message để dễ filter trên Elasticsearch.

---

## Checklist tạo worker mới

1. **Tạo class** `{Name}Worker.cs` trong `src/POS.Infrastructure/Workers/` — `sealed`, kế thừa `BackgroundService`.
2. Chọn pattern (consumer push / timer polling) — copy template bên dưới.
3. Resolve dependency scoped qua `IServiceScopeFactory` (xem quy tắc #3).
4. Cập nhật `healthState` (Status + ProcessedCount).
5. **Đăng ký trong `Program.cs`:** `builder.Services.AddHostedService<{Name}Worker>();`
6. Nếu worker cần monitoring riêng → cân nhắc heartbeat (xem mục Heartbeat).
7. Build kiểm tra: `dotnet build src/POS.Worker/POS.Worker.csproj`.

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

---

## WorkerHealthState — state chia sẻ

`WorkerHealthState` (singleton) cho phép worker (writer) và `WorkerHeartbeatService` /
Ops UI (reader) chia sẻ trạng thái:

```csharp
healthState.Status = "Running";          // "Running" | "Degraded" | "Stopped"
healthState.IncrementProcessed();        // sau mỗi item OK (atomic, Interlocked)
healthState.ProcessedCount;              // đọc tổng đã xử lý
```

> `Status` là `string` → gán reference atomic, không cần lock. `ProcessedCount` dùng `Interlocked`.
> Hiện tại là **một state dùng chung**. Nếu thêm nhiều worker cần health riêng biệt,
> cân nhắc đổi sang `ConcurrentDictionary<string, ...>` keyed theo tên worker.

---

## Heartbeat → Redis (cho Ops monitoring)

`WorkerHeartbeatService` ghi `WorkerHeartbeat` vào Redis định kỳ để Ops dashboard biết worker còn sống:

- Key: `Worker:Heartbeat:{WorkerName}` (String).
- Interval 15s; TTL `60s` (~4× interval) khi chạy, `300s` khi "Stopped".
- Heartbeat **KHÔNG được crash worker** — nuốt mọi exception, chỉ log warning.
- Đọc lại từ Ops/POS.Web qua `IRedisService.StringGet<WorkerHeartbeat>(key)`; key hết hạn = worker chết.

Thêm worker cần monitor riêng → tạo heartbeat service tương tự với key/QueueName khác,
hoặc mở rộng `WorkerHeartbeatService` để ghi nhiều key.

---

## Đăng ký trong Program.cs

```csharp
var builder = Host.CreateApplicationBuilder(args);

builder.AddSerilogWithElastic();                        // logging → Elasticsearch
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSingleton<WorkerHealthState>();     // state chia sẻ (1 lần)
builder.Services.AddHostedService<MyScheduledWorker>(); // ← thêm worker mới ở đây
builder.Services.AddHostedService<WorkerHeartbeatService>();

builder.Build().Run();
```

> `AddInfrastructure` đã đăng ký repositories, Redis, RabbitMQ, DB factories — worker chỉ cần resolve.

**Feature toggle (`WorkerRoles`)**: `Program.cs` thật của dự án đọc section `WorkerRoles`
(`WorkerRolesOptions.cs`) để quyết định `AddHostedService<>` nào chạy — dùng khi cần tách 1 codebase
POS.Worker thành nhiều mô hình deploy (vd Docker container chỉ chạy RabbitMQ/SQL vs cronjob host chỉ
chạy file processing). Thêm worker mới cần bật/tắt độc lập theo môi trường → thêm cờ vào
`WorkerRolesOptions` + nhánh `if (roles.EnableX) AddHostedService<XWorker>()`, KHÔNG hardcode
`AddHostedService` vô điều kiện như ví dụ trên.

---

## KHÔNG làm những điều sau

- ❌ Viết logic worker trong project `POS.Worker` — đặt trong `POS.Infrastructure/Workers/`.
- ❌ Inject trực tiếp repository/service scoped vào worker singleton — dùng `IServiceScopeFactory`.
- ❌ Để exception thoát khỏi vòng lặp `ExecuteAsync` — worker sẽ chết âm thầm.
- ❌ `BasicNackAsync(requeue: true)` cho poison message — gây loop vô hạn; dùng dead-letter.
- ❌ Dùng `System.Text.Json` — phải dùng `Newtonsoft.Json`.
- ❌ Hardcode host/credentials — đọc từ `IConfiguration` (RabbitMQ/Redis) hoặc DB như AppService.
- ❌ Quên `AddHostedService<>` trong `Program.cs` — worker sẽ không chạy (không báo lỗi build).
- ❌ Để heartbeat/logging làm crash worker — luôn try/catch nuốt lỗi phụ trợ.
