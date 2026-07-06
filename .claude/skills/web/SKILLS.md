# Skill: Blazor Server Web Dashboard (POS.Web)

> **Áp dụng khi:** viết hoặc chỉnh sửa bất kỳ thành phần nào trong
> `src/POS.Web/` — page, component, layout, service, hoặc auth layer.
> Bao gồm: tạo page mới, thêm nav link, chỉnh auth, sử dụng MudBlazor, inject service.

---

## Skill con — đọc khi cần (tránh đọc hết file này)

> File này chỉ giữ quy tắc nền tảng. Pattern chi tiết tách ra file riêng — chỉ đọc đúng file khi gặp tình huống.

| File | Đọc khi |
|---|---|
| **`.claude/skills/web/form-input.md`** | **Thiết kế form nhập liệu (MudCard section + MudGrid + validation trực quan); Placeholder vs HelperText; chế độ CHỈ XEM khi bản ghi khóa sau tạo + field ngoại lệ có nút Lưu điều kiện** |
| `.claude/skills/web/filter-store.md` | Thêm combobox lọc cửa hàng vào page |
| `.claude/skills/web/datatable.md` | Tạo bảng dữ liệu (MudTable) — client/server/dynamic |
| `.claude/skills/web/charts.md` | Thêm biểu đồ Line/Bar (MudBlazor v9) |
| `.claude/skills/web/reports.md` | Trang báo cáo pivot / xuất PDF |
| `.claude/skills/web/theming.md` | Sửa màu/theme toàn app |
| `.claude/skills/web/deployment.md` | Deploy production (Docker / nginx) |
| **`.claude/skills/web/audit-logging.md`** | **Tạo/sửa page có thao tác Create/Update/Delete** |
| **`.claude/skills/web/ui-polish-standard.md`** | **Làm đẹp/đồng bộ UI trang đã có — chỉ sửa markup, giữ `@code`** |

---

## Quy tắc cốt lõi

**4 nguyên tắc không được vi phạm:**

1. Toàn bộ UI dùng **MudBlazor** — không dùng raw HTML/CSS thuần (inline style nhỏ được chấp nhận)
2. Serialization dùng **Newtonsoft.Json** (`JsonConvert.*`) — **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json`
3. Mọi page phải có `@attribute [Authorize(Policy = ...)]` và `@rendermode InteractiveServer`
4. Mọi page có thao tác **Create / Update / Delete** — **BẮT BUỘC** đọc và áp dụng `.claude/skills/web/audit-logging.md`

---

## Roles và Policy mapping

| Role constant | String value | Policy constant | Dùng cho |
|---|---|---|---|
| `WebRoles.StoreOperator` | `"StoreOperator"` | `WebPolicies.StoreAndAbove` | `Pages/Store/*` |
| `WebRoles.ITOps` | `"ITOps"` | `WebPolicies.OpsAndAbove` | `Pages/Ops/*` |
| `WebRoles.SystemAdmin` | `"SystemAdmin"` | `WebPolicies.AdminOnly` | `Pages/Admin/*` |

**Coverage của từng policy:**
- `StoreAndAbove` = StoreOperator + ITOps + SystemAdmin (cả 3)
- `OpsAndAbove` = ITOps + SystemAdmin
- `AdminOnly` = chỉ SystemAdmin

> Nguồn: `src/POS.Web/Auth/WebRoles.cs`

---

## Page component — pattern bắt buộc

```razor
@page "/store/ten-trang"                                       @* BẮT BUỘC — route kebab-case *@
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]     @* BẮT BUỘC — đúng section *@
@rendermode InteractiveServer                                  @* BẮT BUỘC — có tương tác *@

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth

@inject ICentralSaleRepository SaleRepo                        @* tuỳ chọn — theo nhu cầu *@
@inject IKibanaService KibanaService                           @* khuyến nghị — logging *@
@inject ISnackbar Snackbar                                     @* tuỳ chọn — notification *@

<PageTitle>Tên trang – POS Dashboard</PageTitle>

@if (_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-3"/>
}
else if (_errorMsg != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">@_errorMsg</MudAlert>
}
else if (_isEmpty)
{
    <MudAlert Severity="Severity.Info">Không có dữ liệu.</MudAlert>
}
else
{
    @* nội dung thật *@
}

@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!; @* BẮT BUỘC nếu cần user info *@

    private bool _loading = true;
    private bool _isEmpty;
    private string? _errorMsg;
    private IReadOnlyList<string> _userStoreCodes = [];      @* BẮT BUỘC với StoreAndAbove *@

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        var json = state.User.FindFirst("store_codes")?.Value;
        _userStoreCodes = string.IsNullOrEmpty(json)
            ? []
            : JsonConvert.DeserializeObject<List<string>>(json) ?? [];
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMsg = "Không thể tải dữ liệu.";
            KibanaService.LogException("PageName.OnInitialized", "", 0, "", ex.Message);
        }
        finally { _loading = false; }
    }

    private async Task LoadDataAsync()
    {
        // gọi repository/service ở đây
        // _isEmpty = data.Count == 0;
    }
}
```

---

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

## Loading / Error / Empty state

Mọi page có data phải handle đủ 3 state:

```razor
@* State 1 — Loading *@
@if (_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-3"/>
}

@* State 2 — Error (exception khi load) *@
else if (_errorMsg != null)
{
    <MudAlert Severity="Severity.Error" Class="mb-3">@_errorMsg</MudAlert>
}

@* State 3 — Empty (load thành công nhưng không có data) *@
else if (_isEmpty)
{
    <MudAlert Severity="Severity.Info">Không có dữ liệu trong khoảng thời gian này.</MudAlert>
}

@* State 4 — Nội dung thật *@
else
{
    @* render table / chart / cards *@
}
```

```csharp
// Trong @code — pattern chuẩn
private bool _loading = true;
private bool _isEmpty;
private string? _errorMsg;

private async Task LoadDataAsync()
{
    _loading = true;
    StateHasChanged();  // cần thiết nếu gọi từ event handler (không phải OnInitializedAsync)
    try
    {
        var data = await SaleRepo.GetXxxAsync(...);
        _isEmpty = data.Count == 0;
        // xử lý data...
    }
    catch (Exception ex)
    {
        _errorMsg = "Không thể tải dữ liệu.";
        KibanaService.LogException("PageName.LoadData", "", 0, "", ex.Message);
    }
    finally { _loading = false; }
}
```

---

## DataTable chuẩn — `MudTable<T>`

> **Chi tiết đầy đủ: `.claude/skills/web/datatable.md`** — đọc trước khi thêm bảng dữ liệu.
> **BẮT BUỘC** dùng MudBlazor `<MudTable>` — sort + phân trang built-in, KHÔNG tự viết HTML `<table>` hay base class.

### Tóm tắt nhanh

| Tình huống | Cách làm |
|---|---|
| Client-side (load 1 lần, đa số) | `<MudTable Items>` + `MudTableSortLabel` + `MudTablePager` |
| Server-side (data lớn, SP theo trang) | `<MudTable @ref ServerData>` + `_table.ReloadServerData()` |
| Cột động (SQL kết quả) | `Items` = `List<object?[]>`, loop index trong RowTemplate |
| Search / Footer tổng | Search → `<ToolBarContent>`; tổng → `<FooterContent>` |

```razor
<MudTable Items="@_items" Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" Loading="@_loading" HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<MyDto, object>(x => x.FieldA)">Cột A</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate><MudTd DataLabel="Cột A">@context.FieldA</MudTd></RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }" RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

**2 anti-pattern quan trọng nhất:**
- ❌ Tự viết `<table class="pos-table">` / `@inherits PosTableBase<T>` — base class đã xóa, dùng `MudTable`.
- ❌ `MudPagination` thủ công — dùng `<MudTablePager>` trong `<PagerContent>`.
- ❌ `PageSizeOptions` không bắt đầu bằng `10` → ô chọn số dòng/trang trống (default `RowsPerPage=10` không khớp).

> **Ngoại lệ:** Pivot report (cột-ngày động) vẫn dùng `<table class="pos-table rpt-pivot-table">` — xem `reports.md`.

---

## Store Selector — Dual Mode (StoreOperator vs Manager/Admin)

> **Chi tiết đầy đủ: `.claude/skills/web/filter-store.md`** — đọc file này trước khi thêm bộ lọc cửa hàng vào page mới.
> Áp dụng bắt buộc cho mọi page `StoreAndAbove` có filter theo cửa hàng.

### Tóm tắt nhanh

| | StoreOperator | ITOps / Admin |
|---|---|---|
| UI | `MudTextField ReadOnly` hiển thị `"2018 – Cửa hàng demo"` | `MudAutocomplete T="StoreDto"` tìm theo mã + tên |
| Binding | `_filterStoreNo` (locked) | `_selectedStore: StoreDto?` |
| Nguồn data | `MdRepo.GetStoreListAsync()` (cache 12h) | Như trái |

```razor
@* Thêm @using POS.Common.Dtos.CentralMD *@
<MudItem xs="12" sm="6" md="3">
    @if (_isStoreOperator)
    {
        <MudTextField Value="@StoreDisplayText"
                      Label="Cửa hàng" Variant="Variant.Outlined" Margin="Margin.Dense"
                      ReadOnly="true" Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Store"/>
    }
    else
    {
        <MudAutocomplete T="StoreDto"
                         @bind-Value="_selectedStore"
                         Label="Cửa hàng" Placeholder="Tất cả cửa hàng"
                         Variant="Variant.Outlined" Margin="Margin.Dense"
                         SearchFunc="@SearchStoreAsync"
                         ToStringFunc="@(s => s == null ? "" : $"{s.StoreNo} – {s.Name}")"
                         Clearable="true" MinCharacters="0" MaxItems="50"
                         Adornment="Adornment.Start" AdornmentIcon="@Icons.Material.Filled.Store"/>
    }
</MudItem>
```

```csharp
// Fields
private bool                  _isStoreOperator;
private string?               _filterStoreNo;      // StoreOperator (locked) + query param
private IReadOnlyList<string> _userStoreCodes = [];
private List<StoreDto>        _allStores      = [];
private StoreDto?             _selectedStore;      // ITOps/Admin autocomplete binding

// OnInitializedAsync
_isStoreOperator = _userStoreCodes.Count > 0;
if (_isStoreOperator) _filterStoreNo = _userStoreCodes[0];
_allStores = await MdRepo.GetStoreListAsync();     // cache Redis 12h

// LoadDataAsync
var storeNo = _isStoreOperator ? _filterStoreNo : _selectedStore?.StoreNo;

// ResetFilterAsync
if (!_isStoreOperator) _selectedStore = null;

// SearchStoreAsync — BẮT BUỘC có .Take(50) để tránh materialize toàn bộ list
private Task<IEnumerable<StoreDto>> SearchStoreAsync(string value, CancellationToken ct)
{
    IEnumerable<StoreDto> matches = string.IsNullOrWhiteSpace(value)
        ? _allStores
        : _allStores.Where(s =>
            (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
    return Task.FromResult(matches.Take(50));
}

// StoreDisplayText (readonly field cho StoreOperator)
private string StoreDisplayText => _allStores.FirstOrDefault(s => s.StoreNo == _filterStoreNo) is { } st
    ? $"{st.StoreNo} – {st.Name}"
    : (_filterStoreNo ?? "");
```

**Anti-patterns:**
- ❌ `GetStoreSetConfigAsync()` cho store picker — không có `Name`, chỉ có `StoreNo`
- ❌ `MudAutocomplete T="string"` với `CoerceValue="true"` — không validate, mất tên
- ❌ Chỉ load `_allStores` cho ITOps — StoreOperator cũng cần để hiển thị tên đầy đủ
- ❌ `_filterStoreNo = null` trong Reset cho ITOps — phải dùng `_selectedStore = null`

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/Transactions/TransactionsPage.razor`
> Chi tiết: `.claude/skills/web/filter-store.md`

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
| `PosKpiCard` | Chưa tạo — dùng `MudPaper` + `MudText` inline tạm | KPI card |
| `PosStatusChip` | Chưa tạo — dùng `MudChip T="string"` inline tạm | Status badge |

> Ví dụ inline KPI card: `src/POS.Web/Components/Pages/Store/RevenuePage.razor` — `MudPaper` + border-left + `MudText`.

---

## Services có thể inject trong POS.Web

### Từ POS.Infrastructure (đăng ký qua `AddInfrastructure()`)

| Interface | Lifetime | Dùng cho |
|---|---|---|
| `IRedisService` | Singleton | Cache (HashGet/Set, StringGet/Set, KeyExists...) |
| `IKibanaService` | Singleton | Structured logging → Elasticsearch |
| `IFileLogHelper` | Singleton | File log fallback |
| `IRabbitMQProducer` | Singleton | Message queue |
| `IKafkaProducer` | Singleton | Kafka producer |
| `ICentralMDRepository` | Scoped | Master data (store config, POS setup, SysWebApi...) |
| `ICentralSaleRepository` | Scoped | Sales data (orders, transactions, revenue...) |
| `IRptCentralSaleRepository` | Scoped | Report queries (SalesByCategory, pivot-style reports...) |
| `ILoyaltyRepository` | Scoped | Loyalty (members, points, wincode...) |
| `IOfferStaffRepository` | Scoped | Staff discount |
| `IWincodeRepository` | Scoped | Wincode / winlife |

**Concrete type — inject trực tiếp (không có interface):**
- `CentralMDConnectionFactory` — dùng khi cần connection thủ công
- `LoyaltyConnectionFactory` — dùng khi cần connection thủ công

> **Lưu ý:** Không inject `IDbConnectionFactory` (interface không tồn tại trong DI) — inject concrete type trực tiếp.

### Từ POS.Application (đăng ký qua `AddApplication()`)

| Interface | Lifetime | Dùng cho |
|---|---|---|
| `ICommonService` | Scoped | POS common ops (store setup, shift, EOD...) |
| `IHealthCheckService` | Scoped | Kiểm tra sức khỏe hạ tầng |
| `IAkaChainLoyaltyService` | Scoped | FMV / AkaChain loyalty |
| `IGotITService` | Scoped | GotIT voucher partner |
| `IUrboxService` | Scoped | Urbox voucher partner |
| `IKafkaService` | Scoped | Kafka publisher |
| `IDataRawService` | Scoped | File sale processing |
| `ISyncDataPosService` | Scoped | POS sync |

### Chỉ trong POS.Web

| Interface | Lifetime | Dùng cho |
|---|---|---|
| `IWebUserService` | Scoped | Dashboard user auth (login, get user, get store codes) |

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

## MudBlazor — component mapping

| Cần làm | MudBlazor component |
|---|---|
| Bảng dữ liệu có sort / filter / page | `MudTable<T>` + `MudTableSortLabel` + `MudTablePager` (xem section DataTable chuẩn) |
| Bảng server-side paging | `MudTable<T>` với `ServerData` + `@ref` + `ReloadServerData()` |
| Biểu đồ đường | `<Line T="double">` (v9 — cần `@using MudBlazor.Charts`) |
| Biểu đồ cột | `<Bar T="double">` (v9 — cần `@using MudBlazor.Charts`) |
| Số liệu tổng quan | `MudPaper` + `MudText` (xem RevenuePage KPI cards) |
| Thông báo popup | `ISnackbar` (inject, gọi `Snackbar.Add(...)`) |
| Dialog xác nhận đơn giản | `MudMessageBox @ref="_msgBox"` trong Razor + `await _msgBox!.ShowAsync()` trong code |
| Dialog form đầy đủ | `IDialogService` + `DialogService.ShowAsync<T>()` |
| Input text | `MudTextField<T>` |
| Dropdown chọn một | `MudSelect<T>` |
| Date picker | `MudDatePicker` |
| Chip lọc / filter | `MudChip T="string"` (bắt buộc có `T=`) |
| Badge trạng thái | `MudChip T="string"` với `Color` |
| Loading thanh ngang | `MudProgressLinear Indeterminate="true"` |
| Loading tròn | `MudProgressCircular Indeterminate="true"` |
| Alert cố định | `MudAlert Severity="..."` |
| Card nội dung | `MudCard` + `MudCardContent` |
| Paper nền | `MudPaper Elevation="2" Class="pa-4"` |
| Grid layout | `MudGrid` + `MudItem xs="12" sm="6"` |

> Bảng trên map theo **nhu cầu chức năng** (cần làm gì → dùng component nào). Khi polish/tạo UI
> mới đối chiếu trực tiếp với 1 mockup HTML (`div.sidebar`, `div.card`, `button.btn-primary`...),
> dùng bảng **"Mapping HTML mockup → MudBlazor Component"** ở `.claude/rules/mudblazor-flat-ui.md`
> mục 0 — map theo **cấu trúc/markup**, bổ sung cho bảng này chứ không lặp lại.

### Pattern: Polish/tạo UI theo mockup HTML — quy trình chuẩn

> Áp dụng khi nhận 1 mockup HTML/CSS mới cần đưa ngôn ngữ thiết kế vào POS.Web (page/component có
> sẵn, KHÔNG tạo page mới nếu không được yêu cầu rõ).

1. Đối chiếu từng phần tử mockup với bảng mapping ở `.claude/rules/mudblazor-flat-ui.md` mục 0 —
   không tự viết `<div>`/`<table>` thuần thay cho component MudBlazor đã có.
2. Màu/spacing cụ thể lấy từ token `--pos-*` trong `app.css` + `PosTheme.cs` (Palette/Typography/
   Shadows) — không hardcode hex/px mới trong markup Razor.
3. Cần ghi đè CSS của component con MudBlazor tự render → xem `.claude/rules/mudblazor-flat-ui.md`
   mục 10 "CSS Isolation" trước khi quyết định `app.css` hay `{Component}.razor.css`.

```razor
@* Ví dụ: card + filter-bar tối thiểu theo mapping mục 0 *@
<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">   @* div.filter-bar *@
    <MudGrid Spacing="2">...</MudGrid>
</MudPaper>

<MudPaper Elevation="2" Class="pa-4">                          @* div.card *@
    <MudText Typo="Typo.h5">@value</MudText>                  @* .kpi-value *@
</MudPaper>
```

> Ví dụ đầy đủ hơn (page-header + filter + table): xem "Template Page Component chuẩn" ở
> `CLAUDE.md` mục "POS.Web — Blazor Server Dashboard" §5.

### Charts (Line / Bar) — MudBlazor v9

> **Chi tiết đầy đủ: `.claude/skills/web/charts.md`** — đọc trước khi thêm biểu đồ.
> MudBlazor 9.5.0 đổi hoàn toàn cú pháp chart: dùng `<Line T="double">` / `<Bar T="double">` (KHÔNG `<MudChart>`), data `ChartData<double>`, options `LineChartOptions`/`BarChartOptions`. Bao gồm pattern Y-axis auto-scale.

---

## Responsive UI — BẮT BUỘC (mobile + tablet + PC)

> **Chi tiết đầy đủ: `CLAUDE.md §10`** — đọc trước khi tạo hoặc sửa bất kỳ page nào.
> Áp dụng cho mọi viewport: xs (<600px), sm (600–959px), md+ (960px+).

### Quy tắc cốt lõi

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

## Chuẩn đặt tên cột DataTable — BẮT BUỘC áp dụng toàn dự án

### Quy tắc

> Tiêu đề cột trong `<MudTh>` **BẮT BUỘC** dùng tên field tiếng Anh tương ứng với tên cột trong database / DTO.
> Áp dụng cho tất cả page trong menu **Vận hành** và **Quản trị**.
> `DataLabel` trong `<MudTd>` phải **khớp** với tiêu đề `<MudTh>` tương ứng.

### Ngoại lệ tên cột (mapping đặc biệt)

| Bảng DB | DB Column | Header cột |
|---|---|---|
| `Store` | `[No]` | `StoreNo` |
| `Store` | `[ClosingMethod]` | `Status` |
| `POSTerminal` | `[No]` | `PosNo` |
| `Branch` | `[No]` | `BranchNo` |

### Ví dụ đúng

```razor
@* Header — tên field DB *@
<HeaderContent>
    <MudTh><MudTableSortLabel SortBy="...">StoreNo</MudTableSortLabel></MudTh>
    <MudTh>Name</MudTh>
    <MudTh>IPAddress</MudTh>
    <MudTh>LastDateModified</MudTh>
    <MudTh>Status</MudTh>
</HeaderContent>

@* RowTemplate — DataLabel phải khớp Header *@
<RowTemplate>
    <MudTd DataLabel="StoreNo">@context.StoreNo</MudTd>
    <MudTd DataLabel="Name">@context.Name</MudTd>
    <MudTd DataLabel="IPAddress">@context.IPAddress</MudTd>
    <MudTd DataLabel="LastDateModified">@(context.LastDateModified?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—")</MudTd>
    <MudTd DataLabel="Status">...</MudTd>
</RowTemplate>
```

### Mapping đã áp dụng (tham khảo)

| Page | DTO | Cột đã chuyển sang English |
|---|---|---|
| `LogsPage.razor` | `InterfaceErrorDto` | ErrorID, ErrorDateTime, UserName, ErrorProcedure, ErrorMessage, ErrorSeverity, ErrorNumber |
| `DataRawLogPage.razor` | `DataRawJsonLogDto` | CrtDate, DataType, Flag, ErrorMessage |
| `PosMapPage.razor` | `PosTerminalListDto` | IsOnline, PosNo, StoreNo, IPAddress, StyleProfile, BluePosVersion, Status, DateTimePos |
| `StorePage.razor` | `StoreListDto` | StoreNo, Name, Address, BranchNo, Status, LastDateModified |
| `ProvincesPage.razor` | `BranchAdminDto` | BranchNo, Description, Address, VATRegistrationNo |
| `UsersPage.razor` | `DashboardUser` | Id, Username, FullName, Role, IsActive |
| `AuditPage.razor` | `AuditRecord` | Id, Actor, Database, Status, RowsAffected, HasWhere, SqlText, ElapsedMs, ExecutedAt, DecidedAt |

---

## Chuẩn format hiển thị DateTime — BẮT BUỘC áp dụng toàn dự án

### Quy tắc

| Loại trường | Format | Ví dụ |
|---|---|---|
| Cột datetime trong datatable (Created, Updated, timestamp...) | `"yyyy-MM-dd HH:mm:ss"` | `2025-06-25 14:30:00` |
| Ngày thuần (business date, date picker label) | `"dd/MM/yyyy"` | `25/06/2025` |
| Label chart / trục X | `"dd/MM"` | `25/06` |
| Timestamp UI phụ (KPI card "Lần cuối", header in/out) | `"HH:mm:ss"` | `14:30:00` |

### Áp dụng

```razor
@* Cột datatable — datetime đầy đủ *@
<MudTd DataLabel="Tạo lúc">@context.CreatedDate?.ToString("yyyy-MM-dd HH:mm:ss")</MudTd>
<MudTd DataLabel="Cập nhật lúc">@context.UpdatedDate?.ToString("yyyy-MM-dd HH:mm:ss")</MudTd>
<MudTd DataLabel="Thời gian">@(context.CrtDate.ToString("yyyy-MM-dd HH:mm:ss"))</MudTd>

@* Nullable — dùng null-coalescing *@
<MudTd DataLabel="Last seen">@(context.DateTimePos?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—")</MudTd>

@* Ngày thuần — giữ dd/MM/yyyy *@
<MudTd DataLabel="Ngày KD">@context.BussinessDate.ToString("dd/MM/yyyy")</MudTd>
```

### Đã áp dụng tại
- `PosMapPage.razor` — cột Last seen
- `DataRawLogPage.razor` — cột CrtDate
- `LogsPage.razor` — cột ErrorDateTime
- `EosShiftsPage.razor` — cột CloseShiftDate
- `PosTerminalDetailDialog.razor` — CreatedDate, UpdatedDate, LastDateModified, DateTimePos
- `AuditPage.razor` — ExecutedAt, DecidedAt (đã đúng từ đầu)

---

## KHÔNG làm (anti-patterns)

- ❌ Quên `@rendermode InteractiveServer` → component không tương tác được (button/event bị ignore)
- ❌ Quên `@attribute [Authorize(Policy = ...)]` → page không được bảo vệ, ai cũng truy cập được
- ❌ Dùng `System.Text.Json` hoặc `JsonSerializer.*` → vi phạm contract serialization toàn solution
- ❌ Gọi HTTP đến `POS.Api` từ `POS.Web` → inject service trực tiếp qua DI thay vì gọi HTTP
- ❌ Inject `IDbConnectionFactory` (interface) → phải inject concrete: `CentralMDConnectionFactory`
- ❌ Không có `_loading` state → UI trắng khi chờ data, UX xấu
- ❌ Không filter row-level với `StoreAndAbove` → lộ data cửa hàng khác cho StoreOperator
- ❌ Bỏ `try/catch` trong `OnInitializedAsync` → **không chỉ crash page**: exception chưa bắt trong lifecycle method làm SẬP LUÔN circuit Blazor Server (SignalR) — mọi tương tác sau đó (kể cả dialog khác đang mở) fail với lỗi JS `"Cannot send data if the connection is not in the 'Connected' State"`. Xem pattern bên dưới khi `OnInitializedAsync` gọi nhiều nguồn độc lập.
- ❌ Gọi `SignInAsync` trong Blazor InteractiveServer component → phải dùng bridge token (xem Auth flow)
- ❌ Dùng `<MudChart ChartType="...">` (v8 syntax) → compile error với MudBlazor 9.5.0
- ❌ Dùng `ChartOptions { YAxisTicks, LineStrokeWidth }` → đã đổi sang `LineChartOptions` / `BarChartOptions` trong v9
- ❌ `BarChartOptions { ShowLegend = false }` không set `YAxisSuggestedMax` → `YAxisTicks` default=20 (spacing!) làm Y-axis luôn max=20 dù data chỉ 2–8M
- ❌ `PosTheme.cs` thiếu `Body1 = new Body1Typography { FontSize = "0.75rem" }` → dropdown/autocomplete/picker popup render theo font-size mặc định MudBlazor (16px), to hơn text bảng/input. `Default.FontSize` KHÔNG tự cascade xuống `Body1`.
- ❌ Chỉ set `Typography.Default.FontFamily` trong `PosTheme.cs` rồi tưởng đổi font toàn app → **SAI**. MudBlazor sinh CSS variable RIÊNG cho mỗi typography variant (`--mud-typography-h5-family`, `--mud-typography-body1-family`...) — KHÔNG kế thừa từ `Default`. Phần lớn text hiển thị thật (H5/H6/Subtitle1/Body1/Body2/Caption/Button) vẫn giữ font mặc định MudBlazor nếu không set `FontFamily` riêng trên TỪNG variant (`H1Typography`...`ButtonTypography`). Xem `PosTheme.cs` — mọi variant đều set `FontFamily` tường minh dù giá trị giống nhau.
- ❌ `MudDatePicker Editable="true"` → click vào ô text không mở calendar, phải click icon. Dùng `AutoClose="true"` (bỏ `Editable`) để click text = mở calendar + tự đóng sau chọn.
- ❌ Raw SQL trong page/component → phải đi qua Repository hoặc Service
- ❌ Thêm nav link mới mà quên wrap `<AuthorizeView Policy="...">` trong `MainLayout.razor`
- ❌ Dùng `MudStack Row Justify.SpaceBetween` cho header title+button → vỡ layout mobile, button stretch cao bất thường
- ❌ `MudButton` trong `MudStack Row` cạnh `MudSelect` có Label → button stretch theo chiều cao Select+Label → thêm `Style="align-self:center"` vào button
- ❌ Tự viết `<table class="pos-table">` + `PosTableBase<T>` cho DataTable mới → dùng `MudTable` (xem section DataTable chuẩn)
- ❌ `MudTable` thiếu `HorizontalScrollbar="true"` → table bị clip trên mobile (pivot/raw table thì wrapper `overflow-x:auto`)
- ❌ Chip container thiếu `flex-wrap` → chips tràn ngang, mất trên mobile
- ❌ `@namespace` đặt trước `@page` trong routable component → Blazor endpoint routing không nhận dạng được route → page không truy cập được (xem section "Tổ chức thư mục Pages")
- ❌ Đặt page mới vào root `Store/` thay vì sub-folder đúng nhóm nav (Operations/Transactions/Reports)
- ❌ Đặt dialog component lẫn với page file trong cùng folder — dialog không có `@page`, đặt vào `{Section}/Dialogs/`
- ❌ Gọi `IDialogService.ShowMessageBox(...)` cho confirm đơn giản → không có overload đó trong MudBlazor v9. Dùng `MudMessageBox @ref` trong Razor + `await _msgBox!.ShowAsync()` (xem pattern bên dưới)
- ❌ Gọi `DialogService.ShowAsync<MudMessageBox>(title, parameters, options)` cho confirm dialog → render nút Yes bằng markup mặc định của MudBlazor, không có `<YesButton>` slot để chỉnh Variant/Color theo bản chất hành động (CLAUDE.md §14). Luôn dùng `MudMessageBox @ref` (xem pattern bên dưới) — lỗi này đã xảy ra thật ở 8 page, khó phát hiện bằng grep vì nút không nằm trong markup của page.

### Pattern: `MudMessageBox @ref` — confirm dialog đơn giản
> Áp dụng khi: cần hỏi "Bạn có chắc không?" trước lock/unlock/delete/approve/retry mà không cần
> form — thay thế `IDialogService.ShowMessageBox` (không tồn tại trong v9).
>
> **BẮT BUỘC dùng cách khai báo `@ref` này — KHÔNG dùng
> `DialogService.ShowAsync<MudMessageBox>(title, parameters, options)`.** Cách gọi qua
> `ShowAsync<MudMessageBox>` render Yes/No button bằng markup MẶC ĐỊNH của MudBlazor (bên trong
> thư viện, không nằm trong source của dự án) — **không có `<YesButton>` slot để can thiệp**, nên
> không thể chọn đúng Variant/Color theo bản chất hành động Yes (xem bảng Button convention ở
> `CLAUDE.md §14`). Đây là lỗi có thật đã xảy ra ở 8 page (BusinessDayPage, VouchersPage,
> SpecialComboPage, PromotionSetupPage, PosDataSetupPage, DataRawLogPage, UsersPage, BankPosPage)
> — phát hiện vì `grep MudButton.*Variant.Filled` không bắt được (nút đó không tồn tại trong
> markup của dự án, MudBlazor tự render). Luôn dùng cách khai báo `@ref` bên dưới để nút Yes nằm
> trong markup của page, chọn đúng Variant/Color theo bảng dưới.

**Chọn Variant/Color cho `<YesButton>` theo bản chất hành động Yes** (CLAUDE.md §14):
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

---

### Pattern: Modal chi tiết nhiều tab — lazy-load theo tab active (miễn phí, không cần state thủ công)
> Áp dụng khi: dialog/modal có nhiều tab (`MudTabs`/`MudTabPanel`), mỗi tab tự load dữ liệu riêng
> (thường mỗi tab 1 `MudTable ServerData` hoặc 1 lệnh gọi service riêng) và muốn tránh gọi hết
> N service cùng lúc lúc mở modal (khác hành vi legacy DataTables — thường load lại TOÀN BỘ mọi
> tab mỗi lần đổi tab, gây gọi API thừa).

`MudTabs` mặc định **không giữ panel không active trong DOM** (không có `KeepPanelsAlive="true"`)
— nghĩa là nội dung mỗi `MudTabPanel` (kể cả `MudTable ServerData` bên trong) chỉ thực sự
**render lần đầu khi tab đó được kích hoạt**, và `ServerData` chỉ được gọi tại thời điểm đó.
→ **Không cần tự viết `HashSet<int> _loadedTabs` hay `OnActivePanelIndexChanged` để lazy-load
thủ công** — đặt thẳng `MudTable ServerData="LoadXxxAsync"` vào từng `MudTabPanel` là đã lazy-load
đúng theo tab, miễn phí nhờ hành vi mặc định của framework.

```razor
<MudTabs Outlined="true" PanelClass="pa-3">
    <MudTabPanel Text="Tab A">
        <MudTable @ref="_tableA" ServerData="LoadAAsync" T="RowA">...</MudTable>
    </MudTabPanel>
    <MudTabPanel Text="Tab B">
        <MudTable @ref="_tableB" ServerData="LoadBAsync" T="RowB">...</MudTable>
    </MudTabPanel>
</MudTabs>
```

- Tab đầu tiên (mặc định active khi mở dialog) **vẫn load ngay** — nếu là dữ liệu 1-record đơn
  giản (không phải `MudTable`), gọi thẳng trong `OnInitializedAsync` thay vì đợi tab activate.
- Quay lại tab đã xem trước đó sẽ gọi lại `ServerData` (không cache) — chấp nhận được cho modal
  read-only tra cứu; nếu cần cache qua lại giữa các lần xem tab trong CÙNG 1 lần mở dialog, tự
  thêm field lưu kết quả và check trước khi gọi lại.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Promotion/Offers/Dialogs/OfferDetailDialog.razor`
> (6 tab: Header/Buy/Benefits/Get/Site/Priority — port từ legacy modal 6 tab load-tất-cả-mỗi-lần-đổi-tab).

---

### Pattern: Load nhiều nguồn độc lập trong `OnInitializedAsync` — tránh crash circuit
> Áp dụng khi: page/dialog cần load ≥2 nguồn dữ liệu ĐỘC LẬP (list chính + dropdown/lookup, hoặc
> ≥2 dropdown/lookup không liên quan nhau — vd danh sách cửa hàng + danh sách ngân hàng + danh sách
> loại hàng) lúc khởi tạo.
> Rút ra từ sự cố thực tế (lặp lại **5 lần** trong 1 session — `BankPosPage`, `BankPosDetailDialog`,
> `ProductDetailDialog`, `SpecialComboPage`, `PromotionSetupPage`): nhiều lệnh `await` (dù chạy song
> song qua `Task.WhenAll` hay chạy tuần tự từng dòng — **hai cách đều lỗi y hệt nhau**) nằm trong
> 1 `try/catch` DUY NHẤT. Chỉ 1 nguồn lỗi (SP/bảng thiếu ở môi trường DEV) làm exception ném ra giữa
> chừng — các dòng SAU nó KHÔNG BAO GIỜ CHẠY, nên dropdown tương ứng trống dù bản thân nguồn đó lẽ ra
> load được bình thường. Nếu method KHÔNG có `try/catch` bao ngoài nào cả (hay chỉ page có, dialog
> quên) → exception chưa bắt trong lifecycle method còn làm sập luôn CIRCUIT Blazor Server, không
> riêng gì phần data bị lỗi.

**Cách nhận diện "độc lập" (PHẢI tách try/catch) vs "cùng 1 báo cáo" (được dùng chung 1 catch):**

| Tình huống | Độc lập hay cùng báo cáo? | Xử lý |
|---|---|---|
| `_articleTypes` + `_unitOfMeasures` + `_vatCodes` cho 3 dropdown KHÁC NHAU trong form | Độc lập | Tách 3 try/catch |
| `_salesTypes` + `_memberCodes` + `_allStores` cho 3 filter/dropdown KHÁC NHAU | Độc lập | Tách 3 try/catch |
| Summary + Detail list của CÙNG 1 domain (vd DataRawLog summary + DataRawLog list) | Cùng báo cáo | 1 try/catch OK |
| Kỳ hiện tại + kỳ so sánh của CÙNG 1 metric (vd Revenue kỳ này + kỳ trước) | Cùng báo cáo | 1 try/catch OK |
| Order lines + payment entries của CÙNG 1 đơn hàng (dialog chi tiết giao dịch) | Cùng báo cáo | 1 try/catch OK |

Quy tắc nhanh: nếu 2 nguồn dữ liệu đến từ 2 **domain nghiệp vụ khác nhau** (cửa hàng vs ngân hàng vs
loại hàng vs hạng thẻ...) và feed vào 2 **control UI khác nhau** — tách. Nếu chúng chỉ là 2 GÓC NHÌN
của CÙNG 1 dữ liệu/báo cáo (chi tiết vs tổng hợp, kỳ này vs kỳ trước) — gộp 1 catch là hợp lý, vì cả
trang/dialog vốn dĩ vô nghĩa nếu thiếu 1 trong 2.

```csharp
protected override async Task OnInitializedAsync()
{
    // Await + try/catch RIÊNG từng nguồn ĐỘC LẬP — 1 nguồn lỗi không kéo sập các nguồn khác,
    // và quan trọng nhất: không để exception thoát khỏi OnInitializedAsync (circuit crash).
    // Sai y hệt nếu viết tuần tự 3 dòng await trong CÙNG 1 try — không liên quan gì Task.WhenAll.
    try { _stores = await Repo.GetStoreListAsync(); }
    catch (Exception ex)
    {
        FileLogger.WriteExpLogs("MyPage.LoadStores", ex);
        Snackbar.Add("Không tải được danh sách cửa hàng.", Severity.Warning);
    }

    try { _banks = await Repo.GetBankListAsync(); }
    catch (Exception ex)
    {
        FileLogger.WriteExpLogs("MyPage.LoadBanks", ex);
        Snackbar.Add("Không tải được danh sách ngân hàng.", Severity.Warning);
    }
}
```

**Quan trọng:**
- Áp dụng cho CẢ page lẫn dialog con (`MudDialog` component) — dialog dễ bị bỏ sót vì hay copy pattern gọn (1 try bọc hết) nhưng không có `try/catch` bao ngoài như page.
- KHÔNG dùng `await Task.WhenAll(taskA, taskB)` rồi `try/catch` bao NGOÀI `WhenAll` nếu muốn 1 nguồn lỗi không ảnh hưởng nguồn còn lại — `WhenAll` ném exception của task đầu tiên fail, các task còn lại tuy vẫn chạy xong nhưng `.Result` không bao giờ được gán vì nằm sau dòng `await` đã throw.
- KHÔNG chỉ nhìn `Task.WhenAll` khi rà soát code cũ — search cả các `OnInitializedAsync` có ≥2 dòng `await Repo.GetXxxAsync()` tuần tự trong 1 try, feed vào ≥2 field khác nhau.
- Nếu component đã có sẵn kiểu báo lỗi khác (vd `Snackbar.Add(...)` đã dùng ở method khác trong CÙNG file) → dùng lại kiểu đó cho nhất quán, không cần thêm field `_errorMsg` + `MudAlert` mới nếu file chưa có.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosPage.razor` (`LoadDataAsync`),
> `src/POS.Web/Components/Pages/Catalog/PosDevices/BankPosDetailDialog.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Promotion/Offers/SpecialComboPage.razor` (`OnInitializedAsync`),
> `src/POS.Web/Components/Pages/Promotion/Offers/PromotionSetupPage.razor` (`OnInitializedAsync`)

---

## Pattern: Bulk import Excel → lưới preview validate + sửa inline
> Áp dụng khi: page nhập liệu hàng loạt từ Excel cần validate DB (item/uom/barcode tồn tại...) rồi cho
> user sửa lỗi trước khi lưu. Rút ra từ 9.3 Setup giá (`PriceSetupPage`).

- Upload `MudFileUpload T="IBrowserFile"` + nút "Nạp" riêng (KHÔNG auto-validate lúc chọn file) → đọc bằng
  **ClosedXML** (`XLWorkbook`, bỏ header dòng 1, `ws.Row(r).IsEmpty()` để skip). Ngày: check
  `cell.DataType == XLDataType.DateTime` trước khi `GetString()`.
- Validate DB qua service→repo **TVP** (không temp-table/SqlBulkCopy): `DataTable.AsTableValuedParameter("dbo.XxxTVP")`
  chạy query `LEFT JOIN` inline → trả từng dòng kèm `ErrorMessage` (rỗng = hợp lệ).
- Lưới sửa dùng `MudTable Items` + view-model có cờ `HasError`; **`RowStyleFunc`** tô nền dòng lỗi
  (`background-color:#fdecea`); ô Giá/Ngày là `MudTextField`/`MudDatePicker` bind thẳng row.
- **Chặn Lưu khi còn dòng lỗi** (`_errorCount > 0` → Snackbar warning); chip đếm Tổng/Lỗi trên toolbar.
- Save chạy validate nghiệp vụ lần cuối ở Application service (port 100% điều kiện legacy) → SP TVP; audit log sau khi Ok.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Price/PriceSetupPage.razor` +
> `Dialogs/PriceItemPickerDialog.razor`; repo `src/POS.Infrastructure/Repositories/Price/PriceRepository.cs`;
> SQL `docs/sql/SetupSalePrice_Save.sql` (TVP validate + TVP save).

---

## Pattern: Upload ảnh → base64 + preview trong dialog (không dùng varbinary)
> Áp dụng khi: cần lưu 1 ảnh nhỏ (≤2-5MB) đại diện cho 1 entity. Rút ra từ upload ảnh sản phẩm
> (`ProductDetailDialog`) — dự án dùng `nvarchar(max)` base64 cho ảnh (xem tiền lệ
> `dbo.TenderTypeImage.Image`), KHÔNG `varbinary`.

- `MudFileUpload T="IBrowserFile"` (`Accept=".jpg,.jpeg,.png"`, `MaximumFileCount="1"`) →
  `FilesChanged` validate đuôi file + `file.Size` **trước** khi đọc, không đợi Lưu mới báo lỗi.
- Đọc `file.OpenReadStream(maxAllowedSize).CopyToAsync(ms)` → `Convert.ToBase64String(ms.ToArray())`
  → build `data:{mime};base64,{...}` hiển thị `<MudImage>` preview **ngay trong dialog trước khi
  Lưu** (không cần round-trip DB để xem lại).
- **Không lưu thêm cột MIME type** — khi cần hiển thị lại ảnh đã lưu (base64 thuần, không prefix),
  suy đoán PNG/JPEG từ magic-byte prefix của base64: PNG luôn bắt đầu `"iVBORw0KGgo"`, còn lại mặc
  định JPEG. Đủ dùng cho 2 định dạng phổ biến, tránh migration thêm cột.
- Lưu ảnh là **thao tác phụ, tách khỏi transaction chính**: entity chính tạo/lưu thành công trước
  → gọi lưu ảnh sau; nếu lưu ảnh lỗi chỉ Snackbar cảnh báo + log, **không rollback** entity chính đã
  tạo (ảnh là optional, entity không phụ thuộc ảnh để tồn tại hợp lệ).

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Catalog/Product/Dialogs/ProductDetailDialog.razor`
> (upload), `Dialogs/ProductViewDialog.razor` (hiển thị lại); bảng `dbo.ProductImage`, SQL
> `docs/sql/ProductImage_Save.sql`.

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
- [ ] Thêm `<MudNavLink>` vào đúng `<MudNavGroup>` trong `MainLayout.razor` (wrap `<AuthorizeView>`)
- [ ] **Responsive checklist** — xem `CLAUDE.md §10.G`: header dùng `pos-page-header`, DataTable dùng `MudTable HorizontalScrollbar="true"`, chip container có `flex-wrap`, không dùng `MudStack Row Justify.SpaceBetween` cho layout title+controls

---

## Auth flow — bridge token (tham chiếu)

```
Login.razor (InteractiveServer — trên WebSocket circuit)
  → ValidateLoginAsync(username, password) → BCrypt.Verify
  → Tạo ClaimsPrincipal (claims: Name, Role, full_name, store_codes)
  → Lưu vào IMemoryCache với key "_login_{token}" — TTL 30s
  → NavigateTo("/account/signin/{token}", forceLoad: true)
      ↓ thoát ra HTTP pipeline thật
GET /account/signin/{token}  (minimal API — Program.cs)
  → cache.TryGetValue → lấy ClaimsPrincipal
  → ctx.SignInAsync(CookieAuth, principal, IsPersistent=true)
  → Redirect("/")
```

> **Lý do bridge token:** Blazor InteractiveServer chạy trên WebSocket circuit — `HttpContext` đã degraded,
> gọi `SignInAsync` lúc này throw hoặc không set cookie được → circuit crash.
> Phải thoát ra HTTP pipeline thật để set cookie đúng cách.

**Cookie config:** 8h, SlidingExpiration, HttpOnly, SameSite=Strict.
Timeout cấu hình qua `appsettings.json` → `WebApp:SessionTimeoutHours` (default 8).

---

## Theming — Custom MudBlazor Theme

> **Chi tiết đầy đủ: `.claude/skills/web/theming.md`** — đọc khi cần đổi màu/typography toàn app,
> hoặc khi tạo page/component mới cần biết chuẩn hiện hành: sidebar/appbar nền sáng, card
> borderless, `DefaultBorderRadius=16px`, button `Variant.Outlined` mọi nơi, `pos-filter-panel`,
> icon sidebar `Outlined` (cập nhật v2 2026-07-04 — theming.md đã đồng bộ đầy đủ).
> Tập trung tại `src/POS.Web/Theme/PosTheme.cs` + `<MudThemeProvider Theme="@PosTheme.Default"/>`. Lưu ý v9: `FontWeight`/`LineHeight` là string, `Shadows.Elevation` đúng 25 phần tử.

---

## Production deployment

> **Chi tiết đầy đủ: `.claude/skills/web/deployment.md`** — đọc khi deploy production (Docker / nginx / self-contained).
> Bao gồm: explicit `UseRouting()`, fix `_framework/blazor.web.js` 404 từ external IP, nginx config WebSocket, DataProtection keys trong Docker.

---

## Sidebar nav (MainLayout) — 3 cấp

> Áp dụng khi: thêm sub-group mới vào sidebar hoặc thêm leaf MudNavLink vào sub-group.
> Cập nhật 2026-07-04: icon cấp 2 đổi thành `ChevronRight` — **giống hệt** icon cấp 3 (trước đó
> mỗi sub-group có icon riêng như `Monitor`/`Assessment`/`Business`... đã bỏ, giảm nhiễu thị giác).
> Chỉ còn cấp 1 giữ icon riêng biệt làm landmark. Mọi `MudNavGroup` (cấp 1 VÀ cấp 2) BẮT BUỘC thêm
> `HideExpandIcon="true"` — ẩn mũi tên expand/collapse mặc định (`ArrowDropDown`) bên phải, tránh
> trùng lặp thị giác với icon trái. `MudNavLink` (cấp 3) không có prop `HideExpandIcon` (không áp dụng).
> **Icon set BẮT BUỘC `Icons.Material.Outlined.*`** (đổi từ `Filled` 2026-07-04 — xem
> `.claude/skills/web/theming.md` pattern "Sidebar — brand header + icon set Outlined").

```razor
@* Cấp 1 — section (icon riêng, Outlined) + HideExpandIcon ẩn mũi tên phải *@
<MudNavGroup Title="Vận hành" Icon="@Icons.Material.Outlined.MonitorHeart" HideExpandIcon="true" @bind-Expanded="_expandOps">
    @* Cấp 2 — sub-group: icon = ChevronRight (giống cấp 3) + HideExpandIcon *@
    <MudNavGroup Title="Giám sát" Icon="@Icons.Material.Outlined.ChevronRight" HideExpandIcon="true" @bind-Expanded="_expandOpsMonitor">
        @* Cấp 3 — leaf link: icon = ChevronRight. BẮT BUỘC Match="NavLinkMatch.All" *@
        <MudNavLink Href="/ops/health" Icon="@Icons.Material.Outlined.ChevronRight" Match="NavLinkMatch.All">System health</MudNavLink>
        <MudNavLink Href="/ops/alerts" Icon="@Icons.Material.Outlined.ChevronRight" Match="NavLinkMatch.All">Alerts</MudNavLink>
    </MudNavGroup>
</MudNavGroup>
```

```csharp
// @code — UpdateExpanded để parent tự mở khi child match route
private bool _expandOps, _expandOpsMonitor, _expandOpsLog;

private void UpdateExpanded(string uri)
{
    var u = uri.ToLowerInvariant();
    // BẮT BUỘC liệt kê ĐỦ mọi Href leaf đang render trong group — thiếu 1 route (vd thêm
    // link mới mà quên thêm vào đây) khiến navigate tới route đó không match điều kiện NÀO,
    // toàn bộ cây (kể cả các nhánh không liên quan) sụp về false → nhìn như "menu bị thu hết".
    _expandOpsMonitor = u.Contains("/ops/health") || u.Contains("/ops/alerts");
    _expandOpsLog     = u.Contains("/ops/logs") || u.Contains("/ops/data-raw-log");
    _expandOps        = _expandOpsMonitor || _expandOpsLog;
}
```

> Anti-pattern:
> - ❌ Đặt icon riêng biệt (khác `ChevronRight`) cho `MudNavGroup` cấp 2 — chỉ cấp 1 mới giữ icon riêng.
> - ❌ Bỏ `HideExpandIcon="true"` trên `MudNavGroup` — để mặc định sẽ hiện thêm mũi tên `ArrowDropDown` bên phải, thừa vì đã có icon trái + accordion tự mở theo route.
> - ❌ Xóa `@bind-Expanded` để "tắt tính năng gì đó" — đây là cơ chế accordion (I3 trong `docs/WEB_STATUS.md`) tự mở nhánh chứa route đang active và tự đóng nhánh khác; muốn ẩn UI thì sửa `HideExpandIcon`/CSS, không xóa binding.
> - ❌ Thêm `MudNavLink` mới vào markup mà quên thêm route đó vào `UpdateExpanded()` — điều hướng tới route mới sẽ không mở đúng nhánh cha, có thể khiến TOÀN BỘ sidebar collapse (mọi flag đều tính lại từ URI mỗi lần navigate, không giữ trạng thái cũ).
> - ❌ `MudNavLink` thiếu `Match="NavLinkMatch.All"` — mặc định `NavLinkMatch.Prefix` (như `NavLink` gốc) khiến 1 route ngắn (vd `/promotion/coupons`) bị đánh dấu active luôn khi đang ở route dài hơn cùng tiền tố (`/promotion/coupons/issue`) → 2 leaf link cùng sáng active. Áp dụng cho MỌI leaf link, kể cả link chưa có route trùng tiền tố hiện tại (phòng khi thêm route mới sau này).
> Ví dụ thực tế: `src/POS.Web/Components/Layout/MainLayout.razor`

---

## AppBar — Breadcrumb động (thay text tĩnh)

> Áp dụng khi: cần hiển thị vị trí hiện tại (Section / Trang) trong `MudAppBar` thay vì tiêu đề
> app tĩnh. Bắt nguồn từ việc port Topbar theo mockup `theme_html.html` (2026-07-06).

```csharp
// @code trong MainLayout.razor — Dictionary tĩnh copy ĐÚNG text đã có sẵn trong MudNavLink/
// MudNavGroup bên dưới (không tự đặt tên mới). Route không có trong map → breadcrumb rỗng.
private static readonly Dictionary<string, (string Section, string Label)> BreadcrumbMap = new()
{
    ["/store/business-day"] = ("CỬA HÀNG", "Xác nhận kết thúc ngày"),
    // ... liệt kê ĐỦ mọi Href leaf đang render — thiếu route mới thêm sau này sẽ khiến
    // trang đó hiển thị fallback tĩnh thay vì breadcrumb đúng (không crash, chỉ thiếu hiển thị)
};

private void UpdateBreadcrumb(string uri)
{
    var path = new Uri(uri).AbsolutePath.ToLowerInvariant().TrimEnd('/');
    if (BreadcrumbMap.TryGetValue(path, out var crumb))
        (_breadcrumbSection, _breadcrumbLabel) = crumb;
    else
        (_breadcrumbSection, _breadcrumbLabel) = ("", "");
}
// Gọi UpdateBreadcrumb() trong CẢ OnInitialized (route ban đầu) VÀ OnLocationChanged
// (điều hướng sau) — thiếu 1 trong 2 chỗ sẽ khiến breadcrumb sai lúc load lần đầu hoặc khi
// chuyển trang bằng client-side routing.
```

> Anti-pattern:
> - ❌ Tự đặt tên Section/Label mới khác với Title/text đã hiển thị trong `MudNavGroup`/`MudNavLink`
>   — gây 2 nguồn sự thật lệch nhau khi 1 bên đổi mà quên đổi bên kia.
>   Trước đó `MudAppBar` có `Dense="true"`; khi cần chiều cao cụ thể (khớp mockup) đã bỏ `Dense`
>   và set thẳng `LayoutProperties.AppbarHeight` trong `PosTheme.cs` — tránh phải tính ngược hệ số
>   0.75 mà `Dense` áp dụng lên `AppbarHeight` gốc.
> Ví dụ thực tế: `src/POS.Web/Components/Layout/MainLayout.razor`, `src/POS.Web/Theme/PosTheme.cs`.

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

## Security headers / CSP + HTTPS config-driven (Program.cs)
> Áp dụng khi: cấu hình bảo mật tầng hạ tầng cho POS.Web. Section `Security` trong appsettings điều khiển,
> KHÔNG hardcode theo môi trường.

```jsonc
// appsettings: section "Security"
"Mode": "Internet",        // BehindProxy | DirectHttps | Internet — chỉ chi phối ForwardedHeaders
"RequireHttps": false,     // TÁCH RIÊNG: false = cho phép HTTP (cookie SameAsRequest, không HSTS/redirect)
"EnableHsts": true,        // chỉ tác dụng khi RequireHttps=true
"EnableSecurityHeaders": true,  // TẮT ở Development (xem anti-pattern bên dưới)
"KnownProxies": [], "KnownNetworks": []  // chỉ dùng khi Mode=BehindProxy
```

- **Tách `RequireHttps` khỏi `Mode`**: cho phép chạy/test Production qua HTTP mà không vỡ login (cookie `Secure=Always` chỉ bật khi `RequireHttps && !IsDevelopment`). Khi có TLS → đổi `RequireHttps:true`, không sửa code.
- **`Mode=Internet`/`DirectHttps`** ⇒ KHÔNG gọi `UseForwardedHeaders` → bịt giả mạo `X-Forwarded-*` khi app expose thẳng (không proxy). `BehindProxy` mới nạp `KnownProxies`/`KnownNetworks` (để trống = tạm tin mọi proxy + log cảnh báo).
- **CSP cho Blazor Server**: `script-src 'self'` (blazor.web.js + MudBlazor), `style-src 'unsafe-inline'` (MudBlazor inject `<style>`), `connect-src 'self'` (WebSocket `/_blazor` cùng origin), **`frame-src 'self' blob:`** (preview PDF qua iframe blob).

> **Anti-pattern:**
> - ❌ Để CSP bật ở **Development** → `connect-src 'self'` chặn **VS Browser Link / dotnet-watch** (cổng localhost khác) làm tắc auto-reload. Đặt `EnableSecurityHeaders:false` trong `appsettings.Development.json`.
> - ❌ Quên `frame-src ... blob:` → vỡ preview PDF (`<iframe src="blob:...">` ở SalesByCategoryPage).
> - ❌ Ép `Cookie.Secure=Always` không điều kiện → login HTTP (dev/test) gãy vì browser không gửi lại cookie Secure.
>
> Ví dụ thực tế: `src/POS.Web/Program.cs` (vars `securityMode/requireHttps/...` + middleware headers); rollout: `docs/ROLLOUT.md`

## SQL Console hardening
> Áp dụng khi: trang chạy SQL trực tiếp (AdminOnly). Phải mask secret khi log + cho phép tắt.

- Mask `password|pwd|token|secret|apikey` (literal `'...'`) **trước khi** ghi audit DB + Kibana log — tránh lưu plaintext.
- Cờ `Security:EnableSqlConsole` (mặc định true) gate **cả service lẫn page** (defense-in-depth): service trả lỗi/throw, page hiện alert + disable. Nên đặt `false` ở Production expose internet.

> Ví dụ thực tế: `src/POS.Web/Services/SqlConsoleService.cs` (`MaskSecrets`, `IsEnabled`), `Components/Pages/Admin/SqlConsolePage.razor`

---

## Pattern: POS.Web kích hoạt tác vụ server-side của POS.Api qua DI (không HTTP)
> Áp dụng khi: page POS.Web cần chạy 1 tác vụ vốn thuộc POS.Api (sinh file master data, xử lý file…).
> Luật dự án: **KHÔNG** gọi HTTP sang POS.Api — inject thẳng Application service (đã đăng ký chung qua
> `AddApplication()`/`AddInfrastructure()`) và gọi method. Bọc glue vào 1 method Application dùng chung,
> KHÔNG nhồi logic vào `.razor`.

```csharp
// Application: method mới delegate service sinh file có sẵn của POS.Api (KHÔNG chép/đổi logic sinh file)
public async Task<GetMasterDataFileResult> PushStartOfDayDataAsync(string siteCode, string posTerminal, CancellationToken ct = default)
{
    // BẮT BUỘC bám ĐÚNG cách controller dựng đường dẫn đích — dùng MapFtpPath, KHÔNG tự Path.Combine(FolderShare,...)
    var folderFile = $"{siteCode}/{posTerminal}";
    const string pathSync = "SyncDataPos/POS/CHANGE";
    var targetDir = MapFtpPath($"{pathSync}/{folderFile}");     // = FtpRootPath\SyncDataPos\POS\CHANGE\{site}\{terminal}
    var req = new GetMasterDataFileRequest { SiteCode = siteCode, PosTerminal = posTerminal,
        FolderFile = folderFile, PathSync = pathSync, TypeSync = "ALL", TargetDir = targetDir };
    return await masterDataSyncService.EnsureMasterDataFileAsync(req, ct);   // tái dùng nguyên
}
```

- **UI**: nút trong cột Action → `MudMessageBox` confirm → `_syncing` HashSet (theo key row) đổi nút thành
  `MudProgressCircular` + `RowClassFunc` pulse nền; bọc nút trong `<div @onclick:stopPropagation="true">` nếu
  row có `OnRowClick`. Ghi `IAuditLogger.LogAsync(actor,"SYNC",entity,key,null,detailJson)` **khi thành công**.
- **Anti-pattern (bug thực tế)**: tự dựng đường dẫn FTP bằng `Path.Combine(configuration["AppSettings:FolderShare"],...)`
  → sai gốc + thiếu segment (`SyncDataPos\POS`) so với `SyncDataPosController.GetFileFromFTP`. Luôn tra controller
  để lấy đúng `pathSync`/`MapFtpPath`, vì file phải nằm đúng nơi POS tạo/đọc + khớp URL download.
- **Rollout**: file sinh trên host POS.Web nhưng POS tải qua POS.Api → POS.Web `AppSettings:FtpRootPath` phải trỏ
  **chung thư mục vật lý** POS.Api phục vụ (UNC share / cùng volume Docker). Xem `docs/ROLLOUT.md` §O3.

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Ops/PosMapPage.razor` (`SyncDataAsync`),
> `src/POS.Application/Features/DataSync/SyncDataPosService.cs` (`PushStartOfDayDataAsync`)
