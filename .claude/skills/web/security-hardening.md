---
name: web-security-hardening
description: Bảo mật tầng Admin trong POS.Web — security headers/CSP, SQL Console hardening, PIN/step-up gate, đọc/tải file server an toàn (path traversal). Đọc khi làm page/tính năng Admin nhạy cảm.
---

# Security Hardening Patterns — POS.Web (Admin)

> **Áp dụng khi:** cấu hình bảo mật tầng hạ tầng, hoặc tạo page Admin có thao tác nhạy cảm (chạy
> SQL trực tiếp, đọc file server, cần lớp xác thực thứ 2).

---

## Security headers / CSP + HTTPS config-driven (Program.cs)
> Áp dụng khi: cấu hình bảo mật tầng hạ tầng cho POS.Web. Section `Security` trong appsettings điều khiển,
> KHÔNG hardcode theo môi trường.

```jsonc
// appsettings: section "Security"
"Mode": "Internet",        // BehindProxy | DirectHttps | Internet — chỉ chi phối ForwardedHeaders
"RequireHttps": false,     // TÁCH RIÊNG: false = cho phép HTTP (cookie SameAsRequest, không HSTS/redirect)
"EnableHsts": true,        // chỉ tác dụng khi RequireHttps=true
"EnableSecurityHeaders": true,  // TẮT ở Development (xem anti-pattern bên dưới)
"KnownProxies": [], "KnownNetworks": []  // chỉ dùng khi Mode=BehindProxy
```

- **Tách `RequireHttps` khỏi `Mode`**: cho phép chạy/test Production qua HTTP mà không vỡ login (cookie `Secure=Always` chỉ bật khi `RequireHttps && !IsDevelopment`). Khi có TLS → đổi `RequireHttps:true`, không sửa code.
- **`Mode=Internet`/`DirectHttps`** ⇒ KHÔNG gọi `UseForwardedHeaders` → bịt giả mạo `X-Forwarded-*` khi app expose thẳng (không proxy). `BehindProxy` mới nạp `KnownProxies`/`KnownNetworks` (để trống = tạm tin mọi proxy + log cảnh báo).
- **CSP cho Blazor Server**: `script-src 'self'` (blazor.web.js + MudBlazor), `style-src 'unsafe-inline'` (MudBlazor inject `<style>`), `connect-src 'self'` (WebSocket `/_blazor` cùng origin), **`frame-src 'self' blob:`** (preview PDF qua iframe blob).

> **Anti-pattern:**
> - ❌ Để CSP bật ở **Development** → `connect-src 'self'` chặn **VS Browser Link / dotnet-watch** (cổng localhost khác) làm tắc auto-reload. Đặt `EnableSecurityHeaders:false` trong `appsettings.Development.json`.
> - ❌ Quên `frame-src ... blob:` → vỡ preview PDF (`<iframe src="blob:...">` ở SalesByCategoryPage).
> - ❌ Ép `Cookie.Secure=Always` không điều kiện → login HTTP (dev/test) gãy vì browser không gửi lại cookie Secure.
>
> Ví dụ thực tế: `src/POS.Web/Program.cs` (vars `securityMode/requireHttps/...` + middleware headers); rollout: `docs/ROLLOUT.md`

---

## SQL Console hardening
> Áp dụng khi: trang chạy SQL trực tiếp (AdminOnly). Phải mask secret khi log + cho phép tắt.

- **Blacklist, không phải whitelist**: `SqlConsoleService.Validate()` dùng ScriptDom (`TSql160Parser`)
  parse AST rồi cho phép hầu hết statement type, chỉ chặn tuyệt đối statement có tên class bắt đầu
  bằng `Drop`/`Truncate` (`stmt.GetType().Name.StartsWith("Drop"/"Truncate")`) — bắt được ~70 biến
  thể DROP mà không cần liệt kê từng loại. `CreateTableStatement`/`AlterTableStatement` (base class,
  bắt mọi biến thể ALTER TABLE) map sang `StatementKind.TableDdl`; mọi statement khác không rơi vào
  case cụ thể → `StatementKind.Other` (vẫn cho chạy, không có chip UI riêng).
- Mask `password|pwd|token|secret|apikey` (literal `'...'`) **trước khi** ghi audit DB + Kibana log — tránh lưu plaintext.
- Cờ `Security:EnableSqlConsole` (mặc định true) gate **cả service lẫn page** (defense-in-depth): service trả lỗi/throw, page hiện alert + disable. Nên đặt `false` ở Production expose internet.
- **PIN gate thứ 2** (độc lập với cookie login) — xem mục "Pattern: PIN/step-up gate" bên dưới.

> Ví dụ thực tế: `src/POS.Web/Services/SqlConsoleService.cs` (`MaskSecrets`, `IsEnabled`, `Validate`), `Components/Pages/Admin/SqlConsolePage.razor`, `src/POS.Web/Auth/WebUserService.cs` (`VerifyPinAsync`, `SetPinAsync`)

---

## Pattern: PIN/step-up gate cho trang nhạy cảm

> Áp dụng khi: 1 trang Admin cần thêm lớp xác thực thứ 2 độc lập với cookie login (bảo vệ trường
> hợp cookie/session bị đánh cắp) — vd trang thực thi SQL trực tiếp, thao tác DB nguy hiểm.

- PIN lưu **BCrypt hash trong DB** (cột `PinHash` trên bảng user đã có, KHÔNG tạo bảng mới nếu
  không cần — KHÔNG lưu trong `appsettings` vì cơ chế `enc:` chỉ dành cho secret cần giải mã lại,
  hash 1 chiều không cần và không nên đi qua đó).
- Toàn bộ nội dung trang bọc trong `@if (_pinVerified) { ... } else { <PIN card> }` — không render
  BẤT KỲ phần nào của trang thật (kể cả banner cảnh báo) trước khi verify xong.
- Khoá tạm sau N lần sai (5 lần/15 phút) bằng Redis counter key riêng — **không có method increment
  nguyên tử trong `IRedisService`**, dùng read-modify-write (`StringGetAsync<int>` rồi
  `StringSetAsync` lại) — chấp nhận được vì tần suất gõ PIN thấp, không cần atomic thật.
- **Đổi PIN bắt buộc nhập đúng PIN CŨ trước** (trừ lần đầu thiết lập) — nếu bỏ qua bước này, 1
  cookie/session bị đánh cắp đủ để tự đặt lại PIN theo ý kẻ tấn công rồi vượt qua chính lớp bảo vệ
  vừa thêm, phá vỡ hoàn toàn mục đích của PIN gate.
- **BẮT BUỘC `try/catch/finally` đầy đủ** quanh lời gọi verify — `try/finally` không đủ, exception
  không lường trước (hash sai định dạng...) sẽ crash circuit (xem `01-architecture-and-logic.md`
  mục lifecycle cho lý do circuit crash).

> Ví dụ thực tế: `Components/Pages/Admin/SqlConsolePage.razor` (PIN card + `VerifyPinAsync`), `src/POS.Web/Auth/WebUserService.cs` (`VerifyPinAsync`, `SetPinAsync`), `Components/Pages/Admin/Dialogs/ChangeMyPinDialog.razor`

---

## Pattern: Đọc/tải file trên server an toàn (whitelist extension + chống Path Traversal)
> Áp dụng khi: page cần liệt kê/tải file từ 1 thư mục gốc trên server (log viewer, file browser nội
> bộ...) — KHÔNG expose toàn bộ filesystem, chỉ 1 subtree cụ thể + đúng loại file cho phép.

- Tính `_rootDir` **1 lần trong constructor** bằng `Path.GetFullPath(...)` (không tính lại mỗi
  request) — nguồn gốc thường là 1 config path sẵn có (vd `Logging:FileLogDirectory`) rồi lấy
  `Directory.GetParent(...)` để mở rộng phạm vi nếu cần liệt kê nhiều thư mục con cùng cấp.
- **Whitelist extension** (không phải blacklist) — check ở CẢ lúc liệt kê (lọc
  `Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)`) LẪN lúc tải (từ chối nếu
  extension không khớp dù path hợp lệ).
- **Chống Path Traversal** — bắt buộc đủ 3 bước theo đúng thứ tự (thiếu bước nào là có lỗ hổng):
  ```csharp
  var fullPath = Path.GetFullPath(Path.Combine(_rootDir, relativePath));   // resolve .. trước
  var rootWithSep = _rootDir.EndsWith(Path.DirectorySeparatorChar) ? _rootDir : _rootDir + Path.DirectorySeparatorChar;
  if (!fullPath.StartsWith(rootWithSep, StringComparison.OrdinalIgnoreCase)) return null; // so khớp PREFIX có separator, không phải fullPath.Contains(_rootDir)
  if (!AllowedExtensions.Contains(Path.GetExtension(fullPath), StringComparer.OrdinalIgnoreCase)) return null;
  if (!File.Exists(fullPath)) return null;
  ```
  So khớp prefix phải có `DirectorySeparatorChar` ở cuối `_rootDir` — nếu không, `/srv/pos/logs2`
  vẫn "StartsWith" `/srv/pos/logs` dù nằm ngoài root (off-by-one thư mục anh em).
- Toàn bộ logic đọc file bọc `try/catch`, lỗi ghi qua `IFileLogHelper.WriteExpLogs(...)` — KHÔNG
  throw ra UI, trả `null`/danh sách rỗng để page tự xử lý thông báo.
- Không dùng controller/endpoint HTTP riêng cho POS.Web (khác POS.Api) — page inject service qua DI
  đọc `byte[]` rồi gọi `JS.SaveAsFileAsync(fileName, bytes, contentType)` (JS interop có sẵn ở
  `src/POS.Web/Services/JsDownloadExtensions.cs`), giống mọi download khác trong POS.Web (Excel
  template, PDF...).

> Anti-pattern:
> - ❌ `fullPath.Contains(_rootDir)` thay vì `StartsWith` + separator — chuỗi con khớp bất kỳ đâu
>   trong path, không phải chỉ ở đầu, dễ bypass bằng path dựng khéo.
> - ❌ Chỉ check extension lúc liệt kê, bỏ qua lúc tải — endpoint tải file vẫn nhận input tuỳ ý từ
>   client, phải tự validate lại độc lập, không tin danh sách đã lọc trước đó.
> - ❌ Trả `FileNotFoundException`/stack trace ra UI khi lỗi filesystem (quyền, đường dẫn không tồn
>   tại) — bọc try/catch, log nội bộ, trả kết quả rỗng/null.
>
> Ví dụ thực tế: `src/POS.Web/Services/LogFileService.cs` (`GetLogFilesAsync`,
> `DownloadLogFileAsync`), page dùng: `Components/Pages/Admin/LogFilePage.razor`.
