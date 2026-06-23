# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.

## [2026-06-23] RevenueHourlyPage — tối ưu data path + page cho quy mô 10M dòng ReportSaleDetail

**Layer:** POS.Infrastructure, POS.Web, POS.Common
**Loại:** Pattern mới + Refactor (tối ưu hiệu năng)

**Bối cảnh:** đánh giá `RevenueHourlyPage` + cách lấy dữ liệu qua `sp_ReportSaleByTime` khi `ReportSaleDetail` (bảng mart, worker rebuild mỗi 60s) lớn tới ~10M dòng.

**Thay đổi:**
- `src/POS.Common/Dtos/RptCentralSale/SaleByTimeKpiDto.cs` + `SaleByTimeSeriesDto.cs`: DTO map RS1 (KPI) / RS2 (series) của SP (đã có từ vòng tạo trang)
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs` + `IRptCentralSaleRepository.cs`: `GetSaleByTimeAsync` thêm **Redis cache** (key `MD:RptSaleByTime:*`, TTL 180s nếu range có hôm nay / 12h nếu quá khứ), tách cache KPI khỏi series, tham số `includeKpi`, timeout riêng 45s (thay 120s); inject thêm `IRedisService`
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor`: (1) guard `if (_loading) return;` + disable preset chips khi load; (2) `CancellationTokenSource` theo vòng đời + `IDisposable` + truyền `ct`; (3) hoãn auto-load khỏi prerender → `OnAfterRenderAsync(firstRender)`; (4) clamp 92 ngày khi xem all-stores; (6) line chart luôn hiện legend. Call DAY xin KPI, HOUR/WEEKDAY/compare `includeKpi:false`
- `docs/migrations/rpt_salebytime_perf.sql`: script chờ DBA — index `ReportSaleDetail(StoreNo, SaleDate)` INCLUDE cột đo + thêm `@IncludeKpi BIT` cho SP

**Pattern mới:**
- `Cache report query (SP) — TTL theo độ mới + bỏ result-set dư` → `.claude/skills/cache/SKILLS.md` (Pattern 4)
- `Report page an toàn ở quy mô lớn` (re-entrancy guard + CTS + defer-prerender + clamp all-stores) → `.claude/skills/web/reports.md`

**Lưu ý cho session sau:**
- Report SP nặng → BẮT BUỘC cache Redis TTL-theo-độ-mới + timeout riêng (KHÔNG dùng 120s chung), không cache vô thời hạn.
- Report page tự load → `if (_loading) return;` + CTS + auto-load trong `OnAfterRenderAsync(firstRender)` (KHÔNG trong `OnInitializedAsync` vì prerender chạy 2 lần). Disable cả **preset chips**, không chỉ nút.
- `CURRENT_STRUCTURE.md` KHÔNG track repo `RptCentralSale` (cả `GetDetailRevenueSales`/`GetSalesByCategory` cũng vắng) → đã bỏ qua Bước 3 để không tạo entry mồ côi.
- Đòn bẩy lớn nhất cho cold-cache lần đầu là **index DB** — còn chờ DBA chạy `docs/migrations/rpt_salebytime_perf.sql`.

---

## [2026-06-23] Chuẩn hóa DataTable → MudTable + tách SKILLS.md web + store combobox

**Layer:** POS.Web, POS.Infrastructure
**Loại:** Refactor + Pattern mới (đảo ngược pattern cũ)

**Thay đổi:**
- **Chuyển TOÀN BỘ DataTable từ `<table class="pos-table">` + `PosTableBase<T>` → `MudTable<T>`** (11 page): TransactionsPage, EosShiftsPage, UsersPage, AuditPage, RevenueHourlyPage, DataRawLogPage, LogsPage, DetailRevenuePage (ServerData), SqlConsolePage (cột động), PosMapPage (từ MudDataGrid); sửa header anti-pattern RevenuePage + PosMapPage
- `src/POS.Web/Components/Shared/PosTableBase.cs`: **ĐÃ XÓA** (MudTable có sort/paginate built-in)
- `src/POS.Infrastructure/Repositories/CentralMDRepository.cs` + `ICentralMDRepository.cs`: thêm `GetStoreListAsync()` — query bảng Store (No+Name), cache Redis `MD:StoreList` 12h
- Store combobox 4 page (TransactionsPage, DetailRevenuePage, SalesByCategoryPage, EosShiftsPage): `MudAutocomplete<StoreDto>` hiển thị "StoreNo – Name", tìm theo mã + tên (thay `MudAutocomplete<string>` chỉ có mã)
- `src/POS.Web/Components/Pages/Store/TransactionDetailDialog.razor`: cột "Mô tả" lấy `TenderTypeName` (thay `ReferenceNo`); table → Default size; nút Đóng → Outlined/Secondary
- **Tách `.claude/skills/web/SKILLS.md` (1136 → 613 dòng)** thành 6 file con: `filter-store.md`, `datatable.md`, `charts.md`, `reports.md`, `theming.md`, `deployment.md` + bảng index "Skill con — đọc khi cần"
- `CLAUDE.md` §10.B + `.claude/skills/web/SKILLS.md`: cập nhật chuẩn DataTable = MudTable

**Pattern mới:** `MudTable<T> — DataTable chuẩn` (client/server/dynamic/footer) → đã cập nhật `.claude/skills/web/datatable.md`. **THAY THẾ** pattern `PosTableBase<T>` cũ (changelog 2026-06-18).

**Lưu ý cho session sau:**
- DataTable mới **BẮT BUỘC** dùng `MudTable` (`MudTableSortLabel` + `MudTablePager`). KHÔNG còn `PosTableBase`/`pos-table` (trừ pivot report `rpt-pivot-table` vẫn raw table).
- Server-side paging: `MudTable @ref + ServerData` + `_table.ReloadServerData()` (KHÔNG gọi LoadDataAsync thủ công). Note cũ ở entry 2026-06-23 DetailRevenue (MudPagination Selected/SelectedChanged) đã lỗi thời.
- Store picker: dùng `MdRepo.GetStoreListAsync()` + `MudAutocomplete<StoreDto>`, KHÔNG dùng `GetStoreSetConfigAsync()` (không có Name). Xem `filter-store.md`.
- SKILLS.md web giờ là index — đọc file con tương ứng khi cần, tránh đọc cả file.

---

## [2026-06-23] Sidebar refactor — Ops tách 2 sub-group + bỏ icon cấp 3

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tách "Vận hành" Ops từ flat 6 links → 2 MudNavGroup con (Giám sát: Health/POS map/Alerts/Queues; Nhật ký: Logs/DataRaw Log); xóa `Icon="..."` khỏi toàn bộ 12 MudNavLink cấp 3 trong Store; thêm `_expandOpsMonitor` + `_expandOpsLog` + cập nhật `UpdateExpanded()`

**Pattern mới:** Sidebar 3-cấp — icon chỉ ở cấp 1 (section) và cấp 2 (sub-group), cấp 3 (leaf MudNavLink) không có icon → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:** Khi thêm trang Ops mới: nếu thuộc monitoring (health/status) → vào sub-group Giám sát; nếu thuộc logs/audit → vào sub-group Nhật ký. Leaf links KHÔNG được thêm `Icon=`.

---

## [2026-06-23] DetailRevenuePage — Báo cáo doanh thu chi tiết + menu sidebar refactor

**Layer:** POS.Web, POS.Infrastructure, POS.Common
**Loại:** Feature mới + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Tổ chức lại menu "Cửa hàng" thành 3 nhóm con (Vận hành, Giao dịch, Báo cáo); cập nhật `UpdateExpanded()` để auto-expand nhóm con
- `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor` *(tạo mới)*: Page báo cáo doanh thu chi tiết — 11 filters (từ/đến ngày, cửa hàng, tìm kiếm, loại đơn, hình thức bán, đối tác, VAT, đơn hàng gốc, thu ngân) + data table 21 cột + server-side pagination (50 rows/page) + Kibana + console logging
- `src/POS.Web/Components/Pages/Store/BusinessDayPage.razor` *(tạo mới)*: Stub — Ngày kinh doanh
- `src/POS.Web/Components/Pages/Store/ShiftSummaryPage.razor` *(tạo mới)*: Stub — Tổng kết ca
- `src/POS.Web/Components/Pages/Store/RefundsPage.razor` *(tạo mới)*: Stub — Hoàn trả
- `src/POS.Web/Components/Pages/Store/VoidsPage.razor` *(tạo mới)*: Stub — Hủy GD
- `src/POS.Web/Components/Pages/Store/RevenueHourlyPage.razor` *(tạo mới)*: Stub — Doanh thu theo giờ
- `src/POS.Web/Components/Pages/Store/PaymentBreakdownPage.razor` *(tạo mới)*: Stub — Phân tích thanh toán
- `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`: Thêm parameter normalization + detailed console logging (FromDate, ToDate, filters, page info, result count)
- `src/POS.Common/Dtos/RptCentralSale/DetailRevenueSalesDto.cs` *(đã tồn tại)*: 40 properties (ngày, giờ, số đơn, CH, POS, thu ngân, loại đơn, barcode, mã SP, tên SP, ĐVT, SL, đơn giá, giảm giá, thuế%, thuế VND, thành tiền, hình thức bán, đối tác, KM, coupon)
- `src/POS.Infrastructure/Repositories/Interfaces/IRptCentralSaleRepository.cs` *(đã tồn tại)*: Interface `GetDetailRevenueSalesAsync()` với 11 parameters + pageSize/pageNumber
- `src/POS.Infrastructure/DependencyInjection.cs` *(đã cập nhật)*: Line 41 — `AddScoped<IRptCentralSaleRepository, RptCentralSaleRepository>()`

**Pattern mới:**
1. **Server-side pagination với MudPagination** — dùng `Selected` + `SelectedChanged` event (KHÔNG `@bind-Selected`) để tránh conflict; phân biệt với TransactionsPage (client-side PosTableBase)
   - File: `src/POS.Web/Components/Pages/Store/DetailRevenuePage.razor`
2. **Menu sidebar nested MudNavGroup** — 3 cấp: parent → 3 sub-group → items; auto-expand theo URL pattern
   - File: `src/POS.Web/Components/Layout/MainLayout.razor`
3. **Tách DTO/Repository cho báo cáo (Rpt prefix)** — `RptCentralSale/` folder + `IRptCentralSaleRepository` riêng khỏi `ICentralSaleRepository` để tránh coupling với POS.Api
   - Files: `src/POS.Common/Dtos/RptCentralSale/`, `src/POS.Infrastructure/Repositories/RptCentralSaleRepository.cs`

**Lưu ý cho session sau:**
- DetailRevenuePage phục thuộc `[dbo].[RPT_GET_DETAIL_REVENUE_SALES_LIST]` SP trên RPOSCentralSales DB — nếu SP không trả data, kiểm tra: FromDate/ToDate format, StoreNo not empty, SalesType="-1" default; test SP trực tiếp với tham số tương ứng
- Menu sidebar UpdateExpanded() phải cover tất cả route mới (`/store/revenue-detail` đã được thêm vào dòng 156)
- Server-side pagination event (`SelectedChanged`) phải gọi `ReloadPageAsync(int newPage)` để gọi lại SP với page number mới (0-based)
- Responsive UI: Filter fields stack dọc trên mobile (xs), nút Tìm/Xóa full-width (`FullWidth="true"`)

---

## [2026-06-19] SAPController — migrate Internal Voucher APIs + business logic fixes

**Layer:** POS.Api, POS.Application, POS.Infrastructure, POS.Common
**Loại:** Feature + Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Api/Controllers/SAPController.cs`: Thêm `CheckReturnVoucher`, `UpdateReturnVoucher`; giữ `CheckVoucher`, `CreateNewVoucher`, `CreateReturnVoucher`, `RedeemCpnVch`
- `src/POS.Application/Interfaces/ISAPService.cs`: Thêm `UpdateReturnVoucherAsync`
- `src/POS.Application/Services/SAPService.cs`: Implement `UpdateReturnVoucherAsync`; fix `CheckVoucherAsync` (RDM→Return="1", EXP status + kiểm tra ngày `Expiry_Date < DateTime.Today`); fix `RedeemCpnVchAsync` (named param `ct: ct` sau khi signature thay đổi)
- `src/POS.Infrastructure/Repositories/Interfaces/ISAPVoucherRepository.cs`: Thêm optional `requiredVoucherType` vào `RedeemVouchersAsync`
- `src/POS.Infrastructure/Repositories/SAPVoucherRepository.cs`: Thêm check VoucherType trong transaction (UPDLOCK); thêm amount validation (0 ≤ AmountRedeem ≤ faceValue); UPDATE per-row với `AmountUsed` + `OrderUsed`
- `src/POS.Common/Dtos/Vouchers/VoucherStatusResponseDto.cs`: Thêm `AmountUsed decimal?`, `OrderUsed string?`
- `src/POS.Common/Dtos/SAP/SAPDto.cs`: Thêm `VoucherUpdateRequest`
- `src/POS.Common/Validation/StringRangeAttribute.cs`: Tạo mới — custom whitelist validation
- `docs/migrations/alter_internal_voucher_add_amountused_orderused.sql`: DDL thêm 2 cột vào `Internal_Voucher`

**Pattern mới:**
- Optional VoucherType filter trong UPDLOCK transaction → `.claude/skills/api/SKILLS.md`
- Named CancellationToken khi thêm optional param vào giữa signature → `.claude/skills/api/SKILLS.md`

**Lưu ý cho session sau:**
- `Internal_Voucher` cần chạy DDL migration trước khi deploy: `ALTER TABLE ADD AmountUsed DECIMAL(18,2) NULL, OrderUsed NVARCHAR(50) NULL`
- `UpdateReturnVoucher` chỉ cho phép voucher `VoucherType = "BNMH"` (do `CreateReturnVoucher` tạo ra) — check diễn ra trong transaction UPDLOCK
- Khi thêm optional param vào giữa signature → scan callers và thêm `ct: ct` (named) cho CancellationToken

---

## [2026-06-19] RevenuePage — Y-axis auto-scale theo dữ liệu thực tế

**Layer:** POS.Web
**Loại:** Bug fix

**Thay đổi:**
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Thêm `CalcYMax` + `CalcYTick` helpers; set `YAxisSuggestedMax` + `YAxisTicks` trên cả 2 `BarChartOptions` sau khi load data

**Pattern mới:** Y-axis auto-scale cho MudBlazor v9 Bar/Line chart → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
- `BarChartOptions.YAxisTicks` mặc định = **20** là *khoảng cách giữa tick* (không phải số lượng) → luôn set kèm `YAxisSuggestedMax` khi data là số nhỏ
- `YAxisSuggestedMax` (double?) là gợi ý — MudBlazor tự mở rộng nếu data vượt quá, không bao giờ clip data

---

## [2026-06-19] DataRawJson audit log + tách POS.Worker thành project độc lập

**Layer:** POS.Infrastructure, POS.Api
**Loại:** Feature + Refactor + Pattern mới

**Thay đổi:**
- `src/POS.Infrastructure/Repositories/Interfaces/ICentralSaleRepository.cs`: Thêm tham số `transactionId` vào `InInsertToTableByJson()`
- `src/POS.Infrastructure/Repositories/CentralSaleRepository.cs`: Refactor `InInsertToTableByJson` dùng try/finally; thêm `InsertDataRawJsonAsync()` private (log vào bảng `DataRawJson`, dùng `directConnectionFactory`); xóa 3 lời gọi `InsertInterfaceErrorAsync` trùng lặp
- `src/POS.Infrastructure/AppServices/KafkaAppService.cs`: Truyền thêm `message.TransactionId`
- `src/POS.Infrastructure/Workers/PosSalesConsumerWorker.cs`: Truyền thêm `msg.TransactionId`
- `src/POS.Infrastructure/Logging/SerilogConfiguration.cs`: Thêm overload `HostApplicationBuilder` + refactor helper `ConfigureSerilogCore` chung — tránh lặp code cấu hình ES/Console
- `src/POS.Infrastructure/DependencyInjection.cs`: Cập nhật comment worker registration
- `src/POS.Api/Program.cs`: Xóa `AddHostedService<PosSalesConsumerWorker>()` — worker đã tách ra
- `src/POS.Worker/` *(tạo mới)*: Project Worker Service — `POS.Worker.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Production.json`
- `Dockerfile.worker` *(tạo mới)*: Multi-stage build dùng `dotnet/runtime:10.0` (không phải aspnet)
- `docker-compose.yml`: Thêm service `worker` (container `pos_worker`, 512MB, `DOTNET_ENVIRONMENT=Production`)
- `POS.slnx`: Thêm `src/POS.Worker/POS.Worker.csproj` vào solution

**Pattern mới:** 
1. `Audit log với try/finally` — `InsertDataRawJsonAsync` pattern trong Repository
2. `POS.Worker project` — Worker Service độc lập với Docker container riêng, hỗ trợ nhiều worker song song qua `AddHostedService<T>()`
3. `SerilogConfiguration dual overload` — cùng 1 extension dùng được cho cả `WebApplicationBuilder` và `HostApplicationBuilder`

**Lưu ý cho session sau:**
- `POS.Worker/Program.cs` KHÔNG gọi `AddApplication()` — worker chỉ cần `AddInfrastructure()` đủ để lấy `ICentralSaleRepository`
- Thêm worker nghiệp vụ mới: chỉ cần thêm class kế thừa `BackgroundService` vào `POS.Infrastructure/Workers/` rồi đăng ký `AddHostedService<T>()` trong `POS.Worker/Program.cs` — không cần project mới
- `DataRawJson` table phải tồn tại trong RPOSCentralSales DB trước khi deploy

---

## [2026-06-19] HealthPage responsive fix + Responsive UI standard vào SKILLS.md

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Components/Pages/Ops/HealthPage.razor`: Fix header — `MudStack Row Justify.SpaceBetween` → `div.pos-page-header` (Case B: title + group controls); button thêm `Style="align-self:center; white-space:nowrap"` để không bị stretch theo chiều cao MudSelect+Label; chip container `MudStack Row` → `div.d-flex gap-1 flex-wrap`
- `.claude/skills/web/SKILLS.md`: Thêm section **"Responsive UI — BẮT BUỘC"** — bảng so sánh sai/đúng cho 6 tình huống phổ biến, code mẫu 2 case pos-page-header (A: title+button đơn; B: title+group controls), 4 anti-pattern responsive mới, 1 checklist item nhắc đọc CLAUDE.md §10.G

**Pattern mới:** `pos-page-header Case B — title + group controls` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Khi `MudButton` nằm trong `MudStack Row` cạnh `MudSelect` có `Label`, button sẽ stretch cao bất thường (flex align-items: stretch) — luôn thêm `Style="align-self:center"` vào button để cố định chiều cao.
Responsive UI standard đã có trong cả CLAUDE.md §10 (chi tiết) và SKILLS.md (tóm tắt tra nhanh).

---

## [2026-06-18] Responsive UI Phase 3 — 5 pages/components theo chuẩn mobile

**Layer:** POS.Web
**Loại:** Refactor

**Thay đổi:**
- `src/POS.Web/Components/Layout/MainLayout.razor`: Drawer responsive init — `IBrowserViewportService.GetCurrentBreakpointAsync()` trong `OnAfterRenderAsync(firstRender)` → drawer đóng trên mobile, mở sẵn trên desktop (≥ md); đổi `IDisposable` → `IAsyncDisposable`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Header `MudStack Row` → `div.pos-page-header` + `pos-page-header-title` + `pos-page-header-btn`; search inner div thêm `flex-wrap`; `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`; summary text `&nbsp;|&nbsp;` → `d-flex flex-wrap gap-3` với 3 `MudText` riêng
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: `MudPaper` table thêm `Style="overflow-x:auto"`
- `src/POS.Web/Components/Pages/Store/RevenuePage.razor`: Chip filter container thêm `flex-wrap`

**Lưu ý cho session sau:**
`IBrowserViewportService` inject được trong Blazor Server component — `Breakpoint.Md or Breakpoint.Lg or Breakpoint.Xl or Breakpoint.Xxl` thay vì `>= Breakpoint.Md` để tránh enum so sánh với range values.
Khi sửa `IDisposable` → `IAsyncDisposable`: đổi `Dispose()` → `async ValueTask DisposeAsync()` và `@implements IDisposable` → `@implements IAsyncDisposable`.

---

## [2026-06-18] DataTable standard — PosTableBase\<T\> + EosShiftsPage + sidebar accordion

**Layer:** POS.Web
**Loại:** Feature + Pattern mới + Refactor

**Thay đổi:**
- `src/POS.Web/Components/Shared/PosTableBase.cs`: Tạo mới — abstract base class cung cấp sort (single-column), phân trang (PageSize=10), `FormatVND`, `PagedItems`, `TotalFiltered`, `PageCount`
- `src/POS.Web/wwwroot/app.css`: Thêm `.pos-table*` CSS standard (header #EEF1F7/#1A2B45, sort icon ⇅↑↓) + active NavLink highlight (rgba 14% + border-left #3A6FCC)
- `src/POS.Web/Components/Layout/MainLayout.razor`: Sidebar accordion — `NavigationManager.LocationChanged` + `@bind-Expanded` + `IDisposable`; thêm EosShifts nav link
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`: Tạo mới — Kết thúc ca bán hàng (filter ngày/store/trạng thái + KPI cards + pos-table); refactored to `@inherits PosTableBase<EosShiftDto>`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`: Migrated từ `MudDataGrid` → `@inherits PosTableBase<TransactionListDto>` + `pos-table`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`: Migrated từ `MudDataGrid + QuickFilter Func<>` → `@inherits PosTableBase<DashboardUser>` + LINQ search với `SearchText` property tự reset `_page = 1`

**Pattern mới:** `PosTableBase<T> — DataTable chuẩn` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Mọi page DataTable mới BẮT BUỘC dùng `@inherits PosTableBase<T>` + `<table class="pos-table">` — KHÔNG dùng `MudDataGrid`.
Khi search filter cần reset page, dùng property C# (`get`/`set { _field = value; _page = 1; }`) thay vì `_searchText` field trực tiếp.

---

## [2026-06-17 20:00] Áp dụng hệ màu DataFlip — PosTheme + CSS variables

**Layer:** POS.Web
**Loại:** Feature + Pattern mới

**Thay đổi:**
- `src/POS.Web/Theme/PosTheme.cs`: Tạo mới — static `MudTheme` với navy primary (#2051A3), sidebar/appbar navy dark (#1B3A5C), teal accent (#1EAA90), semantic status colors, BorderRadius=8px, Button.TextTransform=none
- `src/POS.Web/Components/_Imports.razor`: Thêm `@using POS.Web.Theme` (global cho mọi Layout)
- `src/POS.Web/Components/Layout/MainLayout.razor`: `<MudThemeProvider Theme="@PosTheme.Default"/>`
- `src/POS.Web/Components/Layout/EmptyLayout.razor`: Theme param + bỏ `background:#f0f2f5` hardcode → `var(--mud-palette-background)`
- `src/POS.Web/Components/Layout/MainLayout.razor.css`: Bỏ gradient navy→tím (legacy Blazor template), dùng solid `#1B3A5C`
- `src/POS.Web/Components/Layout/ReconnectModal.razor.css`: Button/spinner dùng `var(--mud-palette-primary)` thay hardcode `#6b9ed2`, `#0087ff`
- `src/POS.Web/wwwroot/app.css`: 28 CSS variables `--pos-*`, scrollbar, `.pos-delta-up/down` utility
- `docs/style-guide.html`: Tạo mới — tài liệu tham chiếu màu (swatches + 6 component mẫu)

**Pattern mới:** `PosTheme.cs — custom MudBlazor Theme` → đã cập nhật `.claude/skills/web/SKILLS.md`

**Lưu ý cho session sau:**
Trong MudBlazor v9, `Typography.FontWeight` và `LineHeight` là **string** ("600", "1.6"), không phải `int`/`double` — sẽ gây compile error nếu dùng sai type.
`WarningContrastText` phải là màu tối vì #F39C12 (amber) contrast với trắng chỉ 2.4:1 (fail WCAG AA).

---

## [2026-06-17 17:00] Fix deployment POS.Web — blazor.web.js 404 + nginx setup

**Layer:** POS.Web
**Loại:** Bug fix + Pattern mới

**Thay đổi:**
- `src/POS.Web/Program.cs`: Thêm middleware rewrite `Host: localhost` cho `/_framework/` + **`app.UseRouting()` tường minh** sau middleware (fix root cause: automatic UseRouting chạy trước mọi middleware trong WebApplication .NET 10)
- `src/POS.Web/Components/App.razor`: Google Fonts load non-blocking (`rel="preload"` + `onload`)
- `src/POS.Web/Dockerfile`: `mkdir -p /home/app/.aspnet/DataProtection-Keys && chown -R app:app` TRƯỚC `USER $APP_UID` — fix `CryptographicException` khi Docker volume owned bởi root
- `publish/POS.Web/`: Build output self-contained linux-x64 cho nginx deployment

**Patterns mới:** 4 patterns → đã cập nhật `.claude/skills/web/SKILLS.md`:
- `Explicit UseRouting() để middleware chạy TRƯỚC routing`
- `Fix _framework/blazor.web.js 404 từ external IP`
- `nginx config cho Blazor Server`
- `DataProtection keys trong Docker`

**Lưu ý cho session sau:**
Trong .NET 9/10 `WebApplication`, `UseRouting()` tự động chèn vào ĐẦU pipeline — BẮT BUỘC gọi
`app.UseRouting()` tường minh sau bất kỳ middleware nào cần chạy trước routing.
Sau khi deploy nginx, test: `curl -sv -H "Host: <ip>:5001" http://localhost:8080/_framework/blazor.web.js` phải trả 200.

---
