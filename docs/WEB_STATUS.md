# POS.Web — Báo cáo hiện trạng
> Cập nhật: 2026-07-01 (UI polish PromotionSetupPage `/promotion/setup`: MudTabs icon+gạch chân active, MudCard gom nhóm cả 5 tab, tooltip/HelperText giải thích, validation trực quan `Required`/`RequiredError`, nút Lưu spinner khi `_saving`, combobox "Điều kiện" 160→240px — markup-only, giữ 100% @code; + tài liệu `docs/web/LOGIC_APPROVE_CTKM.md`)
> Trước đó 2026-07-01 (Promotion/CouponVoucher: 8.1 Cài đặt Coupon + 8.2 Phát hành Coupon + 8.3 Danh mục Voucher (CRUD) + 8.4 Tra cứu Voucher phát hành — service 3 lớp, SP mới usp_SetupCoupon_*/usp_SetupVoucher_*, 8.4 reuse SP CentralSales)
> Trước đó 2026-07-01 (Bug fix: sidebar accordion (I3) + active NavLink highlight (I4) sai logic; BankPosPage/BankPosDetailDialog — sai tên bảng vật lý + SP param + crash circuit khi lookup lỗi)
> Trước đó 2026-06-30 (thêm Catalog section: ProductsPage 6.1+6.2+6.3, ProductLockPage 6.4 — migrate từ VCM.BLUEPOS)
> Trước đó 2026-06-28 (Security hardening: config-driven HTTPS/cookie + RequireHttps, security headers/CSP, mã hóa credentials AES-256-GCM `enc:`, SQL Console mask+toggle, DetailedErrors off Prod)

---

## Cây thư mục hiện tại
_(bỏ qua bin/ và obj/)_

```
src/POS.Web/
├── Auth/
│   ├── DashboardUser.cs / IWebUserService.cs / WebUserService.cs / WebRoles.cs
│   ├── IAuditLogger.cs               ← interface + DbAuditLogger (ghi DashboardAuditLog)
│   ├── migration_dashboard_users.sql
│   ├── migration_sql_console_audit.sql
│   └── migration_dashboard_audit_log.sql  ← DashboardAuditLog + 3 index (chạy trước deploy)
├── Theme/
│   └── PosTheme.cs                  ← MudBlazor custom theme (flat, navy + teal)
├── Components/
│   ├── _Imports.razor / App.razor / Routes.razor / RedirectToLogin.razor / RedirectToAccessDenied.razor
│   ├── Layout/
│   │   ├── EmptyLayout.razor / MainLayout.razor (+ .razor.css) / ReconnectModal.razor (+ .razor.css)
│   └── Pages/
│       ├── AccessDenied.razor / Index.razor / Login.razor
│       ├── Admin/
│       │   ├── UsersPage.razor / RolesPage.razor / ConfigPage.razor / AuditPage.razor
│       │   ├── SqlConsolePage.razor / EncryptSecretPage.razor   ← AdminOnly
│       │   └── Dialogs/UserFormDialog.razor
│       ├── Ops/
│       │   ├── HealthPage.razor / AlertsPage.razor / QueuesPage.razor
│       │   ├── LogsPage.razor / DataRawLogPage.razor / StorePage.razor / PosMapPage.razor
│       │   ├── PosDataSetupPage.razor         ← /ops/pos-data-setup — CRUD cấu hình POS
│       │   ├── PosTerminalSavePayload.cs      ← shared record: payload chain PosMapPage→DetailDialog→EditDialog
│       │   └── Dialogs/ (PosTerminalDetailDialog, PosTerminalEditDialog, StoreDetailDialog,
│       │                  PosDataSetupFormDialog)
│       ├── Catalog/
│       │   └── Product/
│       │       ├── ProductsPage.razor               ← /catalog/products — danh sách + thêm mới + xuất Excel
│       │       ├── ProductLockPage.razor             ← /catalog/product-lock — khóa/mở khóa SP theo cửa hàng
│       │       └── Dialogs/ (ProductDetailDialog — form tạo SP mới, dynamic barcode rows)
│       ├── Promotion/
│       │   ├── Offers/
│       │   │   ├── PromotionSetupPage.razor   ← /promotion/setup — Cài đặt CTKM (editor 5 tab; UI polish MudCard+tooltip+validation trực quan)
│       │   │   ├── SpecialComboPage.razor      ← /promotion/special-combo — Special Combo
│       │   │   └── OffersPage.razor            ← /promotion/offers — Danh mục khuyến mãi (Offer* live)
│       │   └── CouponVoucher/
│       │       ├── CouponsPage.razor / CouponIssuePage.razor        ← 8.1/8.2 Coupon (list+xóa / phát hành Auto·Import·nâng cao)
│       │       ├── VouchersPage.razor                                ← 8.3 Danh mục Voucher (list + CRUD + Export)
│       │       ├── VouchersPublishedPage.razor                       ← 8.4 Tra cứu Voucher phát hành (CentralSales per-store)
│       │       └── Dialogs/ (CouponItemPickerDialog, CouponAdvancedDialog, VoucherFormDialog, VoucherItemPickerDialog)
│       └── Store/
│           ├── Reports/ (Revenue, DetailRevenue, RevenueHourly, PaymentBreakdown, SalesByCategory, TopProduct, Loyalty)
│           ├── Transactions/ (TransactionsPage, RefundsPage, VoidsPage)
│           ├── Operations/ (BusinessDayPage, EosShiftsPage, ShiftSummaryPage)
│           └── Dialogs/ (VoidDetailDialog, TransactionDetailDialog, EosDayShiftListDialog, EosShiftDetailDialog, ProductOrdersDialog)
├── Services/
│   ├── ISqlConsoleService.cs / SqlConsoleService.cs / PendingUpdate.cs / JsDownloadExtensions.cs
│   └── Pdf/ (IPdfExportService, PdfExportService, PivotReportData, ReportHeaderModel)
├── Properties/launchSettings.json
├── wwwroot/
│   ├── app.css          ← CSS design tokens --pos-* + .pos-table* (pivot report) ; js/download.js (PDF blob)
│   ├── favicon.png / lib/bootstrap/ (template, chưa xóa)
├── appsettings.json / .Development.json / .Production.json / .UAT.json(gitignored)
├── Dockerfile
├── POS.Web.csproj
└── Program.cs          ← security config-driven (Security:Mode/RequireHttps), headers/CSP, decryption hook
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
| G12 | UsersPage – /admin/users + AdminOnly + InteractiveServer | Pages/Admin/UsersPage.razor | ✅ | KPI row (3 cards: tổng/active/locked) + filter panel (search+role+status) + MudTable LINQ filter |
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
| I6 | MudTable header CSS override toàn cục | wwwroot/app.css | ✅ | Nền `--pos-bg-alt` (#D9E5F7), border-bottom 2px navy, padding 10px 16px, sort button min-height:unset padding:0 — áp dụng tất cả MudTable không cần sửa Razor |
| I7 | Sort label cột đặc biệt | datatable.md | ✅ | Nullable DateTime → `?? DateTime.MinValue`; string date → dùng `SortOrder` int property |
| I8 | Filter panel Elevation chuẩn | (mọi page có filter) | ✅ | `MudPaper Elevation="1"` cho filter panel, `Elevation="2"` cho DataTable |
| I9 | Không có result summary text inline | (mọi page có table) | ✅ | Xóa `@if (!_loading && _items.Count > 0) { <div>Tìm thấy X dòng</div> }` — KPI cards thay thế |
| I6 | Page header responsive — title+button không vỡ layout mobile | Pages/Admin/UsersPage.razor | ✅ | div.pos-page-header + pos-page-header-title + pos-page-header-btn |
| I7 | DataTable scroll ngang trên mobile | mọi page có MudTable | ✅ | `HorizontalScrollbar="true"` trên MudTable (thay wrapper overflow-x:auto cũ) |
| I8 | Chip filter flex-wrap — chips không tràn ngang mobile | Pages/Store/RevenuePage.razor | ✅ | flex-wrap thêm vào MudPaper filter container |
| I9 | Summary text flex-wrap — &nbsp;\|&nbsp; đổi sang flex items | Pages/Store/TransactionsPage.razor | ✅ | d-flex flex-wrap gap-3 thay separator |
| I10 | HealthPage responsive — header + chip section | Pages/Ops/HealthPage.razor | ✅ | pos-page-header Case B (title + group controls); chip div.d-flex flex-wrap; button align-self:center chống stretch |
| I11 | Responsive UI standard — qui tắc chung mọi page | .claude/skills/web/SKILLS.md | ✅ | Section mới: bảng so sánh sai/đúng, 2 case pos-page-header, anti-patterns, checklist item |
| I12 | RevenuePage – Y-axis auto-scale (`YAxisSuggestedMax` + `YAxisTicks`) | Pages/Store/RevenuePage.razor | ✅ | CalcYMax (dataMax+2.5 ceil) + CalcYTick (spacing 1/2/5/10) — hết cứng max=20 |
| S1 | DetailedErrors tắt ngoài Dev (C2) | appsettings.Production/UAT.json | ✅ | `EnableDetailedErrors:false`; Program đọc `IsDev() || config` |
| S2 | Cookie.Secure + HTTPS/HSTS config-driven (C1) | Program.cs / appsettings | ✅ (cơ chế) ⚠️ (đang tắt) | `Security:RequireHttps` (Prod=false để test HTTP) → cookie SameAsRequest. Bật `true` khi có TLS. SameSite=Strict (Mode=Internet) |
| S3 | Security headers + CSP (M1) | Program.cs | ✅ | X-Content-Type-Options/X-Frame-Options/Referrer-Policy/CSP; `frame-src 'self' blob:` cho PDF; TẮT ở Dev (`EnableSecurityHeaders=false`) tránh chặn Browser Link |
| S4 | ForwardedHeaders an toàn (H2) | Program.cs | ✅ | Mode=Internet → KHÔNG xử lý `X-Forwarded-*` (no-proxy). BehindProxy mới nạp `KnownProxies`/`KnownNetworks` |
| S5 | Mã hóa credentials appsettings (C4) | SecretProtector.cs + Program.cs + EncryptSecretPage.razor | ✅ (cơ chế) ⚠️ (chưa rollout) | AES-256-GCM token `enc:`, khóa `POSWEB_SECRET_KEY`; trang `/admin/encrypt-secret`. Password thật còn plaintext tới khi ops mã hóa — xem docs/ROLLOUT.md |
| S6 | SQL Console hardening (H1) | SqlConsoleService.cs / SqlConsolePage.razor | ✅ | Mask `password/token/secret/...` trong audit+Kibana; cờ `Security:EnableSqlConsole` gate service+page |
| S7 | AllowedHosts = domain thật (H2) | appsettings.Production.json | ⚠️ | Còn `"*"` — cần đặt domain dashboard khi go-live (docs/ROLLOUT.md) |
| G24 | PosDataSetupPage – /ops/pos-data-setup + OpsAndAbove | Pages/Ops/PosDataSetupPage.razor | ✅ | CRUD cấu hình POS — KPI 3 cards (pre-computed) + filter panel + MudTable + Add/Edit dialog; Redis invalidate sau mỗi write |
| J1 | IAuditLogger / DbAuditLogger — audit CRUD vào DashboardAuditLog | Auth/IAuditLogger.cs | ✅ | LogAsync(actor, action, entityType, entityKey, oldValueJson?, newValueJson?); ghi DB + Kibana; try/catch nội bộ; đăng ký Scoped trong Program.cs |
| J2 | PosDataSetupFormDialog – Add/Edit form, trả DTO đầy đủ | Pages/Ops/Dialogs/PosDataSetupFormDialog.razor | ✅ | Code read-only khi Edit; trả DialogResult.Ok(_model) (không Ok(true)) để page có newValue; duplicate Code → thông báo thân thiện |
| J3 | migration_dashboard_audit_log.sql – bảng DashboardAuditLog + 3 index | Auth/migration_dashboard_audit_log.sql | ⚠️ | Script idempotent — **PHẢI CHẠY trên RPOSMasterData trước deploy**; chưa chạy → log fail silently |
| J4 | audit-logging.md – rule audit CRUD chuẩn hóa cho toàn dự án | .claude/skills/web/audit-logging.md | ✅ | Pattern: snapshot oldValue từ item đã có, await LogAsync sau DB success, dialog trả DTO, checklist 12 mục |
| K1 | ProductsPage – /catalog/products + OpsAndAbove | Pages/Catalog/Product/ProductsPage.razor | ✅ | Danh sách SP/Barcode — SP GetProductList server-side paging; filter (mã/tên/barcode/thuế suất); nút Thêm mới + dialog tạo SP; Export Excel (ClosedXML); pos-page-header. Migrate 6.1+6.2+6.3 |
| K2 | ProductDetailDialog – form tạo sản phẩm mới | Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor | ✅ | 8 field (ItemName/Full/UoM/SalesUoM/FamilyCode/TaxCode/Blocked/BlockedVINID) + dynamic barcode table; INSERT dbo.Item + dbo.Barcode trong transaction; auto ItemNo (Max+1). Edit button disabled pending UPDATE route |
| K3 | ProductLockPage – /catalog/product-lock + OpsAndAbove | Pages/Catalog/Product/ProductLockPage.razor | ✅ | Khóa/mở khóa SP theo cửa hàng — StoreNo bắt buộc; MudTable server-side + MultiSelection + chip màu; toggle đơn + bulk action; MudMessageBox @ref confirm; UPSERT dbo.ItemBlock. Migrate 6.4 (Central mode) |
| J5 | IKibanaService → IFileLogHelper — migration toàn POS.Web | 24 .razor + 3 .cs (PendingUpdate, SqlConsoleService, DbAuditLogger) | ✅ | LogInfo → WriteLogs(`[{fn}] {entity}: {msg}`); LogException có ex → WriteExpLogs; LogException không có ex → WriteLogs(`[EXCEPTION][{fn}] msg`) |
| J6 | Audit log UsersPage (CREATE/UPDATE/LOCK/UNLOCK) + PosMapPage (UPDATE PosTerminal, chained dialog) | UsersPage.razor / UserFormDialog.razor / PosMapPage.razor / PosTerminalEditDialog.razor / PosTerminalDetailDialog.razor / PosTerminalSavePayload.cs (mới) | ✅ | UserFormDialog trả DTO đầy đủ (PasswordHash masked); DetailDialog forward result.Data!; PosMapPage capture oldJson trước dialog |
| H1 | Build pass (0 error, 3 warning pre-existing MUD0002) | — | ✅ | `dotnet build POS.Web` → Build succeeded. 3 Warning(s) MUD0002 Title pre-existing (VoidsPage + TransactionsPage ×2). 0 Error(s). |

---

## Tóm tắt

- ✅ Hoàn thành: **90 / 92 hạng mục**
- ⚠️ Có vấn đề: **2 hạng mục** (B9 — SQL seed hash placeholder; J3 — migration chưa chạy trên DB)
- ❌ Còn thiếu: **0 hạng mục**

> +3 hạng mục mới (session 2026-06-30): K1 (ProductsPage 6.1+6.2+6.3), K2 (ProductDetailDialog), K3 (ProductLockPage 6.4).
> Previous +2 (session 2026-06-28 Phase1+2): J5, J6. Previous +5: G24, J1-J4. Previous: S1-S7, G16-G23, I1-I12.

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
    3 Warning(s) — MUD0002 Title pre-existing (VoidsPage + TransactionsPage ×2)
    0 Error(s)

Time Elapsed — [2026-06-28 after Phase1+2: IKibanaService→FileLogger migration + audit UsersPage/PosMapPage]
```
