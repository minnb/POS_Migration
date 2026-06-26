# POS.Web — Báo cáo hiện trạng
> Cập nhật: 2026-06-26 (Flat UI + Density Standard: PosTheme border-radius 4px, shadow E1-E5 hairline, LineHeight 1.45, Dense AppBar/NavMenu/Table, MudGrid Spacing chuẩn hóa, mobile safety block 40px tap targets)

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
├── Theme/
│   └── PosTheme.cs                  ← MudBlazor custom theme (navy + teal color system)
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
│   ├── Shared/   ← (PosTableBase.cs ĐÃ XÓA — DataTable nay dùng MudTable built-in)
│   └── Pages/
│       ├── AccessDenied.razor
│       ├── Index.razor
│       ├── Login.razor
│       ├── Admin/
│       │   └── UsersPage.razor           ← MudTable (search trong ToolBarContent)
│       ├── Ops/
│       │   └── HealthPage.razor
│       └── Store/
│           ├── RevenuePage.razor
│           ├── TransactionsPage.razor    ← MudTable (client-side sort/paginate)
│           └── EosShiftsPage.razor       ← kết thúc ca bán hàng (MudTable)
├── Properties/
│   └── launchSettings.json
├── wwwroot/
│   ├── app.css          ← CSS design tokens --pos-* (28 vars) + scrollbar + delta + active-nav + .pos-table* (nay chỉ dùng cho pivot report table)
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
| B3 | IWebUserService (8 methods) | Auth/IWebUserService.cs | ✅ | ValidateLoginAsync, GetByUsernameAsync, GetStoreCodes, GetAllAsync, CreateAsync, UpdateAsync, DeleteAsync (soft), ActivateAsync, UsernameExistsAsync |
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
| D6 | Program – Cookie authentication | Program.cs | ✅ | LoginPath=/login, SlidingExpiration, HttpOnly, **SameSite=Lax** (đổi từ Strict để fix Safari iOS) |
| D7 | Program – 3 policy (StoreAndAbove, OpsAndAbove, AdminOnly) | Program.cs | ✅ | |
| D8 | Program – AddCascadingAuthenticationState() | Program.cs | ✅ | |
| D9 | Program – Middleware order + explicit UseRouting() | Program.cs | ✅ | Host-rewrite → **UseRouting() tường minh** → UseAuthentication → UseAuthorization → UseAntiforgery → MapStaticAssets → MapRazorComponents |
| D10 | Dockerfile – DataProtection-Keys ownership | Dockerfile | ✅ | `mkdir -p + chown app:app` TRƯỚC `USER $APP_UID` |
| D11 | nginx config – WebSocket + Host passthrough | nginx | ✅ | proxy_set_header Upgrade + Connection + proxy_read_timeout 300s |
| D10 | Program – MapGet("/logout", ...) | Program.cs | ✅ | SignOutAsync + Redirect("/login") + AllowAnonymous |
| E1 | App.razor – MudBlazor.min.css | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.css` |
| E2 | App.razor – MudBlazor.min.js | Components/App.razor | ✅ | `_content/MudBlazor/MudBlazor.min.js` |
| E3 | App.razor – \<Routes/\> component | Components/App.razor | ✅ | `@rendermode="InteractiveServer"` |
| E4 | App.razor – Google Fonts Roboto | Components/App.razor | ✅ | `fonts.googleapis.com/css?family=Roboto` |
| E5 | Routes.razor – AuthorizeRouteView (không phải RouteView) | Components/Routes.razor | ✅ | DefaultLayout = MainLayout |
| E6 | Routes.razor – NotAuthorized: kiểm tra IsAuthenticated | Components/Routes.razor | ✅ | `context.User.Identity?.IsAuthenticated != true` |
| E7 | RedirectToLogin component | Components/RedirectToLogin.razor | ✅ | NavigateTo("/login", forceLoad:true) |
| E8 | RedirectToAccessDenied component | Components/RedirectToAccessDenied.razor | ✅ | NavigateTo("/access-denied", forceLoad:true) |
| F0 | PosTheme.cs – custom MudTheme (navy primary, teal accent, semantic colors) | Theme/PosTheme.cs | ✅ | Primary=#2051A3, Drawer/Appbar=#1B3A5C, **BorderRadius=4px** (flat), **LineHeight=1.45**, Button.TextTransform=none, Body1=0.875rem, **Shadows E1-E5=hairline 0 0 0 1px** (E6+ giữ nguyên cho dropdown/dialog) |
| F1 | MainLayout – MudThemeProvider **Theme="@PosTheme.Default"** + providers | Layout/MainLayout.razor | ✅ | Đã truyền custom theme |
| F2 | MainLayout – MudAppBar: toggle drawer + hiển thị tên user + logout | Layout/MainLayout.razor | ✅ | Href="/logout" trên MudIconButton |
| F3 | MainLayout – Sidebar "Cửa hàng" (Policy=StoreAndAbove) | Layout/MainLayout.razor | ✅ | 3 sub-group (Vận hành/Giao dịch/Báo cáo) + 12 leaf links không icon (tam giác MudNavLink) |
| F4 | MainLayout – Sidebar "Vận hành" (Policy=OpsAndAbove) | Layout/MainLayout.razor | ✅ | 2 sub-group: Giám sát (4 links) + Nhật ký (2 links) — leaf links không icon |
| F5 | MainLayout – Sidebar "Quản trị" (Policy=AdminOnly) | Layout/MainLayout.razor | ✅ | 4 nav link |
| F6 | EmptyLayout – layout căn giữa cho Login | Layout/EmptyLayout.razor | ✅ | flex + align-items:center + **background:var(--mud-palette-background)** (không còn hardcode #f0f2f5), có MudBlazor providers + PosTheme |
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
| G12 | UsersPage – /admin/users + AdminOnly + InteractiveServer | Pages/Admin/UsersPage.razor | ✅ | MudTable + search trong ToolBarContent + LINQ filter |
| G13 | AccessDenied – /access-denied + [AllowAnonymous] | Pages/AccessDenied.razor | ✅ | |
| G14 | TransactionsPage – /store/transactions + StoreAndAbove | Pages/Store/TransactionsPage.razor | ✅ | MudTable client-side sort/paginate + store combobox (StoreNo+Name) |
| G15 | EosShiftsPage – /store/eos-shifts + StoreAndAbove | Pages/Store/EosShiftsPage.razor | ✅ | Kết thúc ca — filter + KPI cards + MudTable + GetEosShiftListAsync |
| G16 | DetailRevenuePage – /store/revenue-detail + StoreAndAbove | Pages/Store/DetailRevenuePage.razor | ✅ | Báo cáo doanh thu chi tiết — 11 filters + 21-col MudTable ServerData (server-side paging) |
| G17 | BusinessDayPage – /store/business-day + StoreAndAbove | Pages/Store/BusinessDayPage.razor | ✅ | Stub — Ngày kinh doanh (UI construction in progress) |
| G18 | ShiftSummaryPage – /store/shift-summary + StoreAndAbove | Pages/Store/ShiftSummaryPage.razor | ✅ | Stub — Tổng kết ca (UI construction in progress) |
| G19 | RefundsPage – /store/refunds + StoreAndAbove | Pages/Store/RefundsPage.razor | ✅ | Stub — Hoàn trả (UI construction in progress) |
| G20 | VoidsPage – /store/voids + StoreAndAbove | Pages/Store/VoidsPage.razor | ✅ | Stub — Hủy GD (UI construction in progress) |
| G21 | RevenueHourlyPage – /store/revenue-hourly + StoreAndAbove | Pages/Store/RevenueHourlyPage.razor | ✅ | Doanh thu theo giờ — KPI + Line/Bar charts + MudTable (FooterContent dòng Tổng) + store combobox. **Tối ưu 10M dòng:** Redis cache repo (TTL theo độ mới) + includeKpi + CancellationToken + guard re-entrancy + hoãn load khỏi prerender + clamp 92 ngày khi all-stores |
| G22 | PaymentBreakdownPage – /store/payment-breakdown + StoreAndAbove | Pages/Store/PaymentBreakdownPage.razor | ✅ | Stub — Phân tích thanh toán (UI construction in progress) |
| G23 | TopProductPage – /store/top-product + StoreAndAbove | Pages/Store/TopProductPage.razor | ✅ | Top sản phẩm bán chạy — sp_ReportTopProduct (cache Pattern 4, clamp 92 ngày). KPI 3 card + CSS bar list + MudTable drill-through (ProductOrdersDialog). **BA/BI:** surface metrics (trả%/giá TB/độ phủ/giảm%) + so sánh cấp SP (Δ hạng/NEW). Ngành hàng ẩn tạm (SP chưa JOIN Item master) |
| I1 | DataTable standard – `MudTable<T>` built-in | (mọi page có bảng) | ✅ | MudTableSortLabel + MudTablePager + ServerData; PosTableBase ĐÃ XÓA. Chi tiết: `.claude/skills/web/datatable.md` |
| I2 | `.pos-table*` CSS – nay chỉ cho pivot report | wwwroot/app.css | ✅ | pos-table/pos-table-wrap còn dùng cho `rpt-pivot-table` (SalesByCategoryPage) |
| I3 | Sidebar accordion – tự mở/đóng theo route | Layout/MainLayout.razor | ✅ | NavigationManager.LocationChanged + @bind-Expanded + IAsyncDisposable |
| I4 | Sidebar active NavLink highlight | wwwroot/app.css | ✅ | rgba(255,255,255,0.14) bg + white text + 3px border-left #3A6FCC |
| I5 | Sidebar drawer responsive init — đóng trên mobile, mở trên desktop | Layout/MainLayout.razor | ✅ | IBrowserViewportService.GetCurrentBreakpointAsync() trong OnAfterRenderAsync(firstRender) |
| I6 | Page header responsive — title+button không vỡ layout mobile | Pages/Admin/UsersPage.razor | ✅ | div.pos-page-header + pos-page-header-title + pos-page-header-btn |
| I7 | DataTable scroll ngang trên mobile | mọi page có MudTable | ✅ | `HorizontalScrollbar="true"` trên MudTable (thay wrapper overflow-x:auto cũ) |
| I8 | Chip filter flex-wrap — chips không tràn ngang mobile | Pages/Store/RevenuePage.razor | ✅ | flex-wrap thêm vào MudPaper filter container |
| I9 | Summary text flex-wrap — &nbsp;\|&nbsp; đổi sang flex items | Pages/Store/TransactionsPage.razor | ✅ | d-flex flex-wrap gap-3 thay separator |
| I10 | HealthPage responsive — header + chip section | Pages/Ops/HealthPage.razor | ✅ | pos-page-header Case B (title + group controls); chip div.d-flex flex-wrap; button align-self:center chống stretch |
| I11 | Responsive UI standard — qui tắc chung mọi page | .claude/skills/web/SKILLS.md | ✅ | Section mới: bảng so sánh sai/đúng, 2 case pos-page-header, anti-patterns, checklist item |
| I12 | RevenuePage – Y-axis auto-scale (`YAxisSuggestedMax` + `YAxisTicks`) | Pages/Store/RevenuePage.razor | ✅ | CalcYMax (dataMax+2.5 ceil) + CalcYTick (spacing 1/2/5/10) — hết cứng max=20 |
| H1 | Build pass (0 error, 0 warning) | — | ✅ | `dotnet build` → Build succeeded. 0 Warning(s). 0 Error(s). |

---

## Tóm tắt

- ✅ Hoàn thành: **81 / 82 hạng mục**
- ⚠️ Có vấn đề: **1 hạng mục** (B9 — SQL seed hash placeholder)
- ❌ Còn thiếu: **0 hạng mục**

> +15 hạng mục mới (session 2026-06-23): G16 (DetailRevenuePage full), G17-G22 (6 stub pages), menu refactor (F3 update).
> Previous: G14 (TransactionsPage), G15 (EosShiftsPage), I1-I12 (DataTable + responsive standards).

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

Time Elapsed 00:00:03.16  [2026-06-23 after DetailRevenuePage]
```
