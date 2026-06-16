# POS.Web — Báo cáo hiện trạng
> Cập nhật: 2026-06-15 (so sánh với đặc tả WEB-01)

---

## Cây thư mục hiện tại
_(bỏ qua bin/ và obj/)_

```
src/POS.Web/
├── Auth/
│   ├── DashboardUser.cs
│   ├── IWebUserService.cs
│   ├── migration_dashboard_users.sql
│   ├── WebRoles.cs
│   └── WebUserService.cs
├── Components/
│   ├── _Imports.razor
│   ├── App.razor
│   ├── RedirectToAccessDenied.razor
│   ├── RedirectToLogin.razor
│   ├── Routes.razor
│   ├── Layout/
│   │   ├── EmptyLayout.razor
│   │   ├── MainLayout.razor
│   │   ├── MainLayout.razor.css
│   │   ├── ReconnectModal.razor          ← template, dùng bởi App.razor
│   │   └── ReconnectModal.razor.css
│   └── Pages/
│       ├── AccessDenied.razor
│       ├── Index.razor
│       ├── Login.razor
│       ├── Admin/
│       │   └── UsersPage.razor
│       ├── Ops/
│       │   └── HealthPage.razor
│       └── Store/
│           └── RevenuePage.razor
├── Properties/
│   └── launchSettings.json
├── wwwroot/
│   ├── app.css
│   ├── favicon.png
│   └── lib/bootstrap/   ← ~30 CSS file template, CHƯA XÓA (không gây lỗi)
├── appsettings.json
├── appsettings.Development.json
├── POS.Web.csproj
└── Program.cs
```

---

## Kết quả kiểm tra

| # | Hạng mục | File | Trạng thái | Vấn đề (nếu có) |
|---|----------|------|-----------|-----------------|
| A1 | Project file – target framework | POS.Web.csproj | ✅ | net10.0 |
| A2 | Project ref – POS.Infrastructure | POS.Web.csproj | ✅ | |
| A3 | Project ref – POS.Application | POS.Web.csproj | ✅ | |
| A4 | Project ref – POS.Common | POS.Web.csproj | ✅ | |
| A5 | Package MudBlazor | POS.Web.csproj | ✅ | 9.5.0 |
| A6 | Package BCrypt.Net-Next | POS.Web.csproj | ✅ | 4.2.0 |
| A7 | Package Newtonsoft.Json | POS.Web.csproj | ✅ | 13.0.4 |
| A8 | Package Microsoft.AspNetCore.Components.Authorization | POS.Web.csproj | ✅ | Không cần — built-in .NET 10, bỏ đúng để tránh NU1510 |
| B1 | WebRoles + WebPolicies (3 const mỗi loại) | Auth/WebRoles.cs | ✅ | StoreOperator, ITOps, SystemAdmin / StoreAndAbove, OpsAndAbove, AdminOnly |
| B2 | DashboardUser model (7 fields) | Auth/DashboardUser.cs | ✅ | Id, Username, PasswordHash, FullName, Role, StoreCodes?, IsActive |
| B3 | IWebUserService (3 methods) | Auth/IWebUserService.cs | ✅ | ValidateLoginAsync, GetByUsernameAsync, GetStoreCodes |
| B4 | WebUserService – inject CentralMDConnectionFactory (concrete) | Auth/WebUserService.cs | ✅ | Primary constructor injection, không qua interface |
| B5 | WebUserService – inject IFileLogHelper | Auth/WebUserService.cs | ✅ | |
| B6 | WebUserService – BCrypt.Verify | Auth/WebUserService.cs | ✅ | `BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)` |
| B7 | WebUserService – GetStoreCodes JSON deserialize | Auth/WebUserService.cs | ✅ | `JsonConvert.DeserializeObject<List<string>>(user.StoreCodes)` |
| B8 | SQL migration – CREATE TABLE DashboardUsers | Auth/migration_dashboard_users.sql | ✅ | IF NOT EXISTS, đủ cột, UNIQUE constraint trên Username |
| B9 | SQL migration – seed admin | Auth/migration_dashboard_users.sql | ⚠️ | Seed tồn tại nhưng `HASH_PLACEHOLDER` chưa được thay bằng BCrypt hash thật |
| C1 | appsettings – ConnectionStrings (CentralMD, Loyalty, StagingDB) | appsettings.json | ✅ | 3 key có mặt + thêm: Partner, EInvoice, IFSAP, CentralSale |
| C2 | appsettings – Redis (Mode, SentinelHosts, MasterName, DefaultDatabase) | appsettings.json | ✅ | Mode=StandAlone, DefaultDatabase=2 |
| C3 | appsettings – RabbitMQ (Host, Port, Username, Password) | appsettings.json | ✅ | |
| C4 | appsettings – Elasticsearch (Nodes, IndexFormat) | appsettings.json | ✅ | IndexFormat=`pos-web-logs-{0:yyyy.MM.dd}` |
| C5 | appsettings – Logging.FileLogDirectory | appsettings.json | ✅ | `D:\\ROOT\\Logs\\POS.Web` |
| C6 | appsettings – WebApp (AppName, SessionTimeoutHours) | appsettings.json | ✅ | AppName="POS Dashboard – WinMart", SessionTimeoutHours=8 |
| D1 | Program – AddMudServices() | Program.cs | ✅ | SnackbarConfiguration.PositionClass = BottomRight |
| D2 | Program – AddRazorComponents().AddInteractiveServerComponents() | Program.cs | ✅ | |
| D3 | Program – AddInfrastructure(builder.Configuration) | Program.cs | ✅ | |
| D4 | Program – AddApplication() | Program.cs | ✅ | |
| D5 | Program – AddScoped\<IWebUserService, WebUserService\>() | Program.cs | ✅ | |
| D6 | Program – Cookie authentication | Program.cs | ✅ | LoginPath=/login, SlidingExpiration, HttpOnly, SameSite=Strict |
| D7 | Program – 3 policy (StoreAndAbove, OpsAndAbove, AdminOnly) | Program.cs | ✅ | |
| D8 | Program – AddCascadingAuthenticationState() | Program.cs | ✅ | |
| D9 | Program – UseAuthentication() TRƯỚC UseAuthorization() | Program.cs | ✅ | Thứ tự: UseStaticFiles → UseAuthentication → UseAuthorization → UseAntiforgery |
| D10 | Program – MapGet("/logout", ...) | Program.cs | ✅ | SignOutAsync + Redirect("/login") + AllowAnonymous |
| E1 | App.razor – MudBlazor.min.css | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.css` |
| E2 | App.razor – MudBlazor.min.js | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.js` |
| E3 | App.razor – \<Routes/\> component | Components/App.razor | ✅ | `@rendermode="InteractiveServer"` |
| E4 | App.razor – Google Fonts Roboto | Components/App.razor | ✅ | `fonts.googleapis.com/css?family=Roboto` |
| E5 | Routes.razor – AuthorizeRouteView (không phải RouteView) | Components/Routes.razor | ✅ | DefaultLayout = MainLayout |
| E6 | Routes.razor – NotAuthorized: kiểm tra IsAuthenticated | Components/Routes.razor | ✅ | `context.User.Identity?.IsAuthenticated != true` |
| E7 | RedirectToLogin component | Components/RedirectToLogin.razor | ✅ | NavigateTo("/login", forceLoad:true) |
| E8 | RedirectToAccessDenied component | Components/RedirectToAccessDenied.razor | ✅ | NavigateTo("/access-denied", forceLoad:true) |
| F1 | MainLayout – MudThemeProvider + MudDialogProvider + MudSnackbarProvider | Layout/MainLayout.razor | ✅ | |
| F2 | MainLayout – MudAppBar: toggle drawer + hiển thị tên user + logout | Layout/MainLayout.razor | ✅ | Href="/logout" trên MudIconButton |
| F3 | MainLayout – Sidebar "Cửa hàng" (Policy=StoreAndAbove) | Layout/MainLayout.razor | ✅ | 4 nav link |
| F4 | MainLayout – Sidebar "Vận hành" (Policy=OpsAndAbove) | Layout/MainLayout.razor | ✅ | 5 nav link |
| F5 | MainLayout – Sidebar "Quản trị" (Policy=AdminOnly) | Layout/MainLayout.razor | ✅ | 4 nav link |
| F6 | EmptyLayout – layout căn giữa cho Login | Layout/EmptyLayout.razor | ✅ | flex + align-items:center + background:#f0f2f5, có MudBlazor providers |
| G1 | Login.razor – @page "/login" | Pages/Login.razor | ✅ | |
| G2 | Login.razor – @layout Layout.EmptyLayout | Pages/Login.razor | ✅ | |
| G3 | Login.razor – @attribute [AllowAnonymous] | Pages/Login.razor | ✅ | |
| G4 | Login.razor – @rendermode InteractiveServer | Pages/Login.razor | ✅ | |
| G5 | Login.razor – MudTextField username + password | Pages/Login.razor | ✅ | Password có toggle show/hide (Adornment.End pattern đúng MudBlazor 9.x) |
| G6 | Login.razor – DoLogin gọi ValidateLoginAsync | Pages/Login.razor | ✅ | |
| G7 | Login.razor – DoLogin tạo ClaimsPrincipal + gọi SignInAsync | Pages/Login.razor | ✅ | Claims: Name, Role, full_name, store_codes |
| G8 | Index.razor – @page "/" + [Authorize] | Pages/Index.razor | ✅ | |
| G9 | Index.razor – redirect theo role | Pages/Index.razor | ✅ | SystemAdmin→/admin/users, ITOps→/ops/health, other→/store/revenue |
| G10 | RevenuePage – /store/revenue + StoreAndAbove + InteractiveServer | Pages/Store/RevenuePage.razor | ✅ | |
| G11 | HealthPage – /ops/health + OpsAndAbove + InteractiveServer | Pages/Ops/HealthPage.razor | ✅ | |
| G12 | UsersPage – /admin/users + AdminOnly + InteractiveServer | Pages/Admin/UsersPage.razor | ✅ | |
| G13 | AccessDenied – /access-denied + [AllowAnonymous] | Pages/AccessDenied.razor | ✅ | |
| H1 | Build pass (0 error, 0 warning) | — | ✅ | `dotnet build` → Build succeeded. 0 Warning(s). 0 Error(s). |

---

## Tóm tắt

- ✅ Hoàn thành: **54 / 55 hạng mục**
- ⚠️ Có vấn đề: **1 hạng mục**
- ❌ Còn thiếu: **0 hạng mục**

---

## Các vấn đề cần xử lý

### 🟡 Cần bổ sung trước khi chạy SQL migration
**B9 — SQL seed có HASH_PLACEHOLDER**
File: `src/POS.Web/Auth/migration_dashboard_users.sql`

```sql
-- Thay HASH_PLACEHOLDER bằng hash thật, ví dụ trong C#:
-- string hash = BCrypt.Net.BCrypt.HashPassword("Admin@2024!");
INSERT INTO DashboardUsers (Username, PasswordHash, ...)
VALUES ('admin', 'HASH_PLACEHOLDER', ...)   -- ← CHƯA THAY
```
→ Chạy đoạn C# để sinh hash, copy vào file SQL trước khi execute.

### 🟢 Quan sát bổ sung (không ảnh hưởng app)
- **wwwroot/lib/bootstrap/**: ~30 file CSS Bootstrap từ template vẫn còn trong `wwwroot/`. Không được reference (dùng MudBlazor CDN), không gây lỗi, nhưng chiếm dung lượng. Có thể xóa lúc cleanup.
- **ReconnectModal.razor**: File từ template gốc, được giữ vì `App.razor` dùng `<ReconnectModal/>`. Không cần sửa.

### 🟢 Đã xong — không cần làm thêm
Tất cả 54 hạng mục còn lại: Project references, Auth layer, Configuration, Program.cs pipeline, Blazor root components, Layouts, tất cả Pages, Build.

---

## Build output

```
dotnet build src/POS.Web/POS.Web.csproj

  POS.Common        → bin/Debug/net10.0/POS.Common.dll
  POS.Infrastructure → bin/Debug/net10.0/POS.Infrastructure.dll
  POS.Application   → bin/Debug/net10.0/POS.Application.dll
  POS.Web           → bin/Debug/net10.0/POS.Web.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:08.78
```
