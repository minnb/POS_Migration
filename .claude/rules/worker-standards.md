# Rule: Worker Standards — POS.Worker / Background Services

## 🎯 Context (Khi nào áp dụng)
Khi tạo/sửa scheduled job, message consumer, hoặc bất kỳ `BackgroundService`/hosted service nào
cho `POS.Worker`. Đây là **tiêu chuẩn bắt buộc** (WHAT/WHY). 4 khuôn mẫu chọn pattern, code mẫu
copy-paste và checklist thực thi nằm ở **`.claude/skills/worker/SKILLS.md`** (+ `templates.md`).

## ✅ DO (Bắt buộc làm)
- **POS.Worker chỉ là host mỏng** — KHÔNG viết logic worker trong project `POS.Worker`. Đặt code
  đúng vị trí:

  | Thành phần | Vị trí | Ghi chú |
  |---|---|---|
  | Bootstrap / DI registration | `src/POS.Worker/Program.cs` | Chỉ đăng ký hosted service + config |
  | **Implementation worker** | `src/POS.Infrastructure/Workers/` | `{Name}Worker.cs` — namespace `POS.Infrastructure.Workers` |
  | State chia sẻ | `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton |
  | Heartbeat | `src/POS.Infrastructure/Workers/WorkerHeartbeatService.cs` | Ghi Redis cho Ops |

  > **Ngoại lệ có tiền lệ**: worker cần gọi trực tiếp Application service (vd
  > `IMasterDataSyncService`) → đặt ở `src/POS.Worker/Workers/` (namespace `POS.Worker.Workers`),
  > **KHÔNG** `POS.Infrastructure/Workers/` (`POS.Infrastructure` không được reference
  > `POS.Application`). Nhớ thêm `AddApplication()` vào `POS.Worker/Program.cs`.

- **8 quy tắc bắt buộc chung cho mọi worker:**
  1. Kế thừa `BackgroundService`, override `ExecuteAsync(CancellationToken)`.
  2. Dùng **primary constructor** để inject.
  3. Worker là **singleton** → KHÔNG inject repository/service scoped trực tiếp; inject
     `IServiceScopeFactory`, mỗi lần xử lý `await using var scope = scopeFactory.CreateAsyncScope();`.
     Ngoại lệ inject thẳng: `IRedisService`, `IConfiguration`, `ILogger<T>`, `WorkerHealthState`.
  4. **Vòng lặp KHÔNG được chết**: try/catch quanh phần xử lý, nuốt exception, set
     `healthState.Status = "Degraded"`, log, tiếp tục lặp.
  5. Tôn trọng `stoppingToken`: bắt `OperationCanceledException` để thoát sạch khi app dừng.
  6. Serialize bằng **Newtonsoft.Json** — KHÔNG `System.Text.Json`.
  7. Cập nhật `healthState.IncrementProcessed()` sau mỗi item thành công.
  8. Logging: prefix `[{WorkerName}]` trong mọi message để filter trên Elasticsearch.

- **Heartbeat conventions** (khi worker cần Ops monitor): key `Worker:Heartbeat:{WorkerName}`
  (String), interval 15s, TTL `60s` (~4× interval) khi chạy / `300s` khi "Stopped"; heartbeat
  KHÔNG được crash worker (nuốt exception, log warning).
  > **Gotcha multi-instance**: chạy 2 instance cùng vai trò → cả 2 ghi đè cùng 1 key heartbeat,
  > che giấu instance chết. Khi cố ý multi-instance cùng vai trò, **tắt `EnableHeartbeat` ở tất cả
  > trừ 1 instance** cho tới khi có heartbeat theo instance riêng.

- **Feature toggle `WorkerRoles`**: worker cần bật/tắt độc lập theo môi trường → thêm cờ vào
  `WorkerRolesOptions` + nhánh `if (roles.EnableX) AddHostedService<XWorker>()`. KHÔNG hardcode
  `AddHostedService` vô điều kiện.

## ❌ DON'T (Tuyệt đối cấm)
- ❌ Viết logic worker trong project `POS.Worker` — đặt trong `POS.Infrastructure/Workers/`.
- ❌ Inject trực tiếp repository/service scoped vào worker singleton — dùng `IServiceScopeFactory`.
- ❌ Để exception thoát khỏi vòng lặp `ExecuteAsync` — worker chết âm thầm.
- ❌ `BasicNackAsync(requeue: true)` cho poison message — loop vô hạn; dùng dead-letter.
- ❌ Dùng `System.Text.Json` — phải dùng `Newtonsoft.Json`.
- ❌ Hardcode host/credentials — đọc từ `IConfiguration` (RabbitMQ/Redis) hoặc DB như AppService.
- ❌ Quên `AddHostedService<>` trong `Program.cs` — worker không chạy (không báo lỗi build).
- ❌ Để heartbeat/logging làm crash worker — luôn try/catch nuốt lỗi phụ trợ.

---

> 4 khuôn mẫu chọn pattern (consumer push / timer polling / one-shot / poll+fan-out+quarantine),
> code mẫu copy-paste, `WorkerHealthState` usage, đăng ký `Program.cs`, pattern Docker/bare-metal:
> **`.claude/skills/worker/SKILLS.md`** (+ `templates.md`) — KHÔNG lặp lại mandate ở đây.
