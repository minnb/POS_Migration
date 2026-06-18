# POS Solution — Changelog
> Ghi lại các task đã hoàn thành và pattern mới được thiết lập.
> Đọc file này khi bắt đầu session mới để nắm context.

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
