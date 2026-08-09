---
name: web-ops
description: Trợ lý vận hành POS.Web: Kiểm tra trạng thái build/pages và generate BCrypt hash cho database.
allowed-tools: Read, Bash
argument-hint: [action: check-status | gen-hash] [args]
---

# WEB OPERATIONS

Skill gộp từ 2 lệnh cũ (`/web-check-status`, `/web-gen-hash`).

| Đối số 1 | Việc cần làm | Mục |
|---|---|---|
| `check-status` | Audit build + pages + DI + auth setup của POS.Web | §A |
| `gen-hash [password]` | Tạo BCrypt hash và cập nhật migration SQL | §B |

Không truyền đối số → hỏi User muốn chạy action nào, rồi đi đúng 1 mục.

---

## §A. Kiểm tra trạng thái POS.Web (`check-status`)

Audit nhanh trạng thái build và completeness của POS.Web.

```
/web-ops check-status
```

### Bước 1 — Build
Chạy: `dotnet build src/POS.Web/ --nologo -v quiet`
- Báo cáo: số errors, số warnings
- Nếu có error → liệt kê từng error và file tương ứng

### Bước 2 — Đọc pages hiện có
Đọc tất cả file trong `src/POS.Web/Components/Pages/**/*.razor`:
- Liệt kê theo section: Store / Ops / Admin
- Với mỗi page: route, policy, status (có implement hay còn TODO)

### Bước 3 — Kiểm tra DI completeness
Đọc `src/POS.Web/Program.cs`:
- Xác nhận `AddInfrastructure()` và `AddApplication()` đã được gọi
- Kiểm tra có `IMemoryCache` (`AddMemoryCache()`) — cần cho login bridge
- Kiểm tra bridge endpoint `/account/signin/{token}` tồn tại

### Bước 4 — Kiểm tra Auth setup
Đọc `src/POS.Web/Auth/migration_dashboard_users.sql`:
- Xác nhận có user admin mặc định
- HASH_PLACEHOLDER đã được thay bằng BCrypt hash thật chưa

### Bước 5 — Báo cáo tổng hợp

```
## POS.Web Status — {ngày hiện tại}

### Build
✅ Build thành công (0 errors, 0 warnings)
❌ Build failed: {N} errors

### Pages
Store section:
  ✅ /store/revenue — RevenuePage (implemented)
  ⚠️  /store/... — {tên} (stub/TODO)

Ops section:
  ✅ /ops/health — HealthPage

Admin section:
  ✅ /admin/users — UsersPage

### DI & Config
✅ AddInfrastructure() registered
✅ AddMemoryCache() registered
✅ Login bridge endpoint /account/signin/{token}
⚠️  {issue nếu có}

### Auth
✅ admin user: BCrypt hash present in migration SQL
❌ HASH_PLACEHOLDER chưa được thay — chạy /web-ops gen-hash

### Gợi ý bước tiếp theo
- {việc cần làm tiếp theo dựa trên kết quả audit}
```

---

## §B. Tạo BCrypt hash cho user migration SQL (`gen-hash`)

Tạo BCrypt hash từ mật khẩu plain-text và cập nhật vào file migration SQL.

```
/web-ops gen-hash
/web-ops gen-hash Admin@2024!
```

### Bước 1 — Hỏi mật khẩu (nếu chưa có)
Hỏi: "Mật khẩu muốn hash là gì?" (ví dụ: `Admin@0987`, `Store@2024!`)

### Bước 2 — Tạo BCrypt hash

**Cách A — Dùng dotnet-script (nếu đã cài):**
```powershell
# Tạo script tạm
$script = @"
#r "nuget: BCrypt.Net-Next, 4.2.0"
using BCrypt.Net;
Console.WriteLine(BCrypt.HashPassword(Args[0], workFactor: 11));
"@
$script | Out-File scripts/gen-hash.csx
dotnet script scripts/gen-hash.csx -- "MẬT_KHẨU"
Remove-Item scripts/gen-hash.csx
```

**Cách B — Dùng PowerShell + NuGet (fallback):**
```powershell
# Add NuGet package và chạy inline
$code = @"
using BCrypt.Net;
Console.WriteLine(BCrypt.HashPassword("MẬT_KHẨU", workFactor: 11));
"@
# Build và chạy quick C# project
```

**Cách C — Đọc hash đã biết từ session (nhanh nhất):**
Nếu hash đã được tạo trong conversation này (ví dụ: session có `$2a$11$...`) → dùng luôn.

### Bước 3 — Cập nhật file migration SQL
Đọc `src/POS.Web/Auth/migration_dashboard_users.sql`:
- Tìm `HASH_PLACEHOLDER` hoặc hash cũ
- Thay bằng hash mới
- Lưu file

> **Lưu ý quyền công cụ**: skill này khai báo `allowed-tools: Read, Bash` — KHÔNG có Edit/Write.
> Nếu bước này bị chặn, in ra hash + dòng SQL cần thay và để User tự sửa, hoặc xin phép User bổ
> sung `Edit` vào `allowed-tools`. KHÔNG dùng Bash để ghi đè file nguồn.

### Bước 4 — Xác nhận
```
✅ BCrypt hash (workFactor=11) tạo thành công
✅ Cập nhật vào: src/POS.Web/Auth/migration_dashboard_users.sql
   Username: admin
   Hash: $2a$11$...

Chạy SQL migration để áp dụng:
  USE RPOSMasterData;
  GO
  -- chạy nội dung file migration_dashboard_users.sql
```

### Thông tin kỹ thuật

- Package: `BCrypt.Net-Next 4.2.0` (đã có trong `src/POS.Web/POS.Web.csproj`)
- Work factor: `11` (cân bằng bảo mật và performance)
- Hash format: `$2a$11$...` (bcrypt, 60 ký tự)
- Mỗi lần hash cùng password → ra hash khác nhau (salt ngẫu nhiên)
- Verify: `BCrypt.Verify(password, hash)` — không cần biết salt
