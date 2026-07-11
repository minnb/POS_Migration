# /web-gen-hash — Tạo BCrypt hash cho user migration SQL

Dùng lệnh này để tạo BCrypt hash từ mật khẩu plain-text và cập nhật vào file migration SQL.

---

## Cách dùng

```
/web-gen-hash
```

Hoặc cung cấp mật khẩu luôn:
```
/web-gen-hash Admin@2024!
```

---

## Quy trình Claude thực hiện

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

---

## Thông tin kỹ thuật

- Package: `BCrypt.Net-Next 4.2.0` (đã có trong `src/POS.Web/POS.Web.csproj`)
- Work factor: `11` (cân bằng bảo mật và performance)
- Hash format: `$2a$11$...` (bcrypt, 60 ký tự)
- Mỗi lần hash cùng password → ra hash khác nhau (salt ngẫu nhiên)
- Verify: `BCrypt.Verify(password, hash)` — không cần biết salt
