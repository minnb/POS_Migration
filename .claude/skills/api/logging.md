---
name: api-logging
description: 3 cơ chế logging trong POS.Api/POS.Infrastructure (IFileLogHelper/IKibanaService/middleware request-response toàn cục) — khi nào dùng cái nào. Đọc TRƯỚC khi thêm log mới ở bất kỳ đâu.
---

# Skill: Logging trong POS.Api/POS.Infrastructure — 3 cơ chế, dùng cái nào khi nào

> **Đọc file này trước khi**: thêm log mới ở bất kỳ đâu (Controller/Service/Repository/Middleware),
> hoặc khi cần bật log request/response để debug 1 API chưa rõ POS gửi/nhận dữ liệu ra sao.

---

## Tổng quan — 3 cơ chế, KHÔNG trộn lẫn mục đích

| Cơ chế | Interface | Nơi ghi | Dùng khi |
|---|---|---|---|
| **File log thô** | `IFileLogHelper` | File `.txt` local (`File.AppendAllText`, đồng bộ) | Log nghiệp vụ rời rạc, exception cụ thể trong 1 method — KHÔNG dùng cho log chạy trên mọi request (I/O đồng bộ, tốn nếu gọi tần suất cao) |
| **Structured log → Kibana** | `IKibanaService` | Serilog → Console + File sink (`pos-*.log`) + Elasticsearch | Log có cấu trúc cần tra cứu/dashboard (request/response từng API nghiệp vụ, exception, response time) |
| **Request/Response toàn cục** | `RequestResponseLoggingMiddleware` (không phải service, tự chạy trong pipeline) | Qua `IKibanaService` — tự động, không cần gọi tay | Bật/tắt qua config để log **MỌI** API khi cần debug 1 đợt, không phải sửa code từng controller |

**Quy tắc chọn:**
- Cần log 1 dòng debug/exception cụ thể trong logic nghiệp vụ (không cần tra cứu structured) →
  `IFileLogHelper.WriteLogs`/`WriteExpLogs`.
- Cần log request/response cho 1 API nghiệp vụ cụ thể có ngữ cảnh riêng (posNo, note, response time
  đo được ngay tại chỗ) → gọi trực tiếp `IKibanaService.LogRequest`/`LogResponse`/`LogException`
  trong controller/service đó (xem ví dụ `PaymentController`, `LoyaltyController`).
- Cần log request/response cho **toàn bộ** API mà không sửa từng controller (vd đang debug 1 API
  "lạ" chưa rõ POS gửi gì) → dùng `RequestResponseLoggingMiddleware`, chỉ cần bật
  `RequestLogging:Enabled=true` trong config, KHÔNG viết thêm code.

---

## 1. `IFileLogHelper` — file text thô

`src/POS.Infrastructure/Logging/IFileLogHelper.cs` + `FileLogHelper.cs`:

```csharp
public interface IFileLogHelper
{
    void WriteLogs(string message);              // Logs/debug/log-yyyyMMdd.txt
    void WriteExpLogs(string function, Exception ex); // Logs/Exception/log-yyyyMMdd.txt
}
```

- Backing store: `File.AppendAllText` — **đồng bộ, mở/ghi/đóng file mỗi lần gọi**, bọc `lock` riêng
  cho từng loại file (`debug`/`Exception`) để tránh 2 thread cùng process ghi trùng thời điểm bị
  `IOException` nuốt lặng lẽ trong try/catch. Bọc try/catch nuốt lỗi ("logging must never throw").
  Đăng ký Singleton, `baseDirectory` từ `Logging:FileLogDirectory`, `retentionDays` từ
  `LogRetention:RawLogRetentionDays` — xem mục 5 "Cách cấu hình số ngày lưu Log trên Server".
- **Anti-pattern đã gặp thực tế**: gọi `File.AppendAllText` (hoặc tương tự) cho MỌI request của MỌI
  endpoint (thay vì vài chỗ nghiệp vụ) → bottleneck I/O dưới tải 5.000 POS. Đây là lý do
  `RequestResponseLoggingMiddleware` KHÔNG dùng `IFileLogHelper` mà dùng `IKibanaService` (đẩy qua
  Serilog — đã async/batch).
- **Anti-pattern khác đã gặp** (bug thực tế, xem CHANGELOG "Thay WinSCP bằng FluentFTP"): KHÔNG
  `JsonConvert.SerializeObject(ex)` khi log exception nếu `ex` có thể tham chiếu object đã bị
  dispose (session, connection...) — Newtonsoft reflection sẽ đệ quy vào property đó và tự ném lỗi
  thứ cấp, che giấu lỗi gốc. Dùng `ex.ToString()` thay thế.

---

## 2. `IKibanaService` — structured log → Serilog → Kibana

`src/POS.Infrastructure/Logging/IKibanaService.cs` + `KibanaService.cs`:

```csharp
public interface IKibanaService
{
    void LogRequest(string endpoint, string posNo, string requestBody);
    void LogResponse(string endpoint, string posNo, long responseTimeMs, string note, string responseBody);
    void LogException(string endpoint, string posNo, int errorCode, string note, string errorDetail);
    void LogInfo(string endpoint, string posNo, string message);
}
```

- Mỗi method: `_ = Task.Run(() => { using LogContext.PushProperty(...) x N; logger.LogWarning/LogError/LogInformation(...); })`
  — **fire-and-forget**, không block thread request.
- Field structured (`WebApi`, `PosNo`, `HttpContext`, `ResponseTime`, `ErrorCode`, `DeveloperMessage`,
  `trace_id`, `span_id`) giữ nguyên tên cũ để không vỡ dashboard Kibana đang có (ECS format nest dưới
  `labels.*`).
- **Field `HttpContext`** đánh dấu loại sự kiện: `"Request"` / `"Response"` (từ `LogRequest`/
  `LogResponse`) hoặc `"Exception"` / `"Info"` (từ `LogException`/`LogInfo`) — **dùng để filter ở
  File sink** (xem mục 4). Khi lọc theo property này ở bất kỳ đâu, PHẢI lọc theo **giá trị cụ thể**,
  không lọc theo sự tồn tại của property (4 loại sự kiện dùng chung tên property này).
- Gọi trực tiếp trong controller/service khi cần log nghiệp vụ cụ thể — ví dụ tham chiếu:
  `PaymentController.ValidateVoucher`, `LoyaltyController.GetCustomerDetail`.

---

## 3. Serilog pipeline (sink) — nơi `IKibanaService` thực sự ghi vào

`src/POS.Infrastructure/Logging/SerilogConfiguration.cs` (`ConfigureSerilogCore`) — dùng chung cho
POS.Api/POS.Web (`WebApplicationBuilder`) và POS.Worker (`HostApplicationBuilder`):

```
loggerConfig
    .WriteTo.Console(...)                                    // luôn bật
    .WriteTo.File(...)            // nếu Logging:FileLogDirectory có giá trị
    .WriteTo.Elasticsearch(...)   // nếu Elasticsearch:Nodes có giá trị (tự no-op nếu rỗng)
```

**Quan trọng — đây là fan-out, KHÔNG phải chọn 1 sink**: mỗi lời gọi log được ghi đồng thời vào
**mọi** sink đang cấu hình. Bật Elasticsearch KHÔNG tự tắt File sink.

### Cờ `RequestLogging:PersistToFile` — tách log Request/Response ra khỏi File sink

Vì `RequestResponseLoggingMiddleware` (mục 4) có thể sinh khối lượng log lớn (2 sự kiện/request ×
mọi endpoint), có cờ riêng quyết định File sink có nhận log Request/Response hay chỉ Elasticsearch:

```csharp
var persistRequestLogToFile = configuration.GetValue("RequestLogging:PersistToFile", defaultValue: true);
if (persistRequestLogToFile)
{
    loggerConfig.WriteTo.File(path: filePath, ...);   // nhận MỌI loại log như cũ
}
else
{
    loggerConfig.WriteTo.Logger(lc => lc
        .Filter.ByExcluding(evt =>
            evt.Properties.TryGetValue("HttpContext", out var v) &&
            v is ScalarValue { Value: "Request" or "Response" })
        .WriteTo.File(path: filePath, ...));   // loại trừ riêng Request/Response, Exception/Info vẫn ghi đủ
}
```

`retainedFileCountLimit`/`fileSizeLimitBytes` của `WriteTo.File` đọc từ `LogRetentionOptions`
(section `LogRetention`), không còn hardcode — xem mục 5.

- **Mặc định `true`** — vì Elasticsearch **hiện chưa được cài đặt** trong dự án, cần bản ghi trên
  đĩa server (`pos-*.log`) làm nơi tra cứu duy nhất. Đổi sang `false` sau khi Elasticsearch go-live
  ổn định để giảm I/O đĩa.
- Đọc bằng raw `IConfiguration.GetValue` (không qua `IOptions`) vì đây là lúc bootstrap Serilog,
  trước khi DI container đầy đủ — cùng cách đọc `fileLogDir`/`esOptions` trong method này.
- Log Exception/Info (`LogException`/`LogInfo`) **luôn** ghi đủ vào File sink bất kể cờ này.

---

## 4. `RequestResponseLoggingMiddleware` — log request/response toàn cục, bật/tắt qua config

`src/POS.Api/Middleware/RequestResponseLoggingMiddleware.cs` + `RequestLoggingOptions.cs`.

Thay thế hoàn toàn các lời gọi `LogRequest` thủ công rải rác trước đây (từng chỉ có ở 3/9
controller, không nhất quán — 1 kiểu ghi `IFileLogHelper` metadata-only, 1 kiểu gọi
`IKibanaService` với body). Đăng ký **ngoài cùng pipeline** trong `Program.cs` (trước
`UsePosExceptionHandling`) để bao trùm cả response lỗi chuẩn hoá:

```csharp
builder.Services.Configure<RequestLoggingOptions>(
    builder.Configuration.GetSection(RequestLoggingOptions.SectionName));
...
app.UseRequestResponseLogging();   // NGOÀI CÙNG — trước UsePosExceptionHandling()
app.UsePosExceptionHandling();
app.UseSerilogRequestLogging(options => { options.GetLevel = ...; });  // xem mục 4.1
```

### Cơ chế capture — pass-through, KHÔNG buffer file lớn

```csharp
private sealed class CappedCapturingStream(Stream inner, int maxBytes) : Stream
{
    // Write/WriteAsync: ghi thẳng ra `inner` (client nhận streaming bình thường, không delay),
    // đồng thời chỉ giữ lại tối đa `maxBytes` đầu tiên vào MemoryStream nội bộ để log.
    // RAM dùng cho capture bị chặn ở maxBytes bất kể response thực tế lớn bao nhiêu (vd DowloadFileStream
    // trả zip vài chục MB) — KHÔNG swap Response.Body sang MemoryStream rồi đọc lại toàn bộ sau.
}
```

- Request: `Request.EnableBuffering()` + đọc capped; **bỏ qua đọc nội dung** nếu
  `Content-Type: multipart/form-data*` (chỉ log metadata — tránh buffer file upload).
- Response: sau khi biết `Response.ContentType` cuối cùng, nếu là binary
  (`application/x-zip-compressed`, `application/octet-stream`, `application/zip`) → log
  `"[binary content-type ..., N bytes, không capture nội dung]"` thay vì nội dung capture được.
- Log qua `IKibanaService.LogRequest`/`LogResponse` — không viết cơ chế log riêng.
- Header loại trừ `Authorization`/`Cookie` trước khi log (giống pattern cũ trong
  `SyncDataPosController.LogRequest`).

### Config — `appsettings.json` section `RequestLogging`

```json
"RequestLogging": {
  "Enabled": false,
  "PersistToFile": true,
  "MaxBodyBytes": 8192,
  "ExcludePaths": [ "/health", "/swagger" ]
}
```

| Key | Ý nghĩa | Mặc định |
|---|---|---|
| `Enabled` | Bật/tắt toàn bộ tính năng — check **đầu tiên** trong middleware, tắt = chi phí gần 0 | `false` (mọi môi trường trừ Development) |
| `PersistToFile` | File sink có nhận log Request/Response hay chỉ Elasticsearch (đọc riêng trong `SerilogConfiguration.cs`, xem mục 3) | `true` |
| `MaxBodyBytes` | Cắt bớt body log quá dài | `8192` |
| `ExcludePaths` | Path bypass hoàn toàn (không log, không capture) | `["/health", "/swagger"]` |

- **`appsettings.Development.json`** override `RequestLogging:Enabled: true` — tiện bật sẵn lúc dev,
  không cần set biến môi trường thủ công mỗi lần debug.
- **UAT/Production**: giữ `Enabled: false` — bật tay khi cần debug 1 đợt cụ thể (đổi config,
  restart, không cần build lại), tắt lại sau khi xong. Xem `docs/ROLLOUT.md` §O4.

**Bug thực tế đã gặp**: verify tính năng lúc mới code bằng cách set `RequestLogging__Enabled=true`
qua biến môi trường PowerShell tạm — không lưu vào file config nào. Lần chạy sau (không set lại env
var) tưởng middleware không hoạt động, thực ra cấu hình quay về mặc định `false`. **Luôn kiểm tra
giá trị `RequestLogging:Enabled` đang hiệu lực trước khi nghi ngờ code không log.**

---

### 4.1 `UseSerilogRequestLogging` — GetLevel tùy biến để chặn nhiễu INF (2026-07-08)

> Áp dụng khi: `MinimumLevel:Default` đã hạ xuống `Warning` (xem mục 3) nhưng vẫn cần
> `UseSerilogRequestLogging()` (middleware `Serilog.AspNetCore`, KHÔNG phải
> `RequestResponseLoggingMiddleware` ở mục 4) ghi log 5xx/4xx thật sự có giá trị tra cứu, đồng
> thời không nuốt luôn log lỗi vì `Default=Warning` chặn hết mức `Information` mặc định của nó.

**Vấn đề gốc**: gọi `app.UseSerilogRequestLogging();` (không tham số) khiến middleware này tự log
ở mức `Information` cho MỌI request kể cả 2xx/3xx/404 — đây là nguồn sinh ra các dòng
`HTTP GET / responded 404`/`responded 200` ngập log, **độc lập** với `RequestResponseLoggingMiddleware`
(middleware mục 4, tắt qua `RequestLogging:Enabled=false`) — tắt middleware đó KHÔNG tắt được
nguồn nhiễu này.

**Giải pháp** — truyền `GetLevel` tùy biến, chỉ tại `src/POS.Api/Program.cs` (nơi duy nhất gọi
`UseSerilogRequestLogging`):

```csharp
using Serilog.Events;

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, elapsed, ex) =>
    {
        if (ex is not null || httpContext.Response.StatusCode >= 500)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode is >= 400 and not 404)
            return LogEventLevel.Warning;

        // 2xx/3xx thành công + 404 dò tìm thông thường: hạ xuống Debug — bị chặn
        // tự động bởi MinimumLevel.Default=Warning, không cần Filter.ByExcluding riêng.
        return LogEventLevel.Debug;
    };
});
```

**Quan trọng**:
- Không sửa `SerilogConfiguration.cs` cho việc này — `UseSerilogRequestLogging` chỉ được gọi ở
  `POS.Api/Program.cs` (POS.Web không dùng nó — Blazor Server, không có API pipeline tương tự;
  POS.Worker không có HTTP host nên không áp dụng được).
- 404 cố ý **loại trừ** khỏi nhánh `>= 400` để không biến log dò quét đường dẫn thông thường
  thành nhiễu ở mức Warning — chỉ 4xx nghiệp vụ thật (401/403/422...) mới lên Warning.
- Đi kèm thay đổi `Serilog:MinimumLevel:Default` từ `Information` → `Warning` trong
  `appsettings*.json` của **cả 3 project** (Api/Web/Worker) — xem bảng ở mục 3 và
  `docs/CHANGELOG.md` entry tương ứng. `Microsoft.Hosting.Lifetime` giữ nguyên `Information` để
  vẫn thấy log lúc start/stop.

> Ví dụ thực tế: `src/POS.Api/Program.cs`

---

## 5. Cách cấu hình số ngày lưu Log trên Server

`src/POS.Infrastructure/Logging/LogRetentionOptions.cs` — options class bind từ section
`LogRetention`, dùng chung cho cả 2 loại output file log (Serilog `pos-*.log` và `IFileLogHelper`
`debug/`+`Exception/` `.txt`):

```json
"LogRetention": {
  "SerilogRetainedFileCountLimit": 7,
  "SerilogFileSizeLimitBytes": null,
  "RawLogRetentionDays": 7
}
```

| Key | Ảnh hưởng output nào | Ý nghĩa | Mặc định nếu bỏ trống section |
|---|---|---|---|
| `SerilogRetainedFileCountLimit` | Serilog File sink (`pos-*.log`, `RollingInterval.Day` → mỗi file ≈ 1 ngày) | Giữ tối đa N file gần nhất, file cũ hơn tự xóa khi Serilog roll sang file mới | `14` |
| `SerilogFileSizeLimitBytes` | Serilog File sink | Giới hạn dung lượng/file trước khi Serilog tự roll thêm file phụ trong cùng ngày. `null` = không giới hạn | `null` (không giới hạn) |
| `RawLogRetentionDays` | `IFileLogHelper` (`debug/log-*.txt`, `Exception/log-*.txt`) | Xóa file cũ hơn N ngày. `<= 0` = tắt cleanup | `30` |

**Giá trị hiện tại của dự án**: Dev = 7 ngày, Production = 10 ngày (`appsettings.json` /
`appsettings.Production.json` của POS.Api, POS.Web, POS.Worker) — điều chỉnh theo dung lượng ổ đĩa
thực tế của từng server, không có con số "đúng" cố định.

### Cách đổi trên server đang chạy (không cần rebuild)

1. Sửa section `LogRetention` trong `appsettings.Production.json` (hoặc override qua biến môi
   trường, vd `LogRetention__RawLogRetentionDays=45`).
2. **Restart lại process** — Serilog đọc `LogRetentionOptions` lúc bootstrap (`AddSerilogWithElastic`),
   `FileLogHelper` đọc lúc `AddInfrastructure` dựng DI container — cả 2 đều chỉ đọc 1 lần lúc khởi
   động, không tự động áp dụng nếu chỉ sửa file mà không restart.

### Cơ chế dọn dẹp thực tế

- **Serilog**: dùng tính năng có sẵn của Serilog.Sinks.File (`retainedFileCountLimit`,
  `fileSizeLimitBytes`) — Serilog tự xóa file thừa mỗi khi roll sang file mới (đầu ngày mới, hoặc
  khi đạt `fileSizeLimitBytes` nếu có set).
- **`IFileLogHelper`**: **không dùng worker/cron riêng** — mỗi lần `WriteLogs`/`WriteExpLogs` được
  gọi, class tự kiểm tra đã quá 24h kể từ lần dọn dẹp trước chưa; nếu đúng, quét
  `debug/log-*.txt` và `Exception/log-*.txt`, xóa file có `LastWriteTimeUtc` cũ hơn
  `RawLogRetentionDays`. Lý do chọn cách này thay vì 1 `BackgroundService` trong POS.Worker: mọi
  process host `FileLogHelper` (POS.Api, POS.Web, POS.Worker) tự có retention mà không phụ thuộc
  POS.Worker có đang chạy hay không (`--run-once` cron mode của POS.Worker không đăng ký hosted
  service nào), và không cần POS.Worker nhìn thấy thư mục log vật lý khác của POS.Api/POS.Web.
- **Bỏ trống toàn bộ section `LogRetention`** → giữ nguyên hành vi trước khi có tính năng này
  (Serilog 14 ngày/không giới hạn size, `IFileLogHelper` không tự dọn) — thay đổi này là cộng thêm,
  tương thích ngược hoàn toàn.

---

## Checklist khi thêm log mới

- [ ] Log 1 dòng debug/exception cụ thể trong 1 method → `IFileLogHelper` (không dùng cho log chạy
  trên mọi request).
- [ ] Log request/response 1 API nghiệp vụ có ngữ cảnh riêng (posNo, note...) → gọi trực tiếp
  `IKibanaService` trong controller/service đó.
- [ ] Cần log toàn bộ request/response cho nhiều/mọi API để debug → dùng
  `RequestResponseLoggingMiddleware` có sẵn (chỉ đổi config `RequestLogging:Enabled`), KHÔNG viết
  thêm `LogRequest` thủ công trong controller (tránh log trùng lặp).
- [ ] Log exception → dùng `ex.ToString()` hoặc field cụ thể, KHÔNG `JsonConvert.SerializeObject(ex)`
  nếu `ex` có thể tham chiếu object đã dispose (connection, session, stream...).
- [ ] Thêm cờ ON/OFF mới cho 1 loại log → đặt trong section `RequestLogging` (hoặc section riêng
  tương tự), đọc qua `IOptions` ở tầng ứng dụng, hoặc raw `IConfiguration` nếu cần đọc lúc bootstrap
  Serilog (trước khi có DI đầy đủ) — xem cách `PersistToFile` được đọc trong mục 3.

---

## Ví dụ tham chiếu

| Thành phần | File |
|---|---|
| File log thô | `src/POS.Infrastructure/Logging/IFileLogHelper.cs`, `FileLogHelper.cs` |
| Structured log Kibana | `src/POS.Infrastructure/Logging/IKibanaService.cs`, `KibanaService.cs` |
| Serilog pipeline + `PersistToFile` | `src/POS.Infrastructure/Logging/SerilogConfiguration.cs` |
| Log Retention Policy (mục 5) | `src/POS.Infrastructure/Logging/LogRetentionOptions.cs`, section `LogRetention` trong `appsettings*.json` của POS.Api/POS.Web/POS.Worker |
| Middleware log toàn cục | `src/POS.Api/Middleware/RequestResponseLoggingMiddleware.cs`, `RequestLoggingOptions.cs` |
| Đăng ký pipeline | `src/POS.Api/Program.cs` |
| Config | `src/POS.Api/appsettings*.json` (section `RequestLogging`), `docs/ROLLOUT.md` §O4 |
| Log nghiệp vụ trực tiếp (không qua middleware) | `src/POS.Api/Controllers/PaymentController.cs`, `LoyaltyController.cs` |
