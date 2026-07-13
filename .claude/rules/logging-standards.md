# Rule: Logging Standards — POS.Api / POS.Infrastructure

## 🎯 Context (Khi nào áp dụng)
Khi thêm/sửa log ở bất kỳ đâu (Controller/Service/Repository/Middleware) hoặc bật log request/
response để debug. Đây là **tiêu chuẩn bắt buộc** (WHAT/WHY — chọn cơ chế nào, cấm gì). Interface
signature, code (`CappedCapturingStream`/`GetLevel`), cách đổi retention khi đang chạy, checklist
thực thi nằm ở **`.claude/skills/api/logging.md`**.

## ✅ DO (Bắt buộc làm)
- **3 cơ chế logging — KHÔNG trộn lẫn mục đích:**

  | Cơ chế | Interface | Dùng khi |
  |---|---|---|
  | **File log thô** | `IFileLogHelper` | Log nghiệp vụ rời rạc, exception cụ thể trong 1 method — KHÔNG cho log chạy trên mọi request (I/O đồng bộ) |
  | **Structured → Kibana** | `IKibanaService` | Log có cấu trúc cần tra cứu/dashboard (request/response 1 API nghiệp vụ, exception, response time) |
  | **Request/Response toàn cục** | `RequestResponseLoggingMiddleware` | Log MỌI API khi debug 1 đợt — bật/tắt qua config, KHÔNG sửa code từng controller |

- **Quy tắc chọn cơ chế:**
  - Log 1 dòng debug/exception cụ thể trong logic nghiệp vụ → `IFileLogHelper.WriteLogs`/`WriteExpLogs`.
  - Log request/response 1 API nghiệp vụ có ngữ cảnh riêng (posNo, note, response time) → gọi
    trực tiếp `IKibanaService.LogRequest/LogResponse/LogException` trong controller/service đó.
  - Log request/response **toàn bộ** API mà không sửa từng controller → dùng
    `RequestResponseLoggingMiddleware`, chỉ bật `RequestLogging:Enabled=true` (không viết thêm code).
- **Serilog là fan-out, KHÔNG phải chọn 1 sink**: mỗi lời gọi log ghi đồng thời vào **mọi** sink
  đang cấu hình (Console + File + Elasticsearch). Bật Elasticsearch KHÔNG tự tắt File sink.
- **Config governance:**
  - Bật/tắt log request/response toàn cục qua `RequestLogging:Enabled` (mặc định `false` mọi môi
    trường trừ Development). UAT/PROD giữ `false`, bật tay khi cần debug rồi tắt lại.
  - Cờ mới bật/tắt 1 loại log → đặt trong section `RequestLogging` (hoặc section riêng tương tự),
    đọc qua `IOptions`; nếu cần đọc lúc bootstrap Serilog (trước khi có DI đầy đủ) → raw
    `IConfiguration.GetValue` (như `RequestLogging:PersistToFile`).
  - Retention đọc từ section `LogRetention` (`SerilogRetainedFileCountLimit`/`SerilogFileSizeLimitBytes`/
    `RawLogRetentionDays`) — chỉ đọc 1 lần lúc bootstrap, đổi giá trị phải **restart** process.
- **Log level cho `UseSerilogRequestLogging`**: 5xx/exception → `Error`; 4xx nghiệp vụ (401/403/422)
  → `Warning`; 2xx/3xx thành công + **404 loại trừ** → `Debug` (bị `MinimumLevel.Default=Warning`
  chặn tự động). 404 KHÔNG được lên `Warning` (dò quét path là nhiễu, không phải lỗi nghiệp vụ).

## ❌ DON'T (Tuyệt đối cấm)
- Cấm gọi `File.AppendAllText`/`IFileLogHelper` cho MỌI request của MỌI endpoint (bottleneck I/O
  đồng bộ dưới tải 5.000 POS) — đó là lý do middleware dùng `IKibanaService` (Serilog async/batch),
  KHÔNG dùng `IFileLogHelper`.
- Cấm `JsonConvert.SerializeObject(ex)` khi log exception nếu `ex` có thể tham chiếu object đã
  dispose (session/connection/stream) — Newtonsoft đệ quy vào property đó, ném lỗi thứ cấp che giấu
  lỗi gốc. Dùng `ex.ToString()` hoặc field cụ thể.
- Cấm thêm `LogRequest` thủ công trong controller khi đã bật `RequestResponseLoggingMiddleware`
  (log trùng lặp).
- Cấm lọc property `HttpContext` theo sự **tồn tại** — phải lọc theo **giá trị cụ thể**
  (`"Request"`/`"Response"`/`"Exception"`/`"Info"` dùng chung 1 tên property).
- Cấm đổi tên field structured (`WebApi`/`PosNo`/`HttpContext`/`ResponseTime`/`ErrorCode`/
  `DeveloperMessage`/`trace_id`/`span_id`) — vỡ dashboard Kibana đang có.

---

> Interface signature, `CappedCapturingStream`, `GetLevel` code, bảng chi tiết key `RequestLogging`/
> `LogRetention`, cách đổi retention trên server đang chạy, cơ chế dọn dẹp, checklist:
> **`.claude/skills/api/logging.md`** — KHÔNG lặp lại quy tắc chọn/anti-pattern ở đây.
