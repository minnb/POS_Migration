---
name: worker-background-services
description: Quy tắc bắt buộc cho scheduled job / message consumer trong POS.Worker — nơi đặt code, 4 khuôn mẫu, DI scope, health/heartbeat. Đọc TRƯỚC khi tạo worker mới; code mẫu copy-paste ở templates.md.
---

# Skill: Background Worker — POS.Worker / Hosted Services

> **Áp dụng khi:** tạo scheduled job, message consumer, hoặc bất kỳ tác vụ chạy nền nào
> (timer polling, Kafka/RabbitMQ consumer...) trong `POS.Worker`. Đọc file này TRƯỚC khi
> tạo worker mới.

> **Mục lục nhanh** (quay lại lần sau, nhảy thẳng tới mục cần — không cần đọc lại từ đầu):
> `Nguyên tắc cốt lõi` → đặt code worker ở project/namespace nào · `Bốn khuôn mẫu worker` → chọn
> đúng pattern trước khi viết (code mẫu ở [`templates.md`](templates.md)) · `Quy tắc BẮT BUỘC`
> (8 mục) + `Checklist tạo worker mới` (7 bước) → đọc trước khi commit ·
> `WorkerHealthState`/`Heartbeat` → state chia sẻ cho Ops monitoring (có gotcha 2 instance cùng
> key Redis) · `Đăng ký trong Program.cs` → DI + `WorkerRoles` toggle + pattern Docker/bare-metal ·
> `KHÔNG làm những điều sau` → 8 anti-pattern cấm.

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

> **Ngoại lệ đã có tiền lệ**: worker cần gọi trực tiếp 1 Application service (vd
> `IMasterDataSyncService`/`ISyncDataPosService`), không chỉ Infrastructure repository → đặt ở
> `src/POS.Worker/Workers/` (namespace `POS.Worker.Workers`), KHÔNG phải `POS.Infrastructure/Workers/`
> (`POS.Infrastructure` không được reference `POS.Application` — ngược dependency flow). Nhớ thêm
> `builder.Services.AddApplication()` vào `POS.Worker/Program.cs` (dễ quên — trước giờ chỉ gọi
> `AddInfrastructure()`). Ví dụ: `MasterDataZipGeneratorWorker.cs`. Nên có 1 test DI-resolution
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

## Template A/B + Pattern C — code mẫu copy-paste

> Chuyển sang file riêng: **[`templates.md`](templates.md)** — Template A (Timer polling), Template
> B (Message consumer RabbitMQ), Pattern C (Poll + fan-out song song + quarantine, dùng khi 1 tick
> phát hiện thay đổi cần áp dụng cho N đối tượng độc lập song song). Đọc đúng template theo pattern
> đã chọn ở mục "Bốn khuôn mẫu worker" trên.

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

> ⚠️ **Gotcha đã gặp thật (2026-07-11)**: `WorkerHeartbeatService` hardcode `WorkerName =
> "PosSalesConsumer"` — nếu chạy **2 instance cùng vai trò** (vd 1 container Docker + 1 process
> bare-metal cùng bật `EnableRabbitMQConsumer`/`EnableSqlReportWorker`), cả 2 ghi ĐÈ lên CÙNG 1 key
> Redis `Worker:Heartbeat:PosSalesConsumer` — Ops không phân biệt được instance nào, và 1 instance
> chết có thể bị che giấu bởi instance còn lại vẫn ghi đều. Khi cố ý chạy multi-instance cùng vai
> trò, **tắt `EnableHeartbeat` ở tất cả trừ 1 instance** cho tới khi thiết kế heartbeat theo tên
> worker/instance riêng (chưa làm). Chi tiết: `docs/worker/WorkerHeartbeatService.md` §3.

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

> **Pattern: cùng `WorkerRoles` chạy song song Docker + bare-metal trên cùng host** (2026-07-11) —
> khi hạ tầng phụ thuộc (SQL Server, RabbitMQ) cũng chạy Docker trên chính host, container Worker
> và process bare-metal cần **địa chỉ khác nhau** tới cùng 1 hạ tầng: `host.docker.internal` (chỉ
> resolve được TRONG container, cần `--add-host host.docker.internal:host-gateway` lúc
> `docker run`) cho container, `127.0.0.1` (port đã publish ra host) cho bare-metal — dùng chung 1
> file `appsettings.Production.json` sẽ khiến 1 bên kết nối sai địa chỉ. Giải pháp: tách file cấu
> hình riêng theo `DOTNET_ENVIRONMENT` (giống mô hình `CronHost` của Model A) — vd
> `appsettings.ProductionHost.json` cho bare-metal, chỉ khác `RabbitMQ:Host`/`ConnectionStrings`
> (địa chỉ) + `Logging:FileLogDirectory`/`Elasticsearch:IndexFormat` (tránh lẫn log) so với file
> Docker. Ví dụ thật: `src/POS.Worker/appsettings.ProductionHost.json` +
> `docs/deploy/pos-worker-ubuntu-guide.md` mục 3.5. Nhớ tắt `EnableHeartbeat` ở 1 trong 2 bên (xem
> gotcha heartbeat ở mục trên) nếu cùng vai trò.

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
