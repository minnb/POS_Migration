# Skill: Blazor Server Web Dashboard (POS.Web)

> **Áp dụng khi:** viết hoặc chỉnh sửa bất kỳ thành phần nào trong
> `src/POS.Web/` — page, component, layout, service, hoặc auth layer.
> Bao gồm: tạo page mới, thêm nav link, chỉnh auth, sử dụng MudBlazor, inject service.

---

## Skill con — đọc khi cần (tránh đọc hết file này)

> File này chỉ giữ quy tắc nền tảng. Pattern chi tiết tách ra file riêng — chỉ đọc đúng file khi gặp tình huống.

| File | Đọc khi |
|---|---|
| `.claude/skills/web/filter-store.md` | Thêm combobox lọc cửa hàng vào page |
| `.claude/skills/web/datatable.md` | Tạo bảng dữ liệu (MudTable) — client/server/dynamic |
| `.claude/skills/web/charts.md` | Thêm biểu đồ Line/Bar (MudBlazor v9) |
| `.claude/skills/web/reports.md` | Trang báo cáo pivot / xuất PDF |
| `.claude/skills/web/theming.md` | Sửa màu/theme toàn app |
| `.claude/skills/web/deployment.md` | Deploy production (Docker / nginx) |

---

## Quy tắc cốt lõi

**3 nguyên tắc không được vi phạm:**

1. Toàn bộ UI dùng **MudBlazor** — không dùng raw HTML/CSS thuần (inline style nhỏ được chấp nhận)
2. Serialization dùng **Newtonsoft.Json** (`JsonConvert.*`) — **TUYỆT ĐỐI KHÔNG** dùng `System.Text.Json`
3. Mọi page phải có `@attribute [Authorize(Policy = ...)]` và `@rendermode InteractiveServer`

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
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-3"
                       Style="border-radius:4px"/>
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
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-3"
                       Style="border-radius:4px"/>
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
    <PagerContent><MudTablePager/></PagerContent>
</MudTable>
```

**2 anti-pattern quan trọng nhất:**
- ❌ Tự viết `<table class="pos-table">` / `@inherits PosTableBase<T>` — base class đã xóa, dùng `MudTable`.
- ❌ `MudPagination` thủ công — dùng `<MudTablePager>` trong `<PagerContent>`.

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

// SearchStoreAsync
private Task<IEnumerable<StoreDto>> SearchStoreAsync(string value, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(value))
        return Task.FromResult<IEnumerable<StoreDto>>(_allStores);
    return Task.FromResult(_allStores.Where(s =>
        (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
        (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false)));
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

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`
> Chi tiết: `.claude/skills/web/filter-store.md`

---

## Báo cáo — Pivot table & Report page layout

> **Chi tiết đầy đủ: `.claude/skills/web/reports.md`** — đọc khi tạo trang báo cáo pivot hoặc trang xuất PDF.

- **Pivot report** (hàng × cột-ngày động): dùng `<table class="pos-table rpt-pivot-table">` — ngoại lệ có chủ đích so với MudTable.
- **Report page layout**: header chuẩn (action bar PDF + user info + title + filter summary) cho trang xuất báo cáo.

> Ví dụ: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

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
| Dialog xác nhận | `IDialogService` + `DialogService.ShowAsync<T>()` |
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
- ❌ Bỏ `try/catch` trong `OnInitializedAsync` → crash cả page, không có error message
- ❌ Gọi `SignInAsync` trong Blazor InteractiveServer component → phải dùng bridge token (xem Auth flow)
- ❌ Dùng `<MudChart ChartType="...">` (v8 syntax) → compile error với MudBlazor 9.5.0
- ❌ Dùng `ChartOptions { YAxisTicks, LineStrokeWidth }` → đã đổi sang `LineChartOptions` / `BarChartOptions` trong v9
- ❌ `BarChartOptions { ShowLegend = false }` không set `YAxisSuggestedMax` → `YAxisTicks` default=20 (spacing!) làm Y-axis luôn max=20 dù data chỉ 2–8M
- ❌ `PosTheme.cs` thiếu `Body1 = new Body1Typography { FontSize = "0.875rem" }` → dropdown/autocomplete/picker popup render 16px (MudBlazor built-in cho Body1), to hơn DataTable 14px. `Default.FontSize` KHÔNG tự cascade xuống `Body1`.
- ❌ `MudDatePicker Editable="true"` → click vào ô text không mở calendar, phải click icon. Dùng `AutoClose="true"` (bỏ `Editable`) để click text = mở calendar + tự đóng sau chọn.
- ❌ Raw SQL trong page/component → phải đi qua Repository hoặc Service
- ❌ Thêm nav link mới mà quên wrap `<AuthorizeView Policy="...">` trong `MainLayout.razor`
- ❌ Dùng `MudStack Row Justify.SpaceBetween` cho header title+button → vỡ layout mobile, button stretch cao bất thường
- ❌ `MudButton` trong `MudStack Row` cạnh `MudSelect` có Label → button stretch theo chiều cao Select+Label → thêm `Style="align-self:center"` vào button
- ❌ Tự viết `<table class="pos-table">` + `PosTableBase<T>` cho DataTable mới → dùng `MudTable` (xem section DataTable chuẩn)
- ❌ `MudTable` thiếu `HorizontalScrollbar="true"` → table bị clip trên mobile (pivot/raw table thì wrapper `overflow-x:auto`)
- ❌ Chip container thiếu `flex-wrap` → chips tràn ngang, mất trên mobile

---

## Checklist khi tạo page mới

- [ ] Đặt file đúng thư mục: `Store/` → StoreAndAbove | `Ops/` → OpsAndAbove | `Admin/` → AdminOnly
- [ ] `@page "/section/kebab-case"` — route dùng kebab-case
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

> **Chi tiết đầy đủ: `.claude/skills/web/theming.md`** — đọc khi cần đổi màu/typography toàn app.
> Tập trung tại `src/POS.Web/Theme/PosTheme.cs` + `<MudThemeProvider Theme="@PosTheme.Default"/>`. Lưu ý v9: `FontWeight`/`LineHeight` là string, `Shadows.Elevation` đúng 25 phần tử.

---

## Production deployment

> **Chi tiết đầy đủ: `.claude/skills/web/deployment.md`** — đọc khi deploy production (Docker / nginx / self-contained).
> Bao gồm: explicit `UseRouting()`, fix `_framework/blazor.web.js` 404 từ external IP, nginx config WebSocket, DataProtection keys trong Docker.

---

## Sidebar nav (MainLayout) — 3 cấp

> Áp dụng khi: thêm sub-group mới vào sidebar hoặc thêm leaf MudNavLink vào sub-group.
> Icon chỉ ở cấp 1 và cấp 2 — cấp 3 (leaf) KHÔNG có icon (chỉ tam giác mặc định).

```razor
@* Cấp 1 — section (có icon) *@
<MudNavGroup Title="Vận hành" Icon="@Icons.Material.Filled.MonitorHeart" @bind-Expanded="_expandOps">
    @* Cấp 2 — sub-group (có icon) *@
    <MudNavGroup Title="Giám sát" Icon="@Icons.Material.Filled.Monitor" @bind-Expanded="_expandOpsMonitor">
        @* Cấp 3 — leaf link (KHÔNG icon) *@
        <MudNavLink Href="/ops/health">System health</MudNavLink>
        <MudNavLink Href="/ops/alerts">Alerts</MudNavLink>
    </MudNavGroup>
</MudNavGroup>
```

```csharp
// @code — UpdateExpanded để parent tự mở khi child match route
private bool _expandOps, _expandOpsMonitor, _expandOpsLog;

private void UpdateExpanded(string uri)
{
    var u = uri.ToLowerInvariant();
    _expandOpsMonitor = u.Contains("/ops/health") || u.Contains("/ops/alerts");
    _expandOpsLog     = u.Contains("/ops/logs") || u.Contains("/ops/data-raw-log");
    _expandOps        = _expandOpsMonitor || _expandOpsLog;
}
```

> Anti-pattern: ❌ Thêm `Icon="..."` vào MudNavLink cấp 3.
> Ví dụ thực tế: `src/POS.Web/Components/Layout/MainLayout.razor`

---

## Ví dụ tham chiếu

| Loại | File |
|---|---|
| Page Store mẫu (chart + KPI + filter) | `src/POS.Web/Components/Pages/Store/RevenuePage.razor` |
| Page Ops mẫu (health check) | `src/POS.Web/Components/Pages/Ops/HealthPage.razor` |
| Page Admin mẫu (user management) | `src/POS.Web/Components/Pages/Admin/UsersPage.razor` |
| Layout chính + sidebar nav | `src/POS.Web/Components/Layout/MainLayout.razor` |
| Login (bridge token pattern) | `src/POS.Web/Components/Pages/Login.razor` |
| Auth service (BCrypt + JSON) | `src/POS.Web/Auth/WebUserService.cs` |
| DI registration | `src/POS.Web/Program.cs` |
| Roles + Policies constants | `src/POS.Web/Auth/WebRoles.cs` |
