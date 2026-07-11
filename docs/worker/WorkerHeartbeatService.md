# WorkerHeartbeatService / WorkerHealthState — Chi tiết kỹ thuật

> Tổng hợp logic kỹ thuật, ràng buộc DB (không có — pure Redis), và các "gotcha" phát hiện được khi
> rà soát code (2026-07-10). Tổng quan/inventory: `docs/worker/worker_status.md` mục 1.1 (#4), 1.2.
> File này chứa **gotcha nghiêm trọng nhất trong toàn bộ POS.Worker**: heartbeat có thể chẩn đoán sai.

---

## 1. Vị trí file

| File | Vai trò |
|---|---|
| `src/POS.Infrastructure/Workers/WorkerHeartbeatService.cs` | `BackgroundService` — publish heartbeat Redis mỗi 15s |
| `src/POS.Infrastructure/Workers/WorkerHealthState.cs` | Singleton `Status`/`ProcessedCount` — **bị 2 worker khác nhau ghi chung** |
| `src/POS.Application/Features/Common/HealthCheckService.cs` | Nơi ĐỌC heartbeat, quyết định hiển thị gì trên `/ops/health` |
| `src/POS.Web/Components/Pages/Ops/HealthPage.razor` | UI hiển thị |
| `src/POS.Api/Controllers/CommonController.cs` | Endpoint `GET api/common/CheckConnection` — gọi cùng `HealthCheckService` nhưng **thiếu config** (xem Gotcha #3) |

---

## 2. Logic kỹ thuật

### 2.1 `WorkerHeartbeatService` — full body (đã rút gọn, giữ nguyên số liệu)

```csharp
private const string RedisKey       = "Worker:Heartbeat:PosSalesConsumer";   // HARDCODE
private const string QueueName      = "pos_sales";                            // chỉ để hiển thị, không dùng để filter
private const int    IntervalSeconds = 15;   // HARDCODE, không đọc config
private const int    NormalTtl       = 60;   // ~4× interval
private const int    StoppedTtl      = 300;

protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    using var timer = new PeriodicTimer(TimeSpan.FromSeconds(IntervalSeconds));
    try { while (await timer.WaitForNextTickAsync(stoppingToken))
              WriteHeartbeat(healthState.Status, NormalTtl); }
    catch (OperationCanceledException) { }
    finally { WriteHeartbeat("Stopped", StoppedTtl); }   // Status="Stopped" LITERAL, không đọc healthState.Status
}

private void WriteHeartbeat(string status, int ttlSeconds)
{
    var hb = new WorkerHeartbeat {
        WorkerName = "PosSalesConsumer",       // HARDCODE — không phản ánh worker THẬT gây ra Status hiện tại
        Status = status,
        InstanceId = Environment.MachineName,
        LastBeatUtc = DateTime.UtcNow,
        QueueName = QueueName,
        ProcessedCount = healthState.ProcessedCount,   // ĐỌC từ singleton dùng chung
    };
    redis.StringSet(RedisKey, hb, ttlSeconds: ttlSeconds);   // bọc try/catch — lỗi Redis chỉ log Warning
}
```

**Không có DB access nào** — constructor chỉ nhận `IRedisService`, `WorkerHealthState`, `ILogger`. Toàn bộ là Redis String write + đọc state in-memory.

### 2.2 `WorkerHealthState` — singleton, và AI đã xác nhận comment trong code SAI/THIẾU

```csharp
/// <summary>
/// Singleton chia sẻ trạng thái giữa PosSalesConsumerWorker (writer) và WorkerHeartbeatService (reader).
/// </summary>
public sealed class WorkerHealthState
{
    private long _processedCount;
    public string Status { get; set; } = "Running";              // string assignment atomic — an toàn không cần lock
    public long ProcessedCount => Interlocked.Read(ref _processedCount);
    public void IncrementProcessed() => Interlocked.Increment(ref _processedCount);
}
```

Comment XML doc nói singleton này chỉ chia sẻ giữa `PosSalesConsumerWorker` và `WorkerHeartbeatService` — **SAI, thiếu 1 writer**. Grep toàn bộ `src/` xác nhận **2 writer thật sự**, cả hai đăng ký `AddSingleton<WorkerHealthState>()` dùng chung 1 instance (`POS.Worker/Program.cs`):

| Writer | Ghi gì |
|---|---|
| `PosSalesConsumerWorker` | `Status="Running"` (khi consume OK), `Status="Degraded"` (lỗi kết nối), `IncrementProcessed()` (mỗi message insert thành công) |
| `Rpt_ReportSaleDetail_Insert` | `Status="Running"` (lúc start), `IncrementProcessed()` (mỗi tick SP thành công), `Status="Degraded"` (SP lỗi) |

Cả 2 hosted service **cùng bật mặc định** ở mọi file cấu hình thật (`appsettings.json`, `appsettings.Production.json`, `appsettings.UAT.json` đều có `EnableRabbitMQConsumer=true` VÀ `EnableSqlReportWorker=true`) — chỉ `appsettings.CronHost.json` (Model A, file-import-only) tắt cả hai. Nghĩa là **trong mọi triển khai dài hạn thật (Production/UAT/dev Docker), 2 worker này LUÔN cùng ghi vào 1 singleton** — không phải tình huống hiếm/giả định.

`PosFileImportWorker`, `MasterDataZipGeneratorWorker`, `SyncTableCounterFlushWorker` **KHÔNG chạm vào `WorkerHealthState`** — mỗi worker này tự ghi heartbeat riêng dưới key riêng (`Worker:Heartbeat:PosFileImport`, `Worker:Heartbeat:MasterDataZipGenerator`, `Worker:Heartbeat:SyncTableCounterFlush-{instance}`), không đi qua `WorkerHeartbeatService`.

---

## 3. ⚠️ Gotcha nghiêm trọng nhất — heartbeat "PosSalesConsumer" thực chất là chỉ số TRỘN LẪN 2 worker

**Kịch bản cụ thể đã xác nhận qua đọc code, không suy đoán:**

1. `Rpt_ReportSaleDetail_Insert` gọi SP thất bại (vd SQL timeout thoáng qua) → `healthState.Status = "Degraded"` (dòng trong chính worker đó).
2. Tick kế tiếp của `WorkerHeartbeatService` (trong vòng 15s) đọc `healthState.Status` = `"Degraded"` → publish lên Redis key `Worker:Heartbeat:PosSalesConsumer` với `WorkerName="PosSalesConsumer"`, `Status="Degraded"`.
3. `HealthCheckService.CheckWorkerHeartbeatAsync` có message cứng cho `Status=="Degraded"`:
   ```csharp
   if (hb.Status == "Degraded")
       return Item(..., false, ..., $"Suy giảm — mất kết nối RabbitMQ | nhịp cuối {age}s trước");
   ```
4. **`/ops/health` hiển thị "mất kết nối RabbitMQ" — trong khi RabbitMQ hoàn toàn khỏe, lỗi thật nằm ở SQL report job.**

Tương tự, `ProcessedCount` hiển thị trên UI (`"Processed: {N:N0} msg"`) là **tổng cộng dồn của cả 2 nguồn** (message RabbitMQ insert thành công + tick SP report thành công) — không thể tách ra được số nào thuộc worker nào chỉ từ giá trị Redis.

**Không có lock/ordering giữa 2 writer** — không phải race điều kiện memory-unsafe (string assignment atomic), nhưng là **race điều kiện logic**: ai ghi `Status` sau cùng quyết định toàn bộ heartbeat phản ánh cái gì, không có cách nào truy ngược nguồn gốc.

### Bảng heartbeat key thật sự tồn tại (để đối chiếu, không cái nào khác đi qua cơ chế lỗi này)

| Key | Worker | Cơ chế |
|---|---|---|
| `Worker:Heartbeat:PosSalesConsumer` | **`PosSalesConsumerWorker` + `Rpt_ReportSaleDetail_Insert` (TRỘN LẪN)** | `WorkerHeartbeatService` — bị ảnh hưởng gotcha này |
| `Worker:Heartbeat:PosFileImport` | `PosFileImportWorker` | Tự ghi riêng, độc lập |
| `Worker:Heartbeat:MasterDataZipGenerator` | `MasterDataZipGeneratorWorker` | Tự ghi riêng, độc lập |
| `Worker:Heartbeat:SyncTableCounterFlush-{instance}` | `SyncTableCounterFlushWorker` | Tự ghi riêng, độc lập (POS.Api/POS.Web, không phải POS.Worker) |

---

## 4. Gotcha #2 — `HealthCheck:WorkerName` chỉ có ở `POS.Web`, POS.Api thiếu hoàn toàn

`HealthCheckService.CheckAllAsync` đọc:
```csharp
var workerName = configuration["HealthCheck:WorkerName"] ?? "DataSync";   // fallback "DataSync"
```
rồi check key `Worker:Heartbeat:{workerName}` — **chỉ 1 giá trị scalar, không phải mảng** (chỉ có đúng 1 lời gọi `CheckWorkerHeartbeatAsync` cho cả hàm).

**Xác nhận qua grep toàn bộ `appsettings*.json` trong repo**: chỉ **`src/POS.Web/appsettings.json`** có section `"HealthCheck": { "WorkerName": "PosSalesConsumer" }`. **`POS.Api` không có section này ở BẤT KỲ file appsettings nào** (base/Development/UAT/Production).

**Hệ quả 2 caller khác nhau cho kết quả khác nhau:**
- `HealthPage.razor` (POS.Web) → `workerName` resolve đúng `"PosSalesConsumer"` → kiểm tra đúng key tồn tại.
- `CommonController.cs` (`GET api/common/CheckConnection`, POS.Api) → không có config → fallback `"DataSync"` → kiểm tra key `Worker:Heartbeat:DataSync` — **KHÔNG worker nào từng ghi key này bao giờ** → mục "Worker" trên response của endpoint này **LUÔN LUÔN báo lỗi cố định** ("Key không tồn tại — worker chưa chạy hoặc đã dừng quá 5 phút"), bất kể tình trạng worker thật sự ra sao. Đây là gotcha **độc lập** với gotcha #1, ảnh hưởng riêng tới endpoint API (không ảnh hưởng trang Web).

---

## 5. Gotcha #3 — 2 ngưỡng thời gian độc lập, không liên kết với nhau

| Ngưỡng | Giá trị | Nơi định nghĩa |
|---|---|---|
| TTL Redis khi `Running` | 60s | `WorkerHeartbeatService.NormalTtl` |
| Ngưỡng "mất tín hiệu" khi đọc | **45s** | `HealthCheckService.CheckWorkerHeartbeatAsync` (`age.TotalSeconds > 45`) |

45s < 60s: heartbeat bị coi là "mất tín hiệu" ở phía đọc **trước khi** key Redis thực sự hết hạn (chỉ chậm 3 tick × 15s là đã bị coi mất tín hiệu, dù key vẫn còn sống thêm 15s nữa). Hai hằng số này **không tính ra từ nhau** (không phải 1 cái = k × cái kia trong code) — nếu sau này ai đổi `IntervalSeconds` mà quên đổi cả 2 nơi, quan hệ giữa chúng sẽ lệch thêm.

`Status=="Stopped"` được check **trước** ngưỡng tuổi — worker vừa tắt sạch sẽ (heartbeat "Stopped" mới ghi 1 giây trước) vẫn bị báo `Ok=false` ("Worker đã dừng có chủ ý") giống hệt như một outage thật, chỉ khác ở message text hiển thị.

---

## 6. Gotchas khác (nhỏ hơn)

- **`finally` ghi `Status="Stopped"` LITERAL**, không đọc `healthState.Status` — dù `healthState.Status` lúc đó có thể là `"Degraded"` (do 1 trong 2 worker để lại), heartbeat cuối cùng vẫn luôn ghi đè thành `"Stopped"`. Đây là hành vi **có chủ đích và đúng** (muốn báo "đã dừng" rõ ràng khi shutdown), chỉ cần lưu ý khi debug: `Status="Stopped"` không có nghĩa là mọi thứ đã "Running" trước đó.
- **`QueueName="pos_sales"` trong heartbeat chỉ mang tính hiển thị** — không dùng để filter/join gì, và bản thân literal này bị hardcode độc lập ở 3 nơi khác nhau trong codebase (xem `docs/worker/PosSalesConsumerWorker.md` §4), không phải hằng số dùng chung.
- **Redis write bọc try/catch chỉ log Warning** — Redis down không làm crash `WorkerHeartbeatService`, nhưng đồng nghĩa **toàn bộ hệ thống giám sát heartbeat mù hoàn toàn** trong lúc đó (không có fallback nào khác báo hiệu worker sống/chết).

---

## 7. Khuyến nghị (ghi nhận — CHƯA implement, ngoài phạm vi rà soát này)

Đã có ghi nhận tương tự ở `docs/worker/worker_status.md` mục 5 (đề xuất #3: đổi `HealthCheckService.CheckAllAsync` sang duyệt **mảng** tên worker thay vì 1 giá trị đơn). Rà soát này bổ sung thêm 2 điểm cụ thể nếu có ai chọn khắc phục sau này:
- Tách `WorkerHealthState` thành 2 instance riêng biệt (1 cho RabbitMQ consumer, 1 cho SQL report worker) thay vì dùng chung 1 singleton — hoặc đổi `WorkerHeartbeatService` sang publish theo tên worker thực tế thay vì hardcode `"PosSalesConsumer"`.
- Thêm section `"HealthCheck"` vào `POS.Api/appsettings.json` để `CommonController`/`api/common/CheckConnection` không luôn báo lỗi cố định cho mục "Worker".

---

## 8. Trạng thái verify

- Đã đọc toàn bộ `WorkerHeartbeatService.cs` (61 dòng), `WorkerHealthState.cs` (17 dòng), `HealthCheckService.cs` (245 dòng, đầy đủ), `CommonController.cs` (đoạn gọi `CheckAllAsync`), `HealthPage.razor` (đoạn gọi `CheckAllAsync`).
- Đã grep toàn bộ `src/` cho mọi điểm ghi vào `WorkerHealthState` (`.Status =`, `IncrementProcessed()`) — xác nhận đúng 2 writer, không hơn không kém.
- Đã grep toàn bộ `appsettings*.json` trong repo cho `"HealthCheck"` — xác nhận chỉ `POS.Web` có section này.
- **CHƯA verify runtime** (cần Redis + cả 2 worker chạy thật để quan sát heartbeat trộn lẫn xảy ra trực tiếp trên `/ops/health`) — mọi phát hiện dựa trên đọc code tĩnh + suy luận logic từ đó, không phải quan sát trực tiếp hiện tượng "chẩn đoán sai" trên môi trường thật.
