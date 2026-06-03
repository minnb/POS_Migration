# Lessons Learned — POS Backend Convert

> File này ghi lại các lỗi phát hiện trong quá trình convert và rule bổ sung tương ứng.
> **Claude Code phải đọc file này** mỗi khi bắt đầu convert một module mới.
> Cập nhật bằng command `/add-rule` mỗi khi phát hiện lỗi mới.

---

## Cách đọc file này

Trước khi convert bất kỳ module nào, đọc toàn bộ file này và
ghi nhớ tất cả các rule trong cột "Rule bổ sung".
Các lỗi ở đây đều đã từng xảy ra thực tế — không được lặp lại.

---

## Danh sách lỗi đã ghi nhận

---

## 2026-06-03 — Cấu hình API đối tác lấy từ appsettings thay vì DB

- **Module bị ảnh hưởng:** Payment (Urbox, GotIT, OneU), Gift (WinX), và tất cả module gọi API bên ngoài
- **Triệu chứng:** Partner HTTP services không thể kết nối vì URL / credentials bị hardcode trong appsettings.json với giá trị rỗng. Khi deploy thực tế, DBA/ops chỉ cấu hình trong bảng DB, không điền appsettings.
- **Nguyên nhân:** Khi implement `UrboxHttpService`, `GotITHttpService`, `OneUHttpService`, `WinXHttpService`, Claude đã dùng `IConfiguration` để đọc config từ appsettings thay vì đọc từ bảng `SysWebApi` + `SysWebApiRoute` trong CentralMD — cách mà hệ thống cũ vận hành qua `MemoryCacheService.GetSysWebApi()`.
- **Fix:** Tạo `ISysWebApiConfigService` / `SysWebApiConfigService` trong Infrastructure; query Dapper 2 bảng từ CentralMD, cache đến nửa đêm bằng IMemoryCache; refactor 4 HTTP services inject interface này thay IConfiguration; xóa các section WinX/Urbox/GotIT/OneU khỏi appsettings.json.
- **Rule bổ sung:** Đã thêm mục **4.7** vào `CLAUDE.md` và mục **9** vào `docs/conventions.md`
- **Kiểm tra tương tự:** Mọi module còn lại khi gọi API đối tác ngoài (Loyalty, Capillary, SAP, WinLife, WinPay, WinCare, PLG, VinID) đều **phải dùng `ISysWebApiConfigService`**, không được dùng `IConfiguration` cho partner config.

---

### Bảng mapping `SysWebApiDto` → logic cũ (tham khảo nhanh)

| Field DTO | Ý nghĩa thực tế | Ví dụ |
|---|---|---|
| `Host` | Base URL của API đối tác | `https://api.urbox.vn` |
| `UserName` | Tên header auth (Urbox: App-Id) hoặc client_id (OneU) hoặc tên header (WinX) | `App-Id` / `client_id` |
| `Password` | Giá trị header auth (Urbox: App-Secret) hoặc client_secret (OneU) | `secret_xxx` |
| `Authorization` | brand_id (Urbox), pin prefix (GotIT), extra auth value | `123` / `PREFIX` |
| `PrivateKey` | **Nội dung XML RSA private key** — KHÔNG phải file path | `<RSAKeyValue>...` |
| `PublicKey` | Public key ID (OneU: X-Key-ID) | `key_xxx` |
| `Description` | merchant_code (OneU), excluded stores (Urbox, ngăn cách `;`) | `WCM` / `0001;0002` |
| `Version` | Phiên bản API (V2/V3/V6) hoặc timeout giây (WINX) | `V2` / `30` |
| `Routes[name].Route` | Endpoint path | `/api/voucher/check` |
| `Routes[name].Notes` | Config phụ của route (OneU Token: audience) | `https://audience.url` |

---

## Template ghi lỗi mới

Khi chạy `/add-rule`, Claude sẽ thêm theo format:

```
## [YYYY-MM-DD] — [Tên lỗi ngắn gọn]

- **Module bị ảnh hưởng:** tên module
- **Triệu chứng:** POS client bị lỗi gì / biểu hiện sai thế nào
- **Nguyên nhân:** Claude đã làm gì sai so với code cũ
- **Fix:** Đã sửa thế nào
- **Rule bổ sung:** Rule mới đã thêm vào file nào, mục nào
- **Kiểm tra tương tự:** Các module khác có thể mắc lỗi tương tự không
```