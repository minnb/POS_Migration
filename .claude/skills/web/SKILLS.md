# Skill: Blazor Server Web Dashboard (POS.Web)

> **Áp dụng khi:** viết hoặc chỉnh sửa bất kỳ thành phần nào trong
> `src/POS.Web/` — page, component, layout, service, hoặc auth layer.
> Bao gồm: tạo page mới, thêm nav link, chỉnh auth, sử dụng MudBlazor, inject service.

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

## DataTable chuẩn — `PosTableBase<T>` + `pos-table`

> **BẮT BUỘC áp dụng cho mọi DataTable trong POS.Web.**
> Dùng HTML `<table class="pos-table">` thay vì `MudDataGrid` — giải quyết vấn đề column width tự động.

### Pattern: PosTableBase\<T\> — base class DataTable

> Áp dụng khi: tạo mới bất kỳ page nào có bảng dữ liệu có sort + phân trang.

```csharp
// 1. Kế thừa base class
@inherits PosTableBase<MyDto>
@using POS.Web.Components.Shared

// 2. Implement abstract property
protected override IEnumerable<MyDto> SortedFiltered => _sortCol switch
{
    "FieldA" => _sortAsc ? _items.OrderBy(x => x.FieldA) : _items.OrderByDescending(x => x.FieldA),
    _        => _sortAsc ? _items.OrderBy(x => x.Id)     : _items.OrderByDescending(x => x.Id),
};

// 3. Base cung cấp sẵn: _sortCol, _sortAsc, _page, PageSize=10
//    PagedItems, TotalFiltered, PageCount, SortBy(), SI(), FormatVND()
```

```razor
@* 4. Markup chuẩn *@
<div class="pos-table-wrap">
  <table class="pos-table">
    <thead>
      <tr>
        <th @onclick='() => SortBy("FieldA")'>
          Tiêu đề<span class="pos-sort @(_sortCol=="FieldA"?"on":"")">@SI("FieldA")</span>
        </th>
      </tr>
    </thead>
    <tbody>
      @foreach (var item in PagedItems) { <tr>...</tr> }
    </tbody>
  </table>
</div>

@* 5. Pagination — trên và dưới table *@
<MudPagination @bind-Selected="_page" Count="@PageCount" Color="Color.Primary" Size="Size.Small"
               BoundaryCount="1" MiddleCount="5" ShowFirstButton="true" ShowLastButton="true"/>
```

**CSS (đã có sẵn trong `wwwroot/app.css`):**
`.pos-table-wrap` | `.pos-table` | `.pos-sort` | `.pos-sort.on`

**Base class:** `src/POS.Web/Components/Shared/PosTableBase.cs`

**Ví dụ thực tế:**
- `src/POS.Web/Components/Pages/Store/EosShiftsPage.razor`
- `src/POS.Web/Components/Pages/Store/TransactionsPage.razor`
- `src/POS.Web/Components/Pages/Admin/UsersPage.razor`

**Anti-patterns:**
- ❌ Dùng `MudDataGrid` cho DataTable mới — column width bị ép 100%, sort phức tạp, style khó đồng nhất
- ❌ Copy-paste sort/pagination C# vào page — dùng `PosTableBase<T>` thay thế
- ❌ Copy-paste `<style>` CSS table vào page — CSS đã có global trong `app.css`
- ❌ `SearchText` dùng field `string` trực tiếp — dùng property để reset `_page = 1`:
  ```csharp
  private string SearchText { get => _searchField; set { _searchField = value; _page = 1; } }
  ```

---

## Store Selector — Dual Mode (StoreOperator vs Manager/Admin)

> Áp dụng khi: page `StoreAndAbove` cần filter theo cửa hàng — StoreOperator chỉ thấy store của mình, ITOps/Admin chọn tự do.

```razor
@* StoreOperator → ReadOnly TextField (không thể đổi) *@
@* ITOps/Admin → MudAutocomplete để chọn bất kỳ store *@

<MudItem xs="12" sm="6" md="3">
    @if (_isStoreOperator)
    {
        <MudTextField Value="@_filterStoreNo"
                      Label="Mã CH/ST (*)"
                      Variant="Variant.Outlined"
                      Margin="Margin.Dense"
                      ReadOnly="true"
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Store"/>
    }
    else
    {
        <MudAutocomplete T="string"
                         @bind-Value="_filterStoreNo"
                         Label="Mã CH/ST (*)"
                         Placeholder="Tất cả"
                         Variant="Variant.Outlined"
                         Margin="Margin.Dense"
                         SearchFunc="@SearchStoreAsync"
                         Clearable="true"
                         AdornmentIcon="@Icons.Material.Filled.Store"
                         Adornment="Adornment.Start"
                         MinCharacters="0"
                         CoerceValue="true"/>
    }
</MudItem>
```

```csharp
// @code — khởi tạo dual mode
private bool _isStoreOperator;
private string _filterStoreNo = string.Empty;
private IReadOnlyList<string> _userStoreCodes = [];
private List<string> _allStoreCodes = [];

protected override async Task OnInitializedAsync()
{
    var state = await AuthState;
    var json = state.User.FindFirst("store_codes")?.Value;
    _userStoreCodes = string.IsNullOrEmpty(json)
        ? [] : JsonConvert.DeserializeObject<List<string>>(json) ?? [];

    _isStoreOperator = _userStoreCodes.Count > 0;

    if (_isStoreOperator)
    {
        _filterStoreNo = _userStoreCodes[0];  // lock vào store đầu tiên
    }
    else
    {
        // ITOps/Admin: nạp toàn bộ danh sách store cho autocomplete
        var configs = await MdRepo.GetStoreSetConfigAsync();
        _allStoreCodes = configs?
            .Select(c => c.StoreNo).Where(s => !string.IsNullOrEmpty(s))
            .Distinct().OrderBy(s => s).ToList() ?? [];
    }
}

// SearchFunc cho MudAutocomplete — hỗ trợ MinCharacters="0" (hiện toàn bộ khi click vào)
private Task<IEnumerable<string>> SearchStoreAsync(string value, CancellationToken ct)
{
    if (string.IsNullOrWhiteSpace(value))
        return Task.FromResult<IEnumerable<string>>(_allStoreCodes);
    return Task.FromResult(_allStoreCodes
        .Where(s => s.Contains(value, StringComparison.OrdinalIgnoreCase)));
}
```

**Key points:**
- `_isStoreOperator = _userStoreCodes.Count > 0` — flag kiểm soát mode
- `MinCharacters="0"` → dropdown hiện ngay khi focus, không cần gõ ký tự
- `CoerceValue="true"` → giữ giá trị đã gõ dù không chọn từ dropdown
- `Clearable="true"` → cho phép xóa filter (= xem tất cả store)
- Trong `ResetFilterAsync`: `if (!_isStoreOperator) _filterStoreNo = string.Empty;`

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

---

## Pivot Report Table — Pattern báo cáo dạng ma trận

> Áp dụng khi: cần hiển thị báo cáo dạng pivot — hàng = category/entity, cột = ngày/tháng, ô = (số lượng, doanh thu).

### Data model

```csharp
// Record pivot row — Dictionary key = ngày, value = tuple (Qty, Amt)
private record PivotRow(
    string MCHCode,
    string MCHName,
    int    TotalQty,
    double TotalAmt,
    Dictionary<DateTime, (int Qty, double Amt)> ByDate);

private List<DateTime>  _dates     = [];   // danh sách ngày = cột
private List<PivotRow>  _pivotRows = [];   // danh sách hàng
private int             _totalQty;
private double          _totalAmt;
```

### BuildPivot logic

```csharp
private void BuildPivot(DateTime fromDate, DateTime toDate)
{
    // Collect distinct dates có trong data (không nhất thiết liên tiếp)
    _dates = _items
        .Select(x => x.BussinessDate.Date)
        .Distinct().OrderBy(d => d).ToList();

    // Group by entity (MCHCode + MCHName)
    var groups = _items
        .GroupBy(x => new { x.MCHCode, x.MCHName })
        .OrderBy(g => g.Key.MCHCode).ToList();

    _pivotRows = groups.Select(g =>
    {
        var byDate = g
            .GroupBy(x => x.BussinessDate.Date)
            .ToDictionary(
                d => d.Key,
                d => (Qty: d.Sum(x => x.OrderTotal), Amt: d.Sum(x => x.AmountTotal)));

        return new PivotRow(
            MCHCode:  g.Key.MCHCode,
            MCHName:  g.Key.MCHName,
            TotalQty: g.Sum(x => x.OrderTotal),
            TotalAmt: g.Sum(x => x.AmountTotal),
            ByDate:   byDate);
    }).ToList();

    _totalQty = _pivotRows.Sum(r => r.TotalQty);
    _totalAmt = _pivotRows.Sum(r => r.TotalAmt);
}
```

### Pivot table markup

```razor
<div style="overflow-x:auto;">
    <table class="pos-table rpt-pivot-table">
        <thead>
            <tr>
                <th style="width:48px; text-align:center;">STT</th>
                <th style="min-width:200px;">Tên gian hàng</th>
                <th style="min-width:110px; text-align:right;">
                    Số lượng/<br/>Số tiền
                </th>
                @foreach (var date in _dates)
                {
                    <th style="min-width:90px; text-align:right;">
                        @date.ToString("dd/MM")<br/>
                        <span style="font-weight:400; font-size:0.78rem;">(@GetDow(date))</span>
                    </th>
                }
            </tr>
        </thead>
        <tbody>
            @{ int stt = 1; }
            @foreach (var row in _pivotRows)
            {
                <tr>
                    <td style="text-align:center; vertical-align:top;">@(stt++)</td>
                    <td style="vertical-align:top;">
                        <div style="font-weight:600; font-size:0.88rem;">@row.MCHCode</div>
                        <div style="color:#1976D2; font-size:0.82rem;">@row.MCHName</div>
                    </td>
                    <td style="text-align:right; vertical-align:top; white-space:nowrap;">
                        <div>@row.TotalQty.ToString("N0")</div>
                        <div style="color:#1976D2; font-weight:500;">@row.TotalAmt.ToString("N0")</div>
                    </td>
                    @foreach (var date in _dates)
                    {
                        var cellQty = row.ByDate.TryGetValue(date, out var cv) ? cv.Item1 : 0;
                        var cellAmt = row.ByDate.TryGetValue(date, out var ca) ? ca.Item2 : 0.0;
                        <td style="text-align:right; vertical-align:top; white-space:nowrap;">
                            <div>@(cellQty > 0 ? cellQty.ToString("N0") : "")</div>
                            <div style="color:#1976D2;">@(cellAmt > 0 ? cellAmt.ToString("N0") : "")</div>
                        </td>
                    }
                </tr>
            }
        </tbody>
        <tfoot>
            <tr class="rpt-pivot-total">
                <td colspan="2" style="text-align:center; font-weight:700;">Total</td>
                <td style="text-align:right; white-space:nowrap;">
                    <div>@_totalQty.ToString("N0")</div>
                    <div>@_totalAmt.ToString("N0")</div>
                </td>
                @foreach (var date in _dates)
                {
                    var qty = _pivotRows.Sum(r => r.ByDate.TryGetValue(date, out var v) ? v.Item1 : 0);
                    var amt = _pivotRows.Sum(r => r.ByDate.TryGetValue(date, out var va) ? va.Item2 : 0.0);
                    <td style="text-align:right; white-space:nowrap;">
                        <div>@(qty > 0 ? qty.ToString("N0") : "")</div>
                        <div>@(amt > 0 ? amt.ToString("N0") : "")</div>
                    </td>
                }
            </tr>
        </tfoot>
    </table>
</div>
```

### Helper: Day of week

```csharp
private static string GetDow(DateTime d) => d.DayOfWeek switch
{
    DayOfWeek.Monday    => "Mon",
    DayOfWeek.Tuesday   => "Tue",
    DayOfWeek.Wednesday => "Wed",
    DayOfWeek.Thursday  => "Thu",
    DayOfWeek.Friday    => "Fri",
    DayOfWeek.Saturday  => "Sat",
    DayOfWeek.Sunday    => "Sun",
    _ => ""
};
```

**CSS classes cần có (đã khai báo trong `app.css`):**
- `pos-table` — base table style
- `rpt-pivot-table` — thêm border/style riêng cho pivot report
- `rpt-pivot-total` — style hàng Total ở footer

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

---

## Report Page Layout — Header chuẩn cho trang báo cáo

> Áp dụng khi: page xuất báo cáo dạng bảng (có thể in / xuất PDF).

### Cấu trúc markup chuẩn

```razor
<MudPaper Elevation="2" Class="mb-4 pa-4">

    @* 1. Action bar (PDF button bên phải) *@
    <div style="display:flex; justify-content:flex-end; margin-bottom:8px;">
        <MudButton Variant="Variant.Filled"
                   Color="Color.Success"
                   StartIcon="@Icons.Material.Filled.PictureAsPdf"
                   OnClick="@OnExportPdfClick"
                   Size="Size.Small">Xuất PDF</MudButton>
    </div>

    @* 2. User info + timestamp *@
    <div style="display:flex; justify-content:space-between; align-items:flex-start; margin-bottom:12px; font-size:0.82rem; color:#555;">
        <div>
            <div>ID của người dùng: <strong>@_userId</strong></div>
            <div>Tên người dùng: <strong>@_userFullName</strong></div>
        </div>
        <div style="text-align:right;">
            Ngày giờ: <strong>@_printedAt</strong>
        </div>
    </div>

    @* 3. Report title *@
    <div style="text-align:center; margin-bottom:16px;">
        <div style="font-size:1.1rem; font-weight:700; letter-spacing:0.5px; text-transform:uppercase;">
            Tên báo cáo
        </div>
    </div>

    @* 4. Filter summary (store + date range) *@
    <div style="font-size:0.84rem; margin-bottom:12px;">
        <div>
            Cửa hàng:
            <strong>
                @if (!string.IsNullOrEmpty(_reportStoreNo))
                { @($"{_reportStoreNo} – {_reportStoreName}") }
                else
                { @("Tất cả") }
            </strong>
        </div>
        <div>
            Ngày giao dịch:
            <strong>
                @((_filterFromDateNullable ?? DateTime.Today).ToString("dd/MM/yyyy"))
                –
                @((_filterToDateNullable ?? DateTime.Today).ToString("dd/MM/yyyy"))
            </strong>
        </div>
    </div>

    @* 5. Nội dung bảng *@
    @* ... *@

</MudPaper>
```

### State fields cần thêm cho report header

```csharp
// Report header info — lấy sau khi có AuthState
private string _userId       = string.Empty;
private string _userFullName = string.Empty;
private string _printedAt    = string.Empty;  // set sau khi load data xong

// Resolved store info cho header (sau khi load data)
private string _reportStoreNo   = string.Empty;
private string _reportStoreName = string.Empty;
```

```csharp
// Trong OnInitializedAsync — lấy user info từ claims
_userId       = state.User.FindFirst(ClaimTypes.Name)?.Value ?? string.Empty;
_userFullName = state.User.FindFirst("full_name")?.Value ?? string.Empty;

// Trong LoadDataAsync — sau khi có data
_printedAt = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss");
_reportStoreNo   = storeNo;
_reportStoreName = _items.FirstOrDefault(x => x.StoreNo == storeNo)?.StoreName ?? string.Empty;
if (string.IsNullOrEmpty(_reportStoreNo)) _reportStoreName = string.Empty;
```

### PDF export placeholder

Khi chức năng xuất PDF chưa implement:

```csharp
private void OnExportPdfClick()
{
    Snackbar.Add("Chức năng Xuất PDF đang được phát triển.", Severity.Info);
}
```

> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/SalesByCategoryPage.razor`

---

## Shared components có sẵn

| Component / Class | File | Dùng cho |
|---|---|---|
| `PosTableBase<T>` | `Components/Shared/PosTableBase.cs` | Base class cho DataTable — sort/paginate/format |
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
| Bảng dữ liệu có sort / filter / page | `@inherits PosTableBase<T>` + `<table class="pos-table">` + `MudPagination` (xem section DataTable chuẩn) |
| Bảng đơn giản (không sort/page) | `MudTable<T>` |
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

### MudBlazor v9 — breaking changes bắt buộc biết

| Thứ | v8 (sai — không dùng) | v9 (đúng) |
|---|---|---|
| Chart component | `<MudChart ChartType="ChartType.Line">` | `<Line T="double">` hoặc `<Bar T="double">` |
| Series attribute | `ChartSeries<double>="@..."` như HTML attr | `ChartSeries="@..."` với `T="double"` trên tag |
| X-axis labels | `XAxisLabels` | `ChartLabels` |
| Data type | `double[]` | `new ChartData<double>(double[])` |
| Options (line) | `ChartOptions { LineStrokeWidth, YAxisTicks }` | `LineChartOptions { LineStrokeWidth, ShowLegend }` |
| Options (bar) | `ChartOptions { YAxisTicks }` | `BarChartOptions { ShowLegend }` |
| Empty check | `series[0].Data.Length == 0` | bool flag set trong LoadData |
| Chip | `<MudChip Color="...">` | `<MudChip T="string" Color="...">` |

```csharp
// ChartSeries<T> — khai báo đúng v9
private List<ChartSeries<double>> _series =
[
    new ChartSeries<double>
    {
        Name = "Label",
        Data = new ChartData<double>(Array.Empty<double>())  // constructor bắt buộc
    }
];

private readonly LineChartOptions _lineOpts = new() { LineStrokeWidth = 2, ShowLegend = false };
private readonly BarChartOptions  _barOpts  = new() { ShowLegend = false };
private bool _isEmpty;   // kiểm tra empty qua flag — KHÔNG qua .Data.Length
```

### Pattern: Y-axis auto-scale theo dữ liệu thực tế
> Áp dụng khi: chart Bar/Line hiển thị data nhỏ (vài triệu đồng) mà trục Y luôn max=20.

**Nguyên nhân:** `BarChartOptions.YAxisTicks` default = **20** là *khoảng cách giữa tick*, không phải số lượng tick.
Khi data max = 8M và spacing = 20 → MudBlazor vẽ tick 0 và 20 → trục Y nhìn cứng max=20.

**Giải pháp:** tính `YAxisSuggestedMax` và `YAxisTicks` sau khi có data:

```csharp
// Sau khi tính xong mảng values, trước khi set BarChartOptions:
var yMax = CalcYMax(values);
_barOpts = new BarChartOptions { ShowLegend = false, YAxisSuggestedMax = yMax, YAxisTicks = CalcYTick(yMax) };

private static double CalcYMax(double[] values)
{
    var max = values.Length > 0 ? values.Max() : 0;
    if (max <= 0) return 5;
    return Math.Ceiling(max + 2.5);   // buffer ~2.5 đơn vị, làm tròn lên
}

private static int CalcYTick(double yMax)
{
    if (yMax <= 5)  return 1;
    if (yMax <= 10) return 2;
    if (yMax <= 20) return 5;
    return 10;
}
```

> `YAxisSuggestedMax` là "gợi ý" — nếu data vượt qua, MudBlazor tự mở rộng (không clip).
> Ví dụ thực tế: `src/POS.Web/Components/Pages/Store/RevenuePage.razor`

---

## Responsive UI — BẮT BUỘC (mobile + tablet + PC)

> **Chi tiết đầy đủ: `CLAUDE.md §10`** — đọc trước khi tạo hoặc sửa bất kỳ page nào.
> Áp dụng cho mọi viewport: xs (<600px), sm (600–959px), md+ (960px+).

### Quy tắc cốt lõi

| Tình huống | Sai | Đúng |
|---|---|---|
| Header: title + button | `MudStack Row Justify.SpaceBetween` | `div.pos-page-header` + `pos-page-header-title` + `pos-page-header-btn` |
| Header: title + (select + button) ghép cặp | `MudStack Row Justify.SpaceBetween` | `div.pos-page-header` + `div.d-flex align-center gap-2` + `Style="align-self:center"` trên button |
| DataTable trong MudPaper | `<MudPaper Elevation="2">` | `<MudPaper Elevation="2" Style="overflow-x:auto">` |
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
- ❌ Raw SQL trong page/component → phải đi qua Repository hoặc Service
- ❌ Thêm nav link mới mà quên wrap `<AuthorizeView Policy="...">` trong `MainLayout.razor`
- ❌ Dùng `MudStack Row Justify.SpaceBetween` cho header title+button → vỡ layout mobile, button stretch cao bất thường
- ❌ `MudButton` trong `MudStack Row` cạnh `MudSelect` có Label → button stretch theo chiều cao Select+Label → thêm `Style="align-self:center"` vào button
- ❌ DataTable trong `MudPaper` thiếu `Style="overflow-x:auto"` → table bị clip trên mobile
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
- [ ] **Responsive checklist** — xem `CLAUDE.md §10.G`: header dùng `pos-page-header`, DataTable MudPaper có `overflow-x:auto`, chip container có `flex-wrap`, không dùng `MudStack Row Justify.SpaceBetween` cho layout title+controls

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

### Pattern: PosTheme.cs — định nghĩa màu tập trung cho toàn app

> Áp dụng khi: cần đổi màu toàn bộ MudBlazor components (primary, sidebar, appbar, success/error...) mà không sửa từng file Razor.

```csharp
// src/POS.Web/Theme/PosTheme.cs
using MudBlazor;
namespace POS.Web.Theme;

public static class PosTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary          = "#2051A3",
            DrawerBackground = "#1B3A5C",   // sidebar
            AppbarBackground = "#1B3A5C",   // appbar
            Background       = "#F2F4F8",   // nền trang
            Success          = "#27AE60",
            Error            = "#DC3545",
            Warning          = "#F39C12",
            WarningContrastText = "#1A2B45",  // QUAN TRỌNG: warning bg sáng, cần text tối
        },
        Typography = new Typography
        {
            // MudBlazor v9: FontWeight và LineHeight là STRING, không phải int/double
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
            Default = new DefaultTypography { LineHeight = "1.6" },
        },
        Shadows = new Shadow { Elevation = [ "none", /* ... 24 entries */ ] },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" }
    };
}
```

```razor
@* Trong MainLayout.razor và EmptyLayout.razor *@
<MudThemeProvider Theme="@PosTheme.Default"/>
```

**Checklist khi tạo/sửa theme:**
- `FontWeight` và `LineHeight` trong Typography là `string` ("600", "1.6") — không phải `int`/`double`
- `Shadows.Elevation` phải có đúng **25 phần tử** (index 0–24)
- `WarningContrastText` phải là màu tối (#1A2B45) vì Warning (#F39C12) có contrast thấp với trắng
- Sau khi thêm `Theme=` vào `MudThemeProvider`, mọi `Color.Primary`, `Color.Success`... tự đổi màu

**Anti-pattern:**
- ❌ `FontWeight = 700` → compile error (phải là `"700"`)
- ❌ `<MudThemeProvider/>` không có `Theme=` → dùng màu mặc định MudBlazor (tím/xanh MUI)
- ❌ Hardcode màu trong CSS isolation thay vì theme → màu không đồng bộ

> Ví dụ thực tế: `src/POS.Web/Theme/PosTheme.cs`
> Style guide reference: `docs/style-guide.html`

---

## Production deployment — Pattern bắt buộc

### Pattern: Explicit UseRouting() để middleware chạy TRƯỚC routing

> Áp dụng khi: cần middleware tùy chỉnh chạy TRƯỚC endpoint routing (vd: rewrite Host header,
> request transformation). Trong .NET 9/10 `WebApplication`, `UseRouting()` tự động chèn vào
> ĐẦU pipeline trước mọi middleware → mọi rewrite header/path sau đó là quá muộn.

```csharp
// Program.cs — đặt middleware TRƯỚC app.UseRouting() tường minh
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/_framework"))
        ctx.Request.Headers.Host = "localhost"; // rewrite trước routing
    await next();
});

app.UseRouting(); // ← TƯỜNG MINH — vô hiệu hóa auto-UseRouting ở đầu pipeline

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
```

> **Anti-pattern:** Không gọi `app.UseRouting()` tường minh → routing tự động chạy trước mọi thứ.
> Ví dụ thực tế: `src/POS.Web/Program.cs`

---

### Pattern: Fix `_framework/blazor.web.js` 404 từ external IP

> Áp dụng khi: deploy Blazor Server trong Docker/nginx với port mapping (external port ≠ internal
> port), `blazor.web.js` trả 404 từ browser nhưng 200 từ `curl localhost`.

**Root cause:** Trong .NET 10, `_framework/` endpoint được build với host selector = `localhost`.
Request từ browser có `Host: <public-ip>:<port>` → không match → 404.

**Kiểm tra nhanh:**
```bash
# Nếu kết quả khác nhau → đây đúng là lỗi này
curl -s http://localhost:5001/_framework/blazor.web.js                          # → 200
curl -s -H "Host: <ip>:5001" http://localhost:5001/_framework/blazor.web.js    # → 404
```

**Fix trong `Program.cs`** (kết hợp với explicit UseRouting ở trên):
```csharp
app.Use(async (ctx, next) =>
{
    if (ctx.Request.Path.StartsWithSegments("/_framework"))
        ctx.Request.Headers.Host = "localhost";
    await next();
});
app.UseRouting(); // BẮT BUỘC đi kèm — xem pattern explicit UseRouting ở trên
```

> Ví dụ thực tế: `src/POS.Web/Program.cs`

---

### Pattern: nginx config cho Blazor Server

> Áp dụng khi: deploy POS.Web với nginx làm reverse proxy (không có hoặc thay thế Docker).

```nginx
server {
    listen 5001;
    server_name _;

    # WebSocket — BẮT BUỘC cho Blazor SignalR circuit
    proxy_http_version 1.1;
    proxy_set_header Upgrade $http_upgrade;
    proxy_set_header Connection "upgrade";

    proxy_set_header Host $http_host;
    proxy_set_header X-Real-IP $remote_addr;
    proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;

    proxy_read_timeout 300s;   # Blazor long-polling fallback cần timeout dài
    proxy_send_timeout 300s;

    location / {
        proxy_pass http://localhost:8080;
    }
}
```

**Build self-contained cho linux (chạy không cần .NET runtime):**
```bash
dotnet publish src/POS.Web/POS.Web.csproj -c Release -r linux-x64 --self-contained true -o publish/POS.Web
```

> Anti-pattern: Quên `proxy_set_header Upgrade` → SignalR WebSocket không upgrade được →
> Blazor circuit không kết nối → button/event không phản hồi.

---

### Pattern: DataProtection keys trong Docker

> Áp dụng khi: app chạy trong Docker container với non-root user (`USER $APP_UID`).
> ASP.NET Core DataProtection cần ghi key vào `/home/app/.aspnet/DataProtection-Keys`.
> Volume Docker do root tạo → user `app` không ghi được → `CryptographicException` khi encrypt cookie.

```dockerfile
# Dockerfile — TRƯỚC USER $APP_UID
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys \
    && chown -R app:app /home/app/.aspnet

USER $APP_UID
```

> Ví dụ thực tế: `src/POS.Web/Dockerfile`

---

### Pattern: Sidebar 3-cấp — icon chỉ ở cấp 1 và cấp 2, không có ở cấp 3

> Áp dụng khi: thêm sub-group mới vào sidebar hoặc thêm leaf MudNavLink vào sub-group.

```razor
@* Cấp 1 — section (có icon) *@
<MudNavGroup Title="Vận hành" Icon="@Icons.Material.Filled.MonitorHeart" @bind-Expanded="_expandOps">

    @* Cấp 2 — sub-group (có icon) *@
    <MudNavGroup Title="Giám sát" Icon="@Icons.Material.Filled.Monitor" @bind-Expanded="_expandOpsMonitor">
        @* Cấp 3 — leaf link (KHÔNG có icon — chỉ tam giác MudNavLink mặc định) *@
        <MudNavLink Href="/ops/health">System health</MudNavLink>
        <MudNavLink Href="/ops/alerts">Alerts</MudNavLink>
    </MudNavGroup>

    <MudNavGroup Title="Nhật ký" Icon="@Icons.Material.Filled.Article" @bind-Expanded="_expandOpsLog">
        <MudNavLink Href="/ops/logs">Log viewer</MudNavLink>
    </MudNavGroup>

</MudNavGroup>
```

```csharp
// @code — khai báo và UpdateExpanded cho từng sub-group
private bool _expandOps;
private bool _expandOpsMonitor;
private bool _expandOpsLog;

private void UpdateExpanded(string uri)
{
    var u = uri.ToLowerInvariant();
    _expandOpsMonitor = u.Contains("/ops/health") || u.Contains("/ops/alerts") || ...;
    _expandOpsLog     = u.Contains("/ops/logs") || u.Contains("/ops/data-raw-log");
    _expandOps        = _expandOpsMonitor || _expandOpsLog;  // parent tự mở khi có child match
}
```

> Anti-pattern: ❌ Thêm `Icon="..."` vào MudNavLink cấp 3 — cấp 3 chỉ dùng tam giác mặc định.
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
