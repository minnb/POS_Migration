---
name: worker-background-services
description: HOW tạo scheduled job / message consumer trong POS.Worker — 4 khuôn mẫu chọn pattern, code mẫu (templates.md), DI scope, health/heartbeat usage, đăng ký Program.cs. Rules (thin-host, 8 luật bắt buộc, 8 anti-pattern, heartbeat/WorkerRoles) ở .claude/rules/worker-standards.md.
---

# Skill: Background Worker — POS.Worker / Hosted Services

> **Áp dụng khi:** tạo scheduled job, message consumer, hoặc bất kỳ tác vụ chạy nền nào
> (timer polling, Kafka/RabbitMQ consumer...) trong `POS.Worker`. Đọc file này TRƯỚC khi
> tạo worker mới.
>
> **Rules (tiêu chuẩn bắt buộc — đọc TRƯỚC):** thin-host + vị trí đặt code, 8 quy tắc bắt buộc,
> 8 anti-pattern, heartbeat key/TTL + gotcha multi-instance, `WorkerRoles` toggle — xem
> **`.claude/rules/worker-standards.md`**. File này chỉ giữ 4 khuôn mẫu chọn pattern + code (HOW).

> **Mục lục nhanh:** `Bốn khuôn mẫu worker` → chọn đúng pattern (code mẫu ở [`templates.md`](templates.md)) ·
> `Checklist tạo worker mới` · `WorkerHealthState`/`Heartbeat` usage · `Đăng ký trong Program.cs`.

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

> 8 quy tắc bắt buộc (BackgroundService/primary ctor/IServiceScopeFactory/loop-must-not-die/
> stoppingToken/Newtonsoft/health increment/`[WorkerName]` prefix) là **Rules** — xem
> `.claude/rules/worker-standards.md`. Checklist dưới đây tham chiếu các quy tắc đó.

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

> **Pattern: cùng `WorkerRoles` chạy song song Docker + bare-metal trên cùng host** (2026-07-11,
> cập nhật 2026-07-15) — khi hạ tầng phụ thuộc (SQL Server, RabbitMQ) cũng chạy Docker trên chính
> host, container Worker và process bare-metal cần **địa chỉ khác nhau** tới cùng 1 hạ tầng:
> `host.docker.internal` (chỉ resolve được TRONG container, cần
> `--add-host host.docker.internal:host-gateway` lúc `docker run`) cho container, `127.0.0.1`
> (port đã publish ra host) cho bare-metal — dùng chung 1 file `appsettings.Production.json` sẽ
> khiến 1 bên kết nối sai địa chỉ. Giải pháp: tách file cấu hình riêng theo `DOTNET_ENVIRONMENT` —
> `appsettings.CronHost.json` cho **MỌI** tiến trình bare-metal trên host (dùng chung cho cả Model A
> cron lẫn Model C daemon và biến thể song song Docker này — không còn file riêng theo từng mô
> hình như `ProductionHost.json` cũ, đã xoá 2026-07-15), chỉ khác `RabbitMQ:Host`/
> `ConnectionStrings` (địa chỉ `127.0.0.1`) so với file Docker. Phân biệt vai trò giữa các tiến
> trình bare-metal hoàn toàn qua `WorkerRoles__*` override trong `Environment=` của từng unit file.
> Ví dụ thật: `docs/deploy/pos-worker-ubuntu-guide.md` mục 3.5 + mục 9. Nhớ tắt `EnableHeartbeat` ở
> 1 trong 2 bên (xem gotcha heartbeat ở mục trên) nếu cùng vai trò.

> ⚠️ **Gotcha đã gặp thực tế (2026-07-13)**: `appsettings.{DOTNET_ENVIRONMENT}.json` là **optional**
> trong ASP.NET Core Generic Host — thiếu file này KHÔNG làm crash app, chỉ âm thầm fallback về
> `appsettings.json` gốc. Nếu file gốc có path kiểu Windows (`D:\...`) và cron chạy trên Linux với
> `DOTNET_ENVIRONMENT=CronHost` mà `appsettings.CronHost.json` chưa tồn tại → job vẫn "chạy thành
> công" (exit code 0) mỗi lần nhưng không xử lý file thật, không ghi log thật (Serilog file sink
> không có `SelfLog`, lỗi ghi bị nuốt im lặng). **Trước khi tin file môi trường mới đã hoạt động**:
> xác nhận file đó **thực sự nằm trong thư mục publish** (`ls appsettings.*.json`), và xác nhận
> file đó **không bị `.gitignore` chặn** (`git check-ignore -v <path>`) — rule `*.json` mặc định
> chặn mọi file `.json` mới trừ khi có dòng `!**/appsettings.{Env}.json` ngoại lệ tương ứng.

---

## KHÔNG làm những điều sau

> 8 anti-pattern cấm (logic trong POS.Worker, inject scoped vào singleton, exception thoát loop,
> `BasicNackAsync(requeue:true)` poison, System.Text.Json, hardcode credentials, quên
> `AddHostedService`, heartbeat crash worker) là **Rules** — xem
> `.claude/rules/worker-standards.md` mục ❌ DON'T.
