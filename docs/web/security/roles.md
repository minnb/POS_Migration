# POS.Web — Vai trò & Phân quyền (Roles & Access Control)

> Nguồn sự thật về **role/policy/menu/route** cho POS.Web (Blazor Server dashboard nội bộ).
> Khác `docs/web/security/security.md` (hardening hạ tầng: HTTPS/CSP/credentials/SQL Console) —
> file này tập trung vào **mô hình phân quyền theo vai trò người dùng dashboard**.
> Cập nhật file này khi thêm role mới, đổi policy của page, hoặc đổi cơ chế gán quyền.

---

## 1. Tổng quan mô hình

POS.Web dùng **Cookie Authentication + Role-based Authorization** hoàn toàn tách biệt với Basic
Auth của POS.Api (không ảnh hưởng nhau). 4 role cố định (không có UI tạo role mới — role là
hằng số trong code, xem mục 6):

| Role | Hằng số (`WebRoles`) | Ý nghĩa |
|---|---|---|
| Vận hành cửa hàng | `StoreOperator` | Nhân viên cửa hàng — xem dữ liệu vận hành/báo cáo store của mình |
| BackOffice | `BackOffice` | Vận hành nghiệp vụ — quản lý Danh mục sản phẩm + Khuyến mãi (xem tất cả store, KHÔNG vào khu vực Vận hành hạ tầng) |
| IT Ops | `ITOps` | Đội vận hành hệ thống — xem toàn bộ store + màn hình cấu hình/giám sát |
| System Admin | `SystemAdmin` | Quản trị hệ thống — toàn quyền, bao gồm quản lý user, SQL Console |

4 role map vào **4 policy phân cấp** (role cao hơn tự động có quyền của role thấp hơn):

| Policy (`WebPolicies`) | Role được phép | Dùng cho |
|---|---|---|
| `StoreAndAbove` | `StoreOperator`, `BackOffice`, `ITOps`, `SystemAdmin` | Trang cấp cửa hàng (báo cáo, giao dịch, vận hành ca) |
| `BackOfficeAndAbove` | `BackOffice`, `ITOps`, `SystemAdmin` | Trang Danh mục sản phẩm + Khuyến mãi (`/catalog/*`, `/promotion/*`) |
| `OpsAndAbove` | `ITOps`, `SystemAdmin` | Trang giám sát/vận hành hạ tầng (`/ops/*`) |
| `AdminOnly` | `SystemAdmin` | Trang quản trị (Users, SQL Console, mã hóa secret...) |

Code: [`src/POS.Web/Auth/WebRoles.cs`](../../../src/POS.Web/Auth/WebRoles.cs) (hằng số) +
[`src/POS.Web/Program.cs`](../../../src/POS.Web/Program.cs) (đăng ký policy, `RequireRole`).

```csharp
// Program.cs
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(WebPolicies.StoreAndAbove,
        p => p.RequireRole(WebRoles.StoreOperator, WebRoles.BackOffice, WebRoles.ITOps, WebRoles.SystemAdmin));
    options.AddPolicy(WebPolicies.BackOfficeAndAbove,
        p => p.RequireRole(WebRoles.BackOffice, WebRoles.ITOps, WebRoles.SystemAdmin));
    options.AddPolicy(WebPolicies.OpsAndAbove,
        p => p.RequireRole(WebRoles.ITOps, WebRoles.SystemAdmin));
    options.AddPolicy(WebPolicies.AdminOnly,
        p => p.RequireRole(WebRoles.SystemAdmin));
});
```

> **Không có** khái niệm permission/claim rời rạc (vd "CanEditPrice", "CanVoidTransaction") —
> phân quyền chỉ theo **4 role cố định → 4 policy cố định → route-level `[Authorize]`**. Muốn
> phân quyền mịn hơn (per-action) hiện chưa có cơ chế, cần thiết kế mới nếu được yêu cầu.
> `BackOffice` không có `StoreCodes` scoping riêng (giống `ITOps`/`SystemAdmin`) — xem tất cả
> store, không lọc row-level theo `store_codes` claim.

---

## 2. Nguồn dữ liệu user & vai trò

- Bảng: `RPOSMasterData.dbo.DashboardUsers` — script khởi tạo:
  [`src/POS.Web/Database/Migrations/001_DashboardUsers.sql`](../../../src/POS.Web/Database/Migrations/001_DashboardUsers.sql).

| Cột | Kiểu | Ý nghĩa |
|---|---|---|
| `Id` | `INT IDENTITY` | PK |
| `Username` | `NVARCHAR(100)` | Unique |
| `PasswordHash` | `NVARCHAR(256)` | BCrypt hash (work factor 11) |
| `FullName` | `NVARCHAR(200)` | Tên hiển thị |
| `Role` | `NVARCHAR(50)` | `'StoreOperator'` \| `'BackOffice'` \| `'ITOps'` \| `'SystemAdmin'` — **string tự do, không FK/CHECK constraint** |
| `StoreCodes` | `NVARCHAR(MAX)` NULL | `NULL` = xem tất cả store; JSON array `'["S001","S002"]'` = chỉ store đó |
| `IsActive` | `BIT` | `0` = tài khoản bị khóa (soft-delete, không xóa cứng) |
| `CreatedAt`/`UpdatedAt` | `DATETIME2` | Audit thời gian |

- Seed mặc định: user `admin` / role `SystemAdmin` / `StoreCodes = NULL` (xem tất cả).
- **Role là chuỗi tự do trong DB** — validate hợp lệ chỉ ở tầng UI (`UserFormDialog.razor` dùng
  `MudSelect` với 4 `MudSelectItem` cố định theo `WebRoles.*`, không cho nhập tự do). Không có
  CHECK constraint ở DB — nếu chỉnh trực tiếp SQL sai chính tả role, `RequireRole` sẽ không khớp
  → user coi như không có quyền nào (không lỗi, chỉ bị chặn mọi trang có `[Authorize]`).

Service truy cập: [`IWebUserService`](../../../src/POS.Web/Auth/IWebUserService.cs) /
[`WebUserService`](../../../src/POS.Web/Auth/WebUserService.cs) — `ValidateLoginAsync`,
`GetAllAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync` (soft-delete → `IsActive=0`),
`ActivateAsync`, `UsernameExistsAsync`, `GetStoreCodes`.

---

## 3. Luồng gán quyền lúc đăng nhập (claims)

```
Login.razor: DoLogin()
  → UserService.ValidateLoginAsync(username, password)   // BCrypt.Verify
  → tạo claims: ClaimTypes.Name = Username
                ClaimTypes.Role = user.Role        ← claim quyết định RequireRole
                "full_name"     = user.FullName
                "store_codes"   = user.StoreCodes  (chỉ add nếu khác rỗng)
  → ClaimsPrincipal → lưu tạm IMemoryCache (token 1 lần, TTL 30s)
  → redirect /account/signin/{token} (forceLoad)
      → minimal API endpoint (HTTP pipeline thật, ngoài Blazor circuit)
      → ctx.SignInAsync(CookieAuth, principal, IsPersistent=true)
      → set cookie, redirect "/"
```

- **Vì sao qua token bridge**: Blazor `InteractiveServer` chạy trên WebSocket circuit —
  `HttpContext` degraded, gọi `SignInAsync` trực tiếp trong component sẽ throw và crash circuit.
  Phải thoát ra HTTP pipeline thật (`MapGet("/account/signin/{token}")`) để set cookie hợp lệ.
- Cookie: `HttpOnly=true`, `ExpireTimeSpan` = `WebApp:SessionTimeoutHours` (mặc định 8h),
  `SlidingExpiration=true`. `SameSite`/`SecurePolicy` phụ thuộc `Security:Mode`/`RequireHttps` —
  xem `docs/web/security/security.md` mục 2.
- **Route mặc định sau login** (`Index.razor` — `@page "/"`): đọc claim `ClaimTypes.Role` →
  `StoreOperator` → `/store/revenue`; mọi role khác (`ITOps`/`SystemAdmin`) → `/ops/health`.
- **Đăng xuất**: `GET /logout` (minimal API, `Program.cs`) → `ctx.SignOutAsync(CookieAuth)`.

---

## 4. Row-level filter theo store (`store_codes` claim)

- Claim `store_codes` (JSON array string, vd `'["S001","S002"]'`) chỉ được set khi
  `DashboardUsers.StoreCodes` khác rỗng — tức **chỉ `StoreOperator` mới bị giới hạn store**,
  `ITOps`/`SystemAdmin` thường để `StoreCodes = NULL` → claim vắng mặt → xem tất cả.
- **Mọi page hiển thị dữ liệu theo store PHẢI đọc claim này và lọc ở tầng SQL** (không lọc phía
  client) — pattern chuẩn dùng trong `CLAUDE.md` §POS.Web mục 5 (template page):

  ```csharp
  var state = await AuthState;
  var json  = state.User.FindFirst("store_codes")?.Value;
  _userStoreCodes = string.IsNullOrEmpty(json)
      ? []                                                                  // rỗng = xem tất cả
      : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json) ?? [];
  ```

- **Bất biến KHÔNG được phá** (xem `security.md` mục 8): row-level store filter phải áp dụng ở
  mọi trang mới có dữ liệu theo store — không tin client, không bỏ qua khi thêm trang/dialog.

---

## 5. Bảng route → policy đầy đủ (59 page, tính đến 2026-07-05)

> Nguồn: `@attribute [Authorize(Policy = WebPolicies.*)]` trên từng `.razor`. Khi thêm page mới,
> cập nhật bảng này (hoặc tối thiểu đúng menu tương ứng trong `MainLayout.razor`).

### 5.1 `StoreAndAbove` — mọi role (Cửa hàng)

| Route | Page |
|---|---|
| `/store/business-day` | `BusinessDayPage` — Xác nhận kết thúc ngày |
| `/store/eos-shifts` | `EosShiftsPage` — Kết thúc ca |
| `/store/shift-summary` | `ShiftSummaryPage` — Tổng kết ca |
| `/store/transactions` | `TransactionsPage` — Danh sách giao dịch |
| `/store/refunds` | `RefundsPage` — Hoàn trả |
| `/store/voids` | `VoidsPage` — Lịch sử hủy giao dịch |
| `/store/revenue` | `RevenuePage` — Tổng hợp doanh thu |
| `/store/revenue-by-staff` | `RevenueByStaffPage` |
| `/store/revenue-by-store` | `RevenueByStorePage` |
| `/store/revenue-detail` | `DetailRevenuePage` |
| `/store/revenue-category` | `SalesByCategoryPage` |
| `/store/revenue-hourly` | `RevenueHourlyPage` |
| `/store/payment-breakdown` | `PaymentBreakdownPage` |
| `/store/top-product` | `TopProductPage` |
| `/store/loyalty` | `LoyaltyPage` |

### 5.2 `BackOfficeAndAbove` — BackOffice + ITOps + SystemAdmin (Danh mục / Khuyến mãi)

| Route | Page |
|---|---|
| `/catalog/employees` | `EmployeesPage` |
| `/catalog/stores` | `StorePage` (namespace `Ops`) |
| `/catalog/provinces` | `ProvincesPage` |
| `/catalog/bank-pos` | `BankPosPage` |
| `/catalog/pos-versions`* | `PosVersionsPage` |
| `/catalog/print-setup`* | `PrintSetupPage` |
| `/catalog/posweb-users`* | `PoswebUsersPage` |
| `/catalog/products` | `ProductsPage` |
| `/catalog/product-lock` | `ProductLockPage` |
| `/catalog/product-lock-grabfood`* | `ProductLockGrabFoodPage` |
| `/catalog/setup-item`* | `SetupItemPage` |
| `/catalog/setup-item-partner`* | `SetupItemPartnerPage` |
| `/catalog/pos-groups`* | `PosGroupsPage` |
| `/catalog/prices` | `PricesPage` |
| `/catalog/price-setup` | `PriceSetupPage` |
| `/catalog/banks`* | `BanksPage` |
| `/catalog/bank-card-types`* | `BankCardTypesPage` |
| `/catalog/ewallets`* | `EwalletsPage` |
| `/catalog/currency-rates`* | `CurrencyRatesPage` |
| `/catalog/sales-order-types`* | `SalesOrderTypesPage` |
| `/catalog/qr-info`* | `QrInfoPage` |
| `/catalog/image-sliders`* | `ImageSlidersPage` |
| `/promotion/offers` | `OffersPage` |
| `/promotion/setup` | `PromotionSetupPage` |
| `/promotion/special-combo` | `SpecialComboPage` |
| `/promotion/coupons` | `CouponsPage` |
| `/promotion/coupons/issue`† | `CouponIssuePage` |
| `/promotion/vouchers` | `VouchersPage` |
| `/promotion/vouchers/issue`† | `VoucherIssuePage` |
| `/promotion/vouchers-published` | `VouchersPublishedPage` |

`*` = page tồn tại trong code nhưng **hiện bị comment trong menu** (`MainLayout.razor`) — vẫn
route được nếu biết URL trực tiếp (route protection vẫn hoạt động qua `[Authorize]`), chỉ không
hiển thị trên sidebar. `†` = chỉ vào được từ trang danh mục tương ứng (không có mục menu riêng).

### 5.3 `OpsAndAbove` — chỉ ITOps + SystemAdmin (Vận hành hạ tầng)

| Route | Page |
|---|---|
| `/ops/health` | `HealthPage` — System health |
| `/ops/alerts` | `AlertsPage` |
| `/ops/queues` | `QueuesPage` |
| `/ops/logs` | `LogsPage` — Interface Error |
| `/ops/data-raw-log` | `DataRawLogPage` |
| `/ops/pos-data-setup` | `PosDataSetupPage` |
| `/ops/pos-map`* | `PosMapPage` |
| `/ops/sql-console-audit`* | `SqlConsoleAuditPage` |

`*` = xem ghi chú ở mục 5.2. **BackOffice không có quyền vào các route này** — đây là điểm khác
biệt chính giữa `OpsAndAbove` và `BackOfficeAndAbove` sau khi thêm role `BackOffice`.

### 5.4 `AdminOnly` — chỉ SystemAdmin (Quản trị)

| Route | Page | Ghi chú |
|---|---|---|
| `/admin/users` | `UsersPage` | Quản lý `DashboardUsers` (CRUD + audit log) |
| `/admin/roles` | `RolesPage` | **Đang phát triển** — hiện chỉ hiển thị placeholder "Tính năng đang được phát triển" |
| `/admin/config` | `ConfigPage` | Cấu hình hệ thống |
| `/admin/audit` | `AuditPage` | Xem audit log CRUD toàn app |
| `/admin/sql-console` | `SqlConsolePage` | Chạy SQL trực tiếp — xem `security.md` mục 6 |
| `/admin/encrypt-secret` | `EncryptSecretPage` | Tạo token `enc:` cho credentials |

> **`/admin/roles` (`RolesPage.razor`) hiện là trang rỗng/placeholder** — không có UI tạo/sửa
> role động. Đừng nhầm đây là nơi cấu hình phân quyền: cấu hình quyền THẬT nằm ở
> `[Authorize(Policy=...)]` trên từng page (code) + cột `Role` của từng user trong `/admin/users`.

---

## 6. Cách áp dụng/gán quyền cho user (vận hành thực tế)

### 6.1 Tạo/sửa user qua UI (khuyến nghị)

1. Đăng nhập bằng tài khoản `SystemAdmin` → menu **Quản trị → Users** (`/admin/users`).
2. Nút "Thêm mới" → điền `Username`/`FullName`/`Password` → chọn **Role** (dropdown 4 giá trị cố
   định: Cửa hàng / BackOffice (Vận hành nghiệp vụ) / IT Ops / System Admin) → nếu Role = Cửa
   hàng, có thể giới hạn **Store codes**. Role BackOffice không có tùy chọn Store codes — luôn
   xem tất cả store, giống IT Ops/System Admin.
3. Lưu → `WebUserService.CreateAsync` → BCrypt hash password → insert `DashboardUsers` → ghi
   audit log (`AuditLogger.LogAsync`, xem `CLAUDE.md` §16 Audit Log).
4. Sửa role/store codes/trạng thái tương tự qua nút "Sửa" (`UpdateAsync`) — đổi password để trống
   nếu không muốn đổi.
5. "Khóa" tài khoản = soft-delete (`DeleteAsync` → `IsActive=0`, không xóa hàng); "Kích hoạt lại"
   = `ActivateAsync` → `IsActive=1`.

> Đổi Role của user đang có phiên đăng nhập **không có hiệu lực ngay** — claim `Role` đã nằm
> trong cookie hiện tại, chỉ cập nhật khi user đăng xuất/đăng nhập lại (cookie sliding 8h).

### 6.2 Tạo user trực tiếp bằng SQL (khi cần seed/khôi phục)

- Dùng skill `/web-ops gen-hash` để sinh BCrypt hash cho password trước khi viết `INSERT`.
- Xem mẫu seed trong `001_DashboardUsers.sql` (user `admin` mặc định).
- **Role phải khớp chính xác** 1 trong 4 chuỗi: `StoreOperator` / `BackOffice` / `ITOps` /
  `SystemAdmin` (phân biệt hoa/thường, không có khoảng trắng) — sai chính tả sẽ khiến
  `RequireRole` không match, user bị chặn ở mọi trang `[Authorize]` mà không có thông báo lỗi rõ
  ràng (chỉ redirect `/access-denied`).

### 6.3 Thêm quyền cho 1 page mới (dành cho dev)

1. Xác định page thuộc nhóm nào theo bảng §5 → chọn đúng policy (`StoreAndAbove` /
   `BackOfficeAndAbove` / `OpsAndAbove` / `AdminOnly`).
2. Thêm `@attribute [Authorize(Policy = WebPolicies.{Policy})]` ngay đầu file `.razor` — **bắt
   buộc**, đây là lớp bảo vệ server-side thật sự.
3. Thêm `MudNavLink` vào đúng `AuthorizeView Policy="@WebPolicies.{Policy}"` trong
   `MainLayout.razor` — **chỉ là UX** (ẩn/hiện menu), KHÔNG thay thế bước 2.
4. Nếu page hiển thị dữ liệu theo store → áp dụng row-level filter theo `store_codes` claim
   (xem mục 4) — không được bỏ qua.
5. Không cần đăng ký gì thêm ở tầng policy/role — 4 policy đã cố định, page mới chỉ chọn 1 trong 4.

### 6.4 Thêm role mới — ví dụ đã triển khai (`BackOffice`, thêm 2026-07-09)

Role thứ 4 `BackOffice` (nằm giữa `StoreOperator` và `ITOps`, quản lý Danh mục + Khuyến mãi,
không vào được `/ops/*`) đã được thêm theo đúng checklist dưới đây — dùng làm mẫu tham chiếu khi
cần thêm role tiếp theo:
- Thêm hằng số role mới (`WebRoles.BackOffice`) + policy mới (`WebPolicies.BackOfficeAndAbove`)
  trong `WebRoles.cs`.
- Thêm `AddPolicy(WebPolicies.BackOfficeAndAbove, ...)` + bổ sung role mới vào
  `RequireRole` của policy `StoreAndAbove` trong `Program.cs`.
- Đổi `@attribute [Authorize(Policy = ...)]` của toàn bộ page `/catalog/*` + `/promotion/*` từ
  `OpsAndAbove` sang `BackOfficeAndAbove` (giữ nguyên `/ops/*` dùng `OpsAndAbove`).
- Đổi `AuthorizeView Policy` của 2 nhóm menu DANH MỤC + KHUYẾN MÃI trong `MainLayout.razor`
  sang `BackOfficeAndAbove` (giữ nguyên nhóm VẬN HÀNH dùng `OpsAndAbove`).
- Cập nhật `MudSelectItem` role trong `UsersPage.razor` (dropdown filter) và
  `UserFormDialog.razor` (dropdown tạo/sửa user — đây mới là nơi thật sự gán role cho user).
- Cập nhật bảng route→policy (§5) và bảng role/policy (§1) trong file này.
- Không cần điền nội dung `/admin/roles` (`RolesPage.razor`) — vẫn giữ nguyên cơ chế role là
  hằng số cứng trong code, chưa có UI quản lý role động.

Muốn thêm permission per-action (mịn hơn cấp role/policy) vẫn KHÔNG có cơ chế sẵn — cần thiết kế
mới nếu được yêu cầu (không nằm trong phạm vi thay đổi này).

---

## 7. Audit trail cho thay đổi quyền

Mọi CRUD trên `DashboardUsers` qua `/admin/users` được ghi vào audit log (bảng tạo bởi
`003_DashboardAuditLog.sql`, xem `CLAUDE.md` §16) — bao gồm giá trị cũ/mới của `Role`/`StoreCodes`
dạng JSON. Xem lịch sử tại `/admin/audit` (`AuditPage`, `AdminOnly`).

---

## 8. Liên quan

| Chủ đề | Xem file |
|---|---|
| Hardening hạ tầng (HTTPS/CSP/credentials/SQL Console) | `docs/web/security/security.md` |
| Luồng đăng nhập chi tiết (bridge token, cookie) | `docs/web/logic/login-flow.md` |
| Audit log CRUD pattern | `.claude/skills/web/audit-logging.md` |
| Cấu trúc code dùng chung (DTO/Service/Repository) | `docs/CURRENT_STRUCTURE.md` |
| Slash command sinh BCrypt hash | `/web-ops gen-hash` |
