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

# Rule: Đọc file/thư mục hệ thống để hiển thị lên UI (log viewer, file browser)

## 🎯 Context (Khi nào áp dụng)
Khi viết service/page **liệt kê hoặc tải file/thư mục trên đĩa** để hiển thị cho vận hành (ví dụ
trang `/admin/logs` — `LogFileService`/`LogFilePage.razor`). Rút ra từ sự cố thực tế: trang log
báo "Thư mục này không có file log nào" trong khi thư mục có rất nhiều log — nguyên nhân là code
**nuốt exception thầm lặng** và dùng `Directory.Exists` làm cổng chặn (che giấu cả lỗi phân quyền).

## ✅ DO (Bắt buộc làm)
- **Phân loại và surface lỗi I/O lên UI** — KHÔNG `catch { return empty; }` thầm lặng. Bắt riêng:
  - `UnauthorizedAccessException` → thông báo quyền **kèm đường dẫn tuyệt đối + tài khoản tiến
    trình đang chạy** (`WindowsIdentity.GetCurrent().Name` trên Windows, fallback
    `Environment.UserName` trên Linux) để vận hành biết cấp quyền cho account nào.
  - `DirectoryNotFoundException`/`FileNotFoundException` → "không tồn tại: {path}".
  - Còn lại → "Lỗi đọc {path}: {ex.Message}"; vẫn ghi `IFileLogHelper.WriteExpLogs`.
  - Trả lỗi qua field trên DTO kết quả (vd `LogDirectoryListing.ErrorMessage`,
    `LogFileDownload.ErrorMessage`) rồi hiển thị `MudAlert`/`Snackbar` — KHÔNG trả `null`/list rỗng
    làm mất thông tin lý do.
- **Luôn hiển thị đường dẫn gốc tuyệt đối đang quét** lên UI (caption) — giúp chẩn đoán nhanh cấu
  hình sai/nhầm thư mục cha-con.
- **KHÔNG dùng `Directory.Exists`/`File.Exists` làm cổng chặn duy nhất** trước khi enumerate/read:
  cả hai trả `false` khi bị **chặn quyền** (nuốt `UnauthorizedAccessException`), khiến UI báo
  "rỗng" sai sự thật. Để thao tác enumerate/read ném lỗi thật rồi phân loại như trên.
- **Đọc file đang được ghi** (log của hôm nay): mở bằng
  `new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)` — `File.ReadAllBytes`
  (mặc định `FileShare.Read`) ném `IOException` khi file đang có handle ghi.
- **Guard kích thước trước khi nạp file vào RAM**: chặn ngưỡng (vd 100MB) và báo lỗi rõ ràng thay
  vì `ReadAllBytes` vô điều kiện (tránh OOM). Download qua `DotNetStreamReference` (chunked, không
  base64) nên KHÔNG vướng `HubOptions.MaximumReceiveMessageSize` (giới hạn đó chỉ áp chiều
  client→server) — rủi ro thật là RAM server, không phải giới hạn message SignalR.
- **Page đọc I/O**: truyền `CancellationToken` (CTS riêng, `Cancel()` trong `DisposeAsync`), bắt
  `OperationCanceledException` bỏ qua khi circuit dispose (theo `blazor-web-app.md` §17.1). Service
  phải `throw` lại `OperationCanceledException` (KHÔNG phân loại thành "lỗi đọc").
- **Chặn path traversal**: chuẩn hóa relative path rồi `Path.GetFullPath` và kiểm tra kết quả nằm
  trong thư mục gốc (như `ResolveSafePath`).

## ❌ DON'T (Tuyệt đối cấm)
- Cấm `catch (Exception) { return []; }` / `return null;` cho thao tác đọc file/thư mục hiển thị
  lên UI — che giấu lỗi phân quyền, người dùng không biết đường sửa.
- Cấm dùng `Directory.Exists`/`File.Exists` để "kiểm tra rồi bỏ qua im lặng" khi mục đích là hiển
  thị dữ liệu cho người dùng.
- Cấm `File.ReadAllBytes(Async)` cho file có thể đang được ghi (mất `FileShare.ReadWrite`) hoặc
  cho file không giới hạn kích thước.

---

> Interface signature, `CappedCapturingStream`, `GetLevel` code, bảng chi tiết key `RequestLogging`/
> `LogRetention`, cách đổi retention trên server đang chạy, cơ chế dọn dẹp, checklist:
> **`.claude/skills/api/logging.md`** — KHÔNG lặp lại quy tắc chọn/anti-pattern ở đây.
