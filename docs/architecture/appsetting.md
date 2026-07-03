# Appsettings — Quy tắc mã hóa credentials (`enc:` + `POS_SECRET_KEY`)

> **Mục đích tài liệu này**: tra cứu nhanh quy tắc mã hóa password trong `appsettings.*.json`.
> Chi tiết cơ chế + code: `src/POS.Infrastructure/Security/SecretProtector.cs`.
> Quy trình rollout đầy đủ (từng bước cho ops): `docs/ROLLOUT.md` §C4.

## Cơ chế

- Token dạng `enc:` + base64(`nonce(12) || tag(16) || ciphertext`) — AES-256-GCM.
- Hook giải mã chạy ở **CẢ 2** `Program.cs` (`src/POS.Api/Program.cs`, `src/POS.Web/Program.cs`),
  NGAY SAU `CreateBuilder`, TRƯỚC `AddInfrastructure`: quét mọi giá trị config chứa `enc:`, giải mã
  bằng khóa từ env `POS_SECRET_KEY`, nạp đè in-memory. Mọi consumer (`GetConnectionString`,
  `GetSection<RabbitMQOptions>`...) tự nhận plaintext, không cần sửa từng factory.
- Khóa `POS_SECRET_KEY` (base64, 32 byte) **dùng chung** cho POS.Api + POS.Web.
- Trang tạo khóa / mã hóa: `/admin/encrypt-secret` (POS.Web, SystemAdmin) — sinh token cho cả 2 project.

## Dùng mã hóa hay không — tự suy ra từ NỘI DUNG file, không phải 1 cờ riêng

| Muốn | Làm gì | Cần `POS_SECRET_KEY`? |
|---|---|---|
| **Cách cũ** (plaintext) | Để `Password=<mật khẩu thật>` trong appsettings | ❌ Không — hook không tìm thấy `enc:` nào → hoàn toàn no-op |
| **Mã hóa** | Thay bằng `Password=enc:<token>` | ✅ Có — thiếu key mà có `enc:` → app **fail-fast** lúc khởi động (chủ đích) |

Không có cờ kiểu `Security:UseEncryptedSecrets` — sẽ tạo thêm 1 nguồn có thể lệch với nội dung file
thật (cờ nói "đang mã hóa" nhưng ai đó lỡ dán plaintext, hoặc ngược lại). Muốn đổi giữa 2 chế độ: sửa
thẳng giá trị `Password=...` trong file, không đụng code.

## Phạm vi áp dụng

- **Chỉ mã hóa file môi trường** (`appsettings.Production.json`, `appsettings.UAT.json` nếu cần) —
  **KHÔNG** mã hóa `appsettings.json` (base). Hook chạy ở mọi môi trường; base có `enc:` mà Dev
  không set khóa → Dev cũng fail-fast.
- Áp dụng cho **cả `src/POS.Api` và `src/POS.Web`**. `POS.Worker` hiện **chưa có hook** — vẫn
  plaintext, ngoài phạm vi cơ chế này.
- Trạng thái hiện tại (2026-07-02): `appsettings.Production.json` của cả POS.Api và POS.Web **đã
  mã hóa** (9 connection string + RabbitMQ password mỗi file).

## Anti-pattern

- ❌ Mã hóa `appsettings.json` (base) → mọi môi trường kể cả Dev đều cần khóa, phá Dev local.
- ❌ Thêm cờ cấu hình bật/tắt mã hóa riêng — dư thừa, nội dung file đã tự quyết định.
- ❌ Hardcode credential mới ở dạng plaintext trong file môi trường mà không mã hóa.

## Tham chiếu

| Việc cần làm | Xem |
|---|---|
| Rollout từng bước (sinh khóa, mã hóa, verify) | `docs/ROLLOUT.md` §C4 |
| Deploy Docker + truyền `POS_SECRET_KEY` qua container | `docs/guide-deploy.md` |
| Pattern code hook (áp dụng khi tạo service mới cần credential) | `.claude/skills/api/SKILLS.md` — "Pattern: Mã hóa credentials trong appsettings" |
