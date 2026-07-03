# POS.Web — Quy định bảo mật

> Chuẩn bảo mật cho POS.Web (Blazor Server dashboard quản trị NỘI BỘ: StoreOperator / ITOps / SystemAdmin, có SQL Console).
> Áp dụng cho mọi thay đổi cấu hình/hạ tầng. Các bước thao tác khi go-live: xem `docs/ROLLOUT.md`.
> Thiết lập 2026-06-28 (security hardening). Cập nhật file này khi thêm quy định mới.

---

## 0. Nguyên tắc chung

- Cấu hình bảo mật phải **config-driven theo môi trường**, KHÔNG hardcode trong code.
- **BẬT đầy đủ ở Production/UAT**, có thể **NỚI ở Development** — nhưng KHÔNG được làm yếu Production để fix lỗi Dev.
- KHÔNG tự ý gỡ/giảm biện pháp bảo mật để hết lỗi; nếu biện pháp bảo mật là nguyên nhân → nêu đánh đổi để chủ dự án quyết định.
- ⚠️ POS.Web có **SQL Console** chạy SQL trực tiếp lên DB production → khuyến nghị đặt sau VPN/IP allowlist, không expose trần internet.

---

## 1. Section `Security` trong appsettings (nguồn cấu hình duy nhất)

```jsonc
"Security": {
  "Mode": "Internet",            // BehindProxy | DirectHttps | Internet — chỉ chi phối ForwardedHeaders
  "RequireHttps": false,         // false = cho phép HTTP (cookie SameAsRequest, không HSTS/redirect); true = ép HTTPS
  "EnableHsts": true,            // chỉ tác dụng khi RequireHttps=true
  "EnableSecurityHeaders": true, // TẮT ở Development (tránh chặn VS Browser Link)
  "EnableSqlConsole": true,      // false để tắt SQL Console (khuyến nghị khi ra internet)
  "KnownProxies": [],            // chỉ dùng khi Mode=BehindProxy
  "KnownNetworks": []            // CIDR — chỉ dùng khi Mode=BehindProxy
}
```

| Môi trường | Mode | RequireHttps | EnableSecurityHeaders | Ghi chú |
|---|---|---|---|---|
| Development | (kế thừa) | false | **false** | HTTP localhost; headers off để Browser Link/Hot Reload chạy |
| Production/UAT | Internet (no-proxy) | false→**true** khi có TLS | true | Đặt thêm `AllowedHosts`, cân nhắc `EnableSqlConsole:false` |

> Cơ chế: `src/POS.Web/Program.cs` (các biến `securityMode/requireHttps/...` + middleware). Quyết định triển khai: `docs/ROLLOUT.md`.

---

## 2. HTTPS & Cookie (C1)

- `Cookie.HttpOnly = true` luôn.
- `Cookie.SecurePolicy = Always` **chỉ khi** `RequireHttps=true && !IsDevelopment` (ngược lại `SameAsRequest` để login HTTP không gãy).
- `Cookie.SameSite = Strict` khi `Mode=Internet`, ngược lại `Lax`.
- `UseHsts()` (khi `EnableHsts`) + `UseHttpsRedirection()` (khi `DirectHttps`/`Internet`) — chỉ chạy khi `RequireHttps=true`.
- **Quy định:** app HTTP-only (Kestrel `http://+:8080`) → phải có lớp TLS thật trước khi đặt `RequireHttps:true`, nếu không browser không gửi lại cookie Secure → mất đăng nhập.
- ❌ KHÔNG ép `Cookie.Secure=Always` vô điều kiện.

---

## 3. Forwarded headers (H2)

- `Mode=Internet`/`DirectHttps` (không proxy) ⇒ **KHÔNG** gọi `UseForwardedHeaders` → không tin `X-Forwarded-*` (chống giả mạo IP/scheme/host).
- `Mode=BehindProxy` ⇒ chỉ tin proxy khai báo trong `KnownProxies`/`KnownNetworks`. Để trống = tạm tin mọi proxy + **log cảnh báo** (phải khai IP proxy thật ở production).
- ❌ KHÔNG `KnownProxies.Clear()` + `KnownNetworks.Clear()` vô điều kiện (tin mọi nguồn).

---

## 4. Security headers / CSP (M1)

Middleware phát (khi `EnableSecurityHeaders=true`): `X-Content-Type-Options: nosniff`, `X-Frame-Options: DENY`, `Referrer-Policy: strict-origin-when-cross-origin`, và CSP:

```
default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'none';
img-src 'self' data:; font-src 'self' data: https://fonts.gstatic.com;
style-src 'self' 'unsafe-inline' https://fonts.googleapis.com;
script-src 'self'; connect-src 'self'; frame-src 'self' blob:; form-action 'self'
```

- `script-src 'self'` (chặt — blazor.web.js + MudBlazor từ self). KHÔNG thêm `'unsafe-inline'` cho script.
- `style-src 'unsafe-inline'` bắt buộc cho MudBlazor (inject `<style>`).
- `connect-src 'self'` cho WebSocket `/_blazor` (cùng origin).
- `frame-src 'self' blob:` cho preview PDF (iframe blob:).
- **Quy định:** KHÔNG dùng inline `<script>`/`on*=` trong markup (CSP chặn). Tài nguyên ngoài mới → thêm host vào đúng directive, không nới `script-src` bừa.
- ❌ KHÔNG bật headers ở Development (CSP `connect-src 'self'` chặn VS Browser Link / dotnet-watch).

---

## 5. Credentials (C4)

- ❌ KHÔNG hardcode credential mới (DB/RabbitMQ/API key) trong bất kỳ `appsettings*.json` nào.
- Password trong `appsettings.Production.json` mã hóa bằng token `enc:` (AES-256-GCM) — giải mã runtime bằng khóa env `POS_SECRET_KEY` (dùng chung cho POS.Api và POS.Web).
- Khóa **không bao giờ vào git** (đặt ở `.env`, đã `.gitignore`). Ciphertext `enc:` thì commit được.
- Chỉ mã hóa file môi trường (Production), KHÔNG mã hóa `appsettings.json` base (sẽ buộc Dev cần khóa).
- Tạo token tại trang `/admin/encrypt-secret` (SystemAdmin, POS.Web). Cơ chế: `src/POS.Infrastructure/Security/SecretProtector.cs` + hook trong `Program.cs` của **cả POS.Api và POS.Web**. Quy trình: `docs/ROLLOUT.md`.

---

## 6. SQL Console (H1)

- Chỉ `Policy = AdminOnly` (SystemAdmin).
- Whitelist `SELECT/INSERT/UPDATE/CREATE|ALTER PROCEDURE`; lệnh ghi chạy trong transaction (Commit/Rollback).
- **Mask** `password|pwd|token|secret|apikey` (literal `'...'`) trước khi ghi audit DB + Kibana log — KHÔNG lưu plaintext credential.
- Cờ `Security:EnableSqlConsole` gate **cả service lẫn page** (defense-in-depth). Đặt `false` ở Production expose internet nếu không thực sự cần.

---

## 7. Lộ thông tin lỗi (C2)

- `WebApp:EnableDetailedErrors = false` ở Production/UAT (DEV tự bật qua `IsDevelopment()`).
- ❌ KHÔNG bật DetailedErrors ngoài Development (lộ stack trace, tên server DB, đường dẫn nội bộ).

---

## 8. Bất biến KHÔNG được phá (authz/auth)

- **Authorization server-side**: mọi page có `[Authorize(Policy=...)]`; menu ẩn theo role chỉ là UX.
- **Row-level store filter**: StoreOperator chỉ thấy store của mình — lọc ở tầng SQL, không tin client. Không bỏ qua khi thêm trang/dialog.
- **Auth bridge token** (`Login.razor → /account/signin/{token} → SignInAsync`): không gọi `SignInAsync` trong InteractiveServer; đổi cookie SameSite/Secure phải kiểm lại flow này.
- 3 policy: `StoreAndAbove` / `OpsAndAbove` / `AdminOnly` (`src/POS.Web/Auth/WebRoles.cs`).

---

## 9. Còn tồn đọng / cần làm khi go-live

| Mã | Việc | Trạng thái |
|---|---|---|
| C4 | Mã hóa password thật (chạy rollout) | ⏳ cơ chế sẵn sàng, chưa rollout |
| C1 | `RequireHttps:true` khi có TLS | ⏳ đang HTTP test |
| H2 | `AllowedHosts` = domain thật (đang `"*"`) | ⏳ |
| H1 | Cân nhắc `EnableSqlConsole:false` ở Prod | ⏳ tùy quyết định |

> Chi tiết thao tác: `docs/ROLLOUT.md`. Pattern code: `.claude/skills/web/SKILLS.md` (Security headers/CSP, SQL Console), `.claude/skills/api/SKILLS.md` (mã hóa credentials).
