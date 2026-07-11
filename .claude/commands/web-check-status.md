# /web-check-status — Kiểm tra trạng thái POS.Web

Dùng lệnh này để audit nhanh trạng thái build và completeness của POS.Web.

---

## Cách dùng

```
/web-check-status
```

---

## Quy trình Claude thực hiện

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
❌ HASH_PLACEHOLDER chưa được thay — chạy /web-gen-hash

### Gợi ý bước tiếp theo
- {việc cần làm tiếp theo dựa trên kết quả audit}
```
