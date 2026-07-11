---
name: web-skills-index
description: Index + quy tắc nền tảng cho Blazor Server POS.Web (MudBlazor 9.5.0) — roles/policy, page pattern, MudMessageBox, checklist tạo page mới. Đọc khi viết/sửa bất kỳ page/component/service nào trong src/POS.Web/; trỏ tới ~25 file skill con theo tình huống cụ thể.
---

# Skill: Blazor Server Web Dashboard (POS.Web)

> **Áp dụng khi:** viết hoặc chỉnh sửa bất kỳ thành phần nào trong
> `src/POS.Web/` — page, component, layout, service, hoặc auth layer.
> Bao gồm: tạo page mới, thêm nav link, chỉnh auth, sử dụng MudBlazor, inject service.

---

## Skill con — đọc khi cần (tránh đọc hết file này)

> File này chỉ giữ quy tắc nền tảng + index. Pattern chi tiết tách ra file riêng — chỉ đọc đúng file khi gặp tình huống.

| File | Đọc khi |
|---|---|
| **`.claude/skills/web/01-architecture-and-logic.md`** | **Bắt đầu bất kỳ page/component/service nào — bản "hiến pháp" rút gọn LUẬT BẮT BUỘC: render mode, auth/policy, lifecycle 3-state, cấm HttpClient→Api/raw SQL, anti-crash MudAutocomplete, audit CRUD** |
| **`.claude/skills/web/02-ui-ux-and-components.md`** | **Viết/sửa markup page/component — LUẬT BẮT BUỘC: MudGrid/MudPaper layout, cấm inline CSS, MudTable/KPI card thay text summary, Dialog trả DTO đầy đủ, responsive qua IBrowserViewportService, MudBlazor component mapping** |
| **`.claude/skills/web/03-integration-and-performance.md`** | **Gọi data/chia sẻ state/xử lý sự kiện async/JS Interop/tối ưu hiệu năng — LUẬT BẮT BUỘC: cấm HttpClient, Scoped Service/CascadingParameter, try-catch+Snackbar+Kibana, CancellationToken, @key, IAsyncDisposable; kèm pattern load nhiều nguồn độc lập, modal lazy-load tab, truyền row object vào dialog** |
| **`.claude/skills/web/04-datatable-and-lists.md`** | **Tạo/sửa danh sách dữ liệu, bảng biểu, filter panel gắn kèm bảng — LUẬT BẮT BUỘC: MudTable mặc định, Elevation="2", NoRecordsContent, debounce search, ServerData+CancellationToken, row actions, chuẩn đặt tên cột + format DateTime** |
| `.claude/skills/web/form-input.md` | Thiết kế form nhập liệu cơ bản (MudCard section + MudGrid + validation trực quan); Placeholder vs HelperText |
| `.claude/skills/web/form-input-special-modes.md` | Chế độ CHỈ XEM (bản ghi khóa vĩnh viễn) + field ngoại lệ có nút Lưu điều kiện; MudTimePicker; MudSelect multi-selection; format số tiền khi nhập |
| `.claude/skills/web/filter-store.md` | Thêm combobox lọc cửa hàng vào page |
| `.claude/skills/web/datatable.md` | Tạo bảng dữ liệu (MudTable) — client/server/dynamic, code mẫu đầy đủ |
| `.claude/skills/web/charts.md` | Thêm biểu đồ Line/Bar (MudBlazor v9) |
| `.claude/skills/web/reports.md` | Trang báo cáo pivot / xuất PDF |
| `.claude/skills/web/theming.md` | Sửa PosTheme.cs / đổi màu-typography toàn app |
| `.claude/skills/web/sidebar-nav.md` | Thêm nhóm/leaf mới vào sidebar (logic `UpdateExpanded`) hoặc breadcrumb động |
| `.claude/skills/web/security-hardening.md` | Security headers/CSP, SQL Console hardening, PIN/step-up gate, đọc/tải file server an toàn |
| `.claude/skills/web/trigger-api-task-via-di.md` | Page POS.Web cần chạy 1 tác vụ vốn thuộc POS.Api (qua DI, không HTTP) |
| `.claude/skills/web/bulk-import-excel.md` | Page nhập liệu hàng loạt từ Excel + preview validate |
| `.claude/skills/web/image-upload.md` | Upload ảnh lưu base64 cho 1 entity |
| `.claude/skills/web/syntax-highlight-textarea.md` | Tô màu cú pháp SQL/code cho 1 ô textarea lớn |
| `.claude/skills/web/deployment.md` | Deploy production (Docker / nginx) |
| **`.claude/skills/web/audit-logging.md`** | **Tạo/sửa page có thao tác Create/Update/Delete** |
| **`.claude/skills/web/ui-polish-standard.md`** | **Làm đẹp/đồng bộ UI trang đã có — chỉ sửa markup, giữ `@code`** |

---

## Quy tắc cốt lõi

**5 nguyên tắc không được vi phạm:**

1. Toàn bộ UI dùng **MudBlazor** — không dùng raw HTML/CSS thuần (inline style nhỏ được chấp nhận)
2. Serialization dùng **Newtonsoft.Json** (`JsonConvert.*`) — **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json`
3. Mọi page phải có `@attribute [Authorize(Policy = ...)]` và `@rendermode InteractiveServer`
4. Mọi page có thao tác **Create / Update / Delete** — **BẮT BUỘC** đọc và áp dụng `.claude/skills/web/audit-logging.md`
5. **LUẬT THÉP** — mọi page/component UI mới **BẮT BUỘC** tuân thủ `.claude/rules/mudblazor-flat-ui.md`
   (mapping component + khuôn mẫu KPI card mục 11 + checklist typography mục 11.1) và
   `.claude/skills/web/ui-polish-standard.md` — xem `.claude/rules/blazor-web-app.md` mục 0 "LUẬT THÉP". KHÔNG tự viết
   `MudGrid`/`MudPaper` tùy ý cho KPI card khi đã có khuôn mẫu chuẩn.

---

## Roles và Policy mapping

| Role constant | String value | Policy constant | Dùng cho |
|---|---|---|---|
| `WebRoles.StoreOperator` | `"StoreOperator"` | `WebPolicies.StoreAndAbove` | `Pages/Store/*` |
| `WebRoles.BackOffice` | `"BackOffice"` | `WebPolicies.BackOfficeAndAbove` | `Pages/Catalog/*`, `Pages/Promotion/*` |
| `WebRoles.ITOps` | `"ITOps"` | `WebPolicies.OpsAndAbove` | `Pages/Ops/*` |
| `WebRoles.SystemAdmin` | `"SystemAdmin"` | `WebPolicies.AdminOnly` | `Pages/Admin/*` |

**Coverage của từng policy:**
- `StoreAndAbove` = StoreOperator + BackOffice + ITOps + SystemAdmin (cả 4)
- `BackOfficeAndAbove` = BackOffice + ITOps + SystemAdmin
- `OpsAndAbove` = ITOps + SystemAdmin
- `AdminOnly` = chỉ SystemAdmin

> Nguồn: `src/POS.Web/Auth/WebRoles.cs`

---

## Page component pattern + Auth flow + Services inject — nguồn canonical

> 3 nội dung này (template page 3-state, bridge-token auth flow, bảng đầy đủ service inject được)
> đã có **bản đầy đủ và canonical tại `.claude/rules/blazor-web-app.md`** (§5 "Template Page
> Component chuẩn", §2 "Kiến trúc Auth", §4 "Services inject được"). Không lặp lại ở đây — đọc
> trực tiếp file đó.

## Row-level filter cho StoreOperator

StoreOperator chỉ được xem data của store được gán trong `store_codes` claim.
ITOps và SystemAdmin xem tất cả (`_userStoreCodes` rỗng = không giới hạn).

```csharp
// Lấy store codes từ claims sau OnInitializedAsync
var json = state.User.FindFirst("store_codes")?.Value;
var _userStoreCodes = string.IsNullOrEmpty(json)
    ? []  // empty = ITOps/Admin → xem tất cả
    : JsonConvert.DeserializeObject<List<string>>(json) ?? [];

// Dùng khi gọi repository — truyền null nếu không giới hạn
var data = await SaleRepo.GetSalesAsync(
    storeCodes: _userStoreCodes.Count > 0 ? _userStoreCodes : null,
    startDate: _startDate,
    endDate: _endDate);
```

> **Lưu ý:** Không áp dụng row-level filter với `OpsAndAbove` hoặc `AdminOnly` — các role đó luôn xem tất cả.

---

## DataTable chuẩn — `MudTable<T>`

> **Luật bắt buộc: `.claude/skills/web/04-datatable-and-lists.md`** (Elevation, filter panel,
> ServerData/CancellationToken, row actions, chuẩn cột/DateTime). **Pattern code đầy đủ
> (client/server/cột động/sort/footer tổng): `.claude/skills/web/datatable.md`.** BẮT BUỘC dùng
> MudBlazor `<MudTable>` — KHÔNG tự viết HTML `<table>` hay base class (ngoại lệ: Pivot report —
> xem `reports.md`).

---

## Store Selector — Dual Mode (StoreOperator vs Manager/Admin)

> **Chi tiết đầy đủ (fields, markup, SearchFunc, checklist, anti-pattern, biến thể multi-add
> picker): `.claude/skills/web/filter-store.md`** — đọc file này trước khi thêm bộ lọc cửa hàng vào
> page mới. Áp dụng bắt buộc cho mọi page `StoreAndAbove` có filter theo cửa hàng.

| | StoreOperator | ITOps / Admin |
|---|---|---|
| UI | `MudTextField ReadOnly` hiển thị `"2018 – Cửa hàng demo"` | `MudAutocomplete T="StoreDto"` tìm theo mã + tên |
| Binding | `_filterStoreNo` (locked) | `_selectedStore: StoreDto?` |
| Nguồn data | `MdRepo.GetStoreListAsync()` (cache 12h) | Như trái |

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/Transactions/TransactionsPage.razor`

---

## Báo cáo — Pivot table & Report page layout

> **Chi tiết đầy đủ: `.claude/skills/web/reports.md`** — đọc khi tạo trang báo cáo pivot hoặc trang xuất PDF.

- **Pivot report** (hàng × cột-ngày động): dùng `<table class="pos-table rpt-pivot-table">` — ngoại lệ có chủ đích so với MudTable.
- **Report page layout**: header chuẩn (action bar PDF + user info + title + filter summary) cho trang xuất báo cáo.

> Ví dụ: `src/POS.Web/Components/Pages/Store/Reports/SalesByCategoryPage.razor`

---

## Shared components có sẵn

| Component / Class | File | Dùng cho |
|---|---|---|
| DataTable | — dùng `MudTable<T>` built-in (xem `datatable.md`) | Bảng dữ liệu sort/paginate (KHÔNG còn base class) |
| KPI card | Chưa có component riêng — dùng khuôn mẫu `MudPaper` + `.pos-kpi-value`/`.pos-kpi-label` ở `.claude/rules/mudblazor-flat-ui.md` mục 11 | KPI/summary card — **BẮT BUỘC** theo khuôn mẫu này, không tự viết |
| `PosDeltaBadge` | `Components/Shared/PosDeltaBadge.razor` (đăng ký `@using` toàn cục trong `_Imports.razor`) | Trend/delta badge (%) trong KPI card — dùng `.pos-delta-up`/`.pos-delta-down` |
| Status badge | Chưa có component riêng — dùng `<span class="pos-status-chip pos-status-{success,error,warning,info}">Label</span>` (helper trả `(string CssClass, string Label)`, xem `.claude/rules/mudblazor-flat-ui.md` §4a) | Status/loại/hình thức tĩnh trong `MudTable`/dialog — **CHUẨN MẶC ĐỊNH** (2026-07-09), KHÔNG dùng `MudChip` trừ khi cần tương tác (multi-select/closable/trong filter) |

> Ví dụ KPI card đã chuẩn hóa (2026-07-08): `src/POS.Web/Components/Pages/Store/Reports/RevenueByStorePage.razor`
> (không icon), `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor` (có icon minh họa),
> `src/POS.Web/Components/Pages/Store/Reports/RevenueHourlyPage.razor` (có `PosDeltaBadge`).

---

## Logging pattern trong component

```csharp
@inject IKibanaService KibanaService    @* BẮT BUỘC cho page có data *@
@inject IFileLogHelper FileLogHelper    @* tuỳ chọn — fallback khi Kibana unavailable *@

// Trong LoadDataAsync — log khi load thành công
KibanaService.LogInfo("PageName.LoadData",
    _userStoreCodes.FirstOrDefault() ?? "all",
    $"Loaded {count} items");

// Trong catch — log exception
KibanaService.LogException("PageName.MethodName", "", 0, "", ex.Message);
```

> **KHÔNG** log thông tin nhạy cảm: card number, password, token, PII của khách hàng.

---

## Charts (Line / Bar) — MudBlazor v9

> **Chi tiết đầy đủ: `.claude/skills/web/charts.md`** — đọc trước khi thêm biểu đồ.
> MudBlazor 9.5.0 đổi hoàn toàn cú pháp chart: dùng `<Line T="double">` / `<Bar T="double">` (KHÔNG `<MudChart>`), data `ChartData<double>`, options `LineChartOptions`/`BarChartOptions`. Bao gồm pattern Y-axis auto-scale.

---

## Responsive UI — BẮT BUỘC (mobile + tablet + PC)

> **Chi tiết đầy đủ: `.claude/rules/blazor-web-app.md` §10** + `02-ui-ux-and-components.md` §5 —
> đọc trước khi tạo hoặc sửa bất kỳ page nào. Áp dụng cho mọi viewport: xs (<600px), sm (600–959px), md+ (960px+).

| Tình huống | Sai | Đúng |
|---|---|---|
| Header: title + button | `MudStack Row Justify.SpaceBetween` | `div.pos-page-header` + `pos-page-header-title` + `pos-page-header-btn` |
| Header: title + (select + button) ghép cặp | `MudStack Row Justify.SpaceBetween` | `div.pos-page-header` + `div.d-flex align-center gap-2` + `Style="align-self:center"` trên button |
| DataTable scroll ngang trên mobile | `MudTable` không cho scroll | `<MudTable HorizontalScrollbar="true">` (pivot/raw table thì wrapper `overflow-x:auto`) |
| Chip container | `d-flex gap-2` | `d-flex gap-2 flex-wrap` |
| Summary text nhiều phần | `&nbsp;\|&nbsp;` separator | `d-flex flex-wrap gap-3` + nhiều `MudText` riêng |
| Sidebar drawer init | `_drawerOpen = true` | `IBrowserViewportService.GetCurrentBreakpointAsync()` trong `OnAfterRenderAsync(firstRender)` |

### pos-page-header — pattern header chuẩn

```razor
@* Case A: title + 1 button đơn lẻ *@
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton ... Class="pos-page-header-btn">Thêm</MudButton>
</div>

@* Case B: title + group controls (select + button ghép cặp) *@
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title">...</MudText>
    <div class="d-flex align-center gap-2">
        <MudSelect .../>
        <MudButton ... Style="align-self:center; white-space:nowrap">...</MudButton>
    </div>
</div>
```

**Desktop (≥600px):** title bên trái, controls bên phải — cùng hàng.
**Mobile (xs <600px):**
- Case A: title full-width hàng 1, button full-width hàng 2 (`pos-page-header-btn`)
- Case B: title full-width hàng 1, cả group (Select + Button) xuống hàng 2 cùng nhau

> CSS `pos-page-header` + `pos-page-header-title` + `pos-page-header-btn` đã có trong `wwwroot/app.css`.
> Ví dụ: `src/POS.Web/Components/Pages/Admin/UsersPage.razor` (Case A), `src/POS.Web/Components/Pages/Ops/HealthPage.razor` (Case B)

---

## Tổ chức thư mục Pages — BẮT BUỘC

```
src/POS.Web/Components/Pages/
├── Store/
│   ├── Dialogs/        ← dialog/detail components (không có @page)
│   ├── Operations/     ← Vận hành: BusinessDay, EosShifts, ShiftSummary
│   ├── Transactions/   ← Giao dịch: Transactions, Refunds, Voids
│   └── Reports/        ← Báo cáo: Revenue, RevenueHourly, DetailRevenue, ...
├── Ops/
│   ├── Dialogs/        ← dialog/detail components (không có @page)
│   └── *.razor         ← các page Ops trực tiếp
└── Admin/
    ├── Dialogs/        ← dialog/detail components (không có @page)
    └── *.razor         ← các page Admin trực tiếp
```

**Quy tắc đặt file:**
- Page điều hướng (có `@page`) trong `Store/` → đặt vào sub-folder đúng nhóm nav (Operations/Transactions/Reports)
- Dialog, detail panel (KHÔNG có `@page`) → đặt vào `Dialogs/` của section tương ứng
- `Ops/` và `Admin/` chưa cần sub-folder (số page ít) — thêm khi > ~15 page
- File page trong sub-folder cần `@namespace POS.Web.Components.Pages.{Section}` để giữ type identity khi dialog open bằng `ShowAsync<T>()`

**Thứ tự directive BẮT BUỘC khi có `@namespace`:**
```razor
@page "/store/ten-trang"                     ← PHẢI là dòng đầu tiên
@namespace POS.Web.Components.Pages.Store    ← PHẢI đứng SAU @page
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]
@rendermode InteractiveServer
```
> **Lý do:** Blazor Web App dùng `MapRazorComponents<App>()` — `@page` phải ở đầu file để endpoint routing nhận dạng được component là routable. Đặt `@namespace` trước `@page` khiến route không được đăng ký → page không truy cập được.

---

## Checklist khi tạo page mới

- [ ] Đặt file đúng **sub-folder**: Store/Operations, Store/Transactions, Store/Reports, Ops/, Admin/ (theo nhóm nav)
- [ ] Dialog/detail component → đặt vào `{Section}/Dialogs/` (không có `@page`)
- [ ] `@page "/section/kebab-case"` — **dòng đầu tiên của file** (trước cả `@namespace`)
- [ ] `@namespace POS.Web.Components.Pages.{Section}` — thêm khi đặt vào sub-folder, đứng SAU `@page`
- [ ] `@attribute [Authorize(Policy = WebPolicies.XXX)]` — đúng với section
- [ ] `@rendermode InteractiveServer` — bắt buộc
- [ ] `@inject IKibanaService KibanaService` — để log
- [ ] `[CascadingParameter] Task<AuthenticationState> AuthState` — để lấy user info
- [ ] Parse `_userStoreCodes` từ `store_codes` claim trong `OnInitializedAsync`
- [ ] `_loading = true` khi bắt đầu, `finally { _loading = false; }` khi kết thúc
- [ ] Loading state trong markup: `@if (_loading) { <MudProgressLinear .../> }`
- [ ] Error state: `else if (_errorMsg != null) { <MudAlert .../> }`
- [ ] Empty state: `else if (_isEmpty) { <MudAlert Severity.Info .../> }`
- [ ] Row-level filter nếu policy là `StoreAndAbove` — pass `_userStoreCodes` vào repo call
- [ ] Thêm `<MudNavLink>` vào đúng `<MudNavGroup>` trong `MainLayout.razor` (wrap `<AuthorizeView>`) — xem `sidebar-nav.md`
- [ ] **Responsive checklist** — xem `.claude/rules/blazor-web-app.md` §10.G: header dùng `pos-page-header`, DataTable dùng `MudTable HorizontalScrollbar="true"`, chip container có `flex-wrap`, không dùng `MudStack Row Justify.SpaceBetween` cho layout title+controls

---

## Theming — Custom MudBlazor Theme

> **Chi tiết đầy đủ: `.claude/skills/web/theming.md`** + `.claude/rules/mudblazor-flat-ui.md`
> (nguồn sự thật cho theme/UI pattern — đọc khi cần đổi màu/typography toàn app, hoặc khi tạo
> page/component mới cần biết chuẩn hiện hành).

---

## Production deployment

> **Chi tiết đầy đủ: `.claude/skills/web/deployment.md`** — đọc khi deploy production (Docker / nginx / self-contained).
> Bao gồm: explicit `UseRouting()`, fix `_framework/blazor.web.js` 404 từ external IP, nginx config WebSocket, DataProtection keys trong Docker.

---

## Ví dụ tham chiếu

| Loại | File |
|---|---|
| Page Store mẫu (chart + KPI) | `src/POS.Web/Components/Pages/Store/Reports/RevenuePage.razor` |
| Page Store mẫu (filter + table + dialog) | `src/POS.Web/Components/Pages/Store/Transactions/TransactionsPage.razor` |
| Page Store mẫu (operations) | `src/POS.Web/Components/Pages/Store/Operations/EosShiftsPage.razor` |
| Dialog mẫu (Store section) | `src/POS.Web/Components/Pages/Store/Dialogs/VoidDetailDialog.razor` |
| Page Ops mẫu (health check) | `src/POS.Web/Components/Pages/Ops/HealthPage.razor` |
| Page Admin mẫu (user management) | `src/POS.Web/Components/Pages/Admin/UsersPage.razor` |
| Layout chính + sidebar nav | `src/POS.Web/Components/Layout/MainLayout.razor` |
| Login (bridge token pattern) | `src/POS.Web/Components/Pages/Login.razor` |
| Auth service (BCrypt + JSON) | `src/POS.Web/Auth/WebUserService.cs` |
| DI registration | `src/POS.Web/Program.cs` |
| Roles + Policies constants | `src/POS.Web/Auth/WebRoles.cs` |

---

## Pattern: `MudMessageBox @ref` — confirm dialog đơn giản
> Áp dụng khi: cần hỏi "Bạn có chắc không?" trước lock/unlock/delete/approve/retry mà không cần
> form — thay thế `IDialogService.ShowMessageBox` (không tồn tại trong v9).
>
> **BẮT BUỘC dùng cách khai báo `@ref` này — KHÔNG dùng
> `DialogService.ShowAsync<MudMessageBox>(title, parameters, options)`.** Cách gọi qua
> `ShowAsync<MudMessageBox>` render Yes/No button bằng markup MẶC ĐỊNH của MudBlazor (bên trong
> thư viện, không nằm trong source của dự án) — **không có `<YesButton>` slot để can thiệp**, nên
> không thể chọn đúng Variant/Color theo bản chất hành động Yes (xem bảng Button convention ở
> `.claude/rules/mudblazor-flat-ui.md` §3). Đây là lỗi có thật đã xảy ra ở 8 page (BusinessDayPage, VouchersPage,
> SpecialComboPage, PromotionSetupPage, PosDataSetupPage, DataRawLogPage, UsersPage, BankPosPage)
> — phát hiện vì `grep MudButton.*Variant.Filled` không bắt được (nút đó không tồn tại trong
> markup của dự án, MudBlazor tự render). Luôn dùng cách khai báo `@ref` bên dưới để nút Yes nằm
> trong markup của page, chọn đúng Variant/Color theo bảng dưới. Xem thêm quy trình đầy đủ cho
> lệnh `/web-ui-confirm-dialog` ở `.claude/commands/web-ui-confirm-dialog.md`.

**Chọn Variant/Color cho `<YesButton>` theo bản chất hành động Yes** (`.claude/rules/mudblazor-flat-ui.md` §3):
- Yes = phá hủy/không hoàn tác (xóa, hủy giao dịch, khóa) → `Variant="Variant.Outlined" Color="Color.Error"`.
- Yes = xác nhận tích cực/chốt luồng, không phá hủy (kích hoạt, mở khóa, đồng bộ lại, retry) →
  `Variant="Variant.Filled" Color="Color.Primary"` (hoặc `Color.Success`/`Color.Warning` nếu ngữ
  cảnh cần nhấn mạnh cảnh báo — vd "Xác nhận kết thúc ngày" dùng `Filled`/`Warning` vì không thể
  hoàn tác dù không phải "xóa dữ liệu").

```razor
@* Khai báo trong Razor template — đặt gần đầu content, TRƯỚC mọi @if bao ngoài (nếu page có list/edit mode) *@
<MudMessageBox @ref="_confirmBox" Title="Xác nhận xóa" CancelText="Hủy">
    <MessageContent>@_confirmMsg</MessageContent>
    <YesButton><MudButton Variant="Variant.Outlined" Color="Color.Error">Xóa</MudButton></YesButton>
</MudMessageBox>

@code {
    private MudMessageBox? _confirmBox;
    private string _confirmMsg = string.Empty;

    private async Task DeleteAsync(MyItem item)
    {
        _confirmMsg = $"Bạn có chắc muốn xóa [{item.Code}]? Không thể hoàn tác.";
        var ok = await _confirmBox!.ShowAsync();
        if (ok != true) return;
        // thực hiện action
    }
}
```

**Title/YesText/Variant/Color động** (vd khóa/mở khóa dùng chung 1 dialog — 2 hành động khác bản
chất): thêm field `_confirmTitle`/`_confirmYesText`/`_confirmYesColor` (kiểu `Color`), bind vào
`Title="@_confirmTitle"` và
`<YesButton><MudButton Variant="@(_confirmYesColor == Color.Success ? Variant.Filled : Variant.Outlined)" Color="@_confirmYesColor">@_confirmYesText</MudButton></YesButton>`
— **Variant cũng phải tính động theo Color** (không hardcode `Outlined`), vì "khóa" (Error) và
"kích hoạt/mở khóa" (Success) thuộc 2 nhóm khác nhau trong bảng Button convention. Set cả 3 field
trước khi gọi `_confirmBox!.ShowAsync()`. Ví dụ thực tế: `Admin/UsersPage.razor`
(`ConfirmToggleAsync` — khóa dùng `Outlined`/`Color.Error`/"Khóa", kích hoạt dùng
`Filled`/`Color.Success`/"Kích hoạt"); `Catalog/Product/ProductLockPage.razor` (dialog dùng chung
khóa/mở khóa sản phẩm — ternary theo nội dung `_confirmMsg` vì không có field Color riêng).

> Ví dụ thực tế (static Title/YesText): `src/POS.Web/Components/Pages/Catalog/Product/ProductLockPage.razor`,
> `Ops/PosMapPage.razor`, `Store/Operations/BusinessDayPage.razor`.
