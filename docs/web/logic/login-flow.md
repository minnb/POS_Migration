# POS.Web — Luồng Xác Thực Đăng Nhập

## 1. User lưu ở đâu?

**Database:** `RPOSMasterData` (connection string `CentralMD` trong `appsettings.json`)

**Bảng:** `DashboardUsers`

```sql
CREATE TABLE DashboardUsers (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(256) NOT NULL,   -- BCrypt hash
    FullName     NVARCHAR(200) NOT NULL,
    Role         NVARCHAR(50)  NOT NULL,   -- 'StoreOperator' | 'ITOps' | 'SystemAdmin'
    StoreCodes   NVARCHAR(MAX) NULL,       -- JSON array: ["S001","S002"] hoặc NULL = all stores
    IsActive     BIT           NOT NULL DEFAULT 1,
    CreatedAt    DATETIME2     NOT NULL DEFAULT GETDATE(),
    UpdatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
);
```

**File model:** [src/POS.Web/Auth/DashboardUser.cs](../../../src/POS.Web/Auth/DashboardUser.cs)  
**Migration SQL:** [src/POS.Web/Auth/migration_dashboard_users.sql](../../../src/POS.Web/Auth/migration_dashboard_users.sql)

---

## 2. Kiến trúc Auth (các file liên quan)

```
src/POS.Web/
├── Auth/
│   ├── DashboardUser.cs          -- Model entity
│   ├── IWebUserService.cs        -- Interface: ValidateLoginAsync, GetByUsernameAsync
│   ├── WebUserService.cs         -- Impl: query DB + BCrypt.Verify
│   ├── WebRoles.cs               -- Hằng số role + policy
│   └── migration_dashboard_users.sql
├── Components/
│   ├── Routes.razor              -- AuthorizeRouteView + NotAuthorized handler
│   ├── RedirectToLogin.razor     -- Redirect /login khi chưa authen
│   ├── RedirectToAccessDenied.razor
│   ├── Pages/
│   │   ├── Login.razor           -- Form UI + gọi SignInAsync
│   │   ├── Index.razor           -- Redirect theo role sau login
│   │   └── AccessDenied.razor
│   └── Layout/
│       ├── MainLayout.razor      -- Layout có sidebar (authenticated)
│       └── EmptyLayout.razor     -- Layout trắng (login page)
└── Program.cs                    -- Cấu hình cookie auth + policies
```

---

## 3. Luồng đăng nhập (step-by-step)

```
Trình duyệt                    Blazor Server                    Database
     │                              │                               │
     │── GET /store/revenue ───────>│                               │
     │                         AuthorizeRouteView                   │
     │                         → chưa authenticate                  │
     │<── Redirect /login ─────────│                               │
     │                              │                               │
     │── GET /login ───────────────>│                               │
     │<── Login.razor (EmptyLayout)─│                               │
     │                              │                               │
     │── [submit form] ────────────>│                               │
     │   username + password        │                               │
     │                         Login.razor.DoLogin()                │
     │                              │── ValidateLoginAsync() ──────>│
     │                              │   SELECT * FROM DashboardUsers│
     │                              │   WHERE Username=? AND        │
     │                              │         IsActive=1            │
     │                              │<── DashboardUser row ─────────│
     │                              │                               │
     │                              │   BCrypt.Verify(password,     │
     │                              │       user.PasswordHash)      │
     │                              │                               │
     │                         [Thất bại] → hiện lỗi, ở lại /login │
     │                              │                               │
     │                         [Thành công]                         │
     │                              │  Tạo Claims:                  │
     │                              │  • ClaimTypes.Name = username │
     │                              │  • ClaimTypes.Role = role     │
     │                              │  • "full_name" = fullName     │
     │                              │  • "store_codes" = storeCodes │
     │                              │                               │
     │                              │  SignInAsync() → Set-Cookie   │
     │<── Redirect / ──────────────│                               │
     │── GET / ────────────────────>│                               │
     │                         Index.razor → đọc role claim         │
     │<── Redirect /store/revenue ─│  (StoreOperator)              │
     │                              │  (ITOps → /ops/health)        │
     │                              │  (SystemAdmin → /admin/users) │
```

---

## 4. Cookie & Session

**Cấu hình** ([Program.cs](../../../src/POS.Web/Program.cs)):

| Thuộc tính | Giá trị | Ý nghĩa |
|---|---|---|
| `LoginPath` | `/login` | Redirect khi chưa authen |
| `LogoutPath` | `/logout` | Xử lý đăng xuất |
| `AccessDeniedPath` | `/access-denied` | Redirect khi thiếu role |
| `ExpireTimeSpan` | 8 giờ (configurable) | Thời gian hết hạn session |
| `SlidingExpiration` | `true` | Gia hạn mỗi khi có request |
| `HttpOnly` | `true` | JavaScript không đọc được cookie |
| `SameSite` | `Strict` | Chống CSRF |
| `IsPersistent` | `true` | Cookie tồn tại qua lần đóng tab |

**Cấu hình timeout** trong `appsettings.json`:
```json
"WebApp": {
  "SessionTimeoutHours": 8
}
```

**Đăng xuất** — GET `/logout` (không cần form, dùng Href trực tiếp):
```
MainLayout logout button → Href="/logout"
  → app.MapGet("/logout") → SignOutAsync() → Redirect /login
```

---

## 5. Phân quyền Role-Based

### 3 Roles ([WebRoles.cs](../../../src/POS.Web/Auth/WebRoles.cs))

| Role | Mô tả |
|---|---|
| `StoreOperator` | Nhân viên cửa hàng |
| `ITOps` | Vận hành hệ thống |
| `SystemAdmin` | Quản trị viên |

### 3 Policies (cộng dồn)

| Policy | Roles được phép | Áp dụng cho |
|---|---|---|
| `StoreAndAbove` | StoreOperator, ITOps, SystemAdmin | `/store/*` |
| `OpsAndAbove` | ITOps, SystemAdmin | `/ops/*` |
| `AdminOnly` | SystemAdmin | `/admin/*` |

### Trang theo role

| URL | Policy | Component |
|---|---|---|
| `/store/revenue` | `StoreAndAbove` | [RevenuePage.razor](../../../src/POS.Web/Components/Pages/Store/RevenuePage.razor) |
| `/ops/health` | `OpsAndAbove` | [HealthPage.razor](../../../src/POS.Web/Components/Pages/Ops/HealthPage.razor) |
| `/admin/users` | `AdminOnly` | [UsersPage.razor](../../../src/POS.Web/Components/Pages/Admin/UsersPage.razor) |
| `/login` | `[AllowAnonymous]` | [Login.razor](../../../src/POS.Web/Components/Pages/Login.razor) |
| `/access-denied` | `[AllowAnonymous]` | [AccessDenied.razor](../../../src/POS.Web/Components/Pages/AccessDenied.razor) |

### StoreCodes (hạn chế theo cửa hàng)

- `StoreCodes = NULL` → user truy cập **tất cả** cửa hàng
- `StoreCodes = '["S001","S002"]'` → chỉ truy cập store S001, S002
- Hiện tại field được lưu vào claim `store_codes` nhưng **chưa enforce** trong query filter — dành cho tính năng tương lai.

### Sidebar hiển thị theo role

`MainLayout.razor` dùng `<AuthorizeView Policy="...">` để ẩn/hiện menu:
- `StoreAndAbove` → hiện nhóm menu Store
- `OpsAndAbove` → hiện nhóm menu Ops
- `AdminOnly` → hiện nhóm menu Admin

---

## 6. Xử lý trường hợp đặc biệt

### Chưa đăng nhập → truy cập protected route
```
Routes.razor → <NotAuthorized>
  → User.Identity.IsAuthenticated == false
  → <RedirectToLogin /> → NavigateTo("/login")
```

### Đã đăng nhập nhưng thiếu role → truy cập route cần quyền cao hơn
```
Routes.razor → <NotAuthorized>
  → User.Identity.IsAuthenticated == true
  → <RedirectToAccessDenied /> → NavigateTo("/access-denied")
```

### Kết nối Blazor bị mất (server reconnect)
```
App.razor → <ReconnectModal /> — hiện overlay "Đang kết nối lại..."
```

---

## 7. Checklist bảo mật

- [x] Password hash bằng BCrypt (4.2.0) — không lưu plaintext
- [x] Cookie `HttpOnly` — JavaScript không đọc được
- [x] Cookie `SameSite=Strict` — chống CSRF
- [x] Sliding expiration — session tự gia hạn khi hoạt động
- [x] `IsActive` flag — vô hiệu hóa user mà không cần xóa
- [x] Claims-based identity — role & store được nhúng vào cookie, không query DB mỗi request
- [ ] StoreCodes chưa enforce trong data filter (future work)
- [ ] Chưa có rate limiting cho `/login` (brute force protection)
- [ ] Chưa có yêu cầu độ phức tạp password khi tạo user mới
