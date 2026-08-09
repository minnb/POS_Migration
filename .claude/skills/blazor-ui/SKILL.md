---
name: blazor-ui
description: Trợ lý tạo UI Component (Chart, Table, KPI, Dialog, Grid) và Scaffold Feature mới cho POS.Web theo đúng chuẩn Flat UI v9.
allowed-tools: Read, Write, Grep
argument-hint: [loại-ui: feature|chart|table|kpi|dialog|grid] [tên-file/page]
---

# BLAZOR UI GENERATOR

Skill gộp từ 6 lệnh cũ (`/web-add-feature`, `/web-ui-chart`, `/web-ui-data-table`,
`/web-ui-kpi-row`, `/web-ui-confirm-dialog`, `/web-ui-status-grid`).

## Điều phối — chọn đúng mục theo `$ARGUMENTS`

| Đối số 1 | Việc cần làm | Mục |
|---|---|---|
| `feature` | Tạo page mới hoàn chỉnh (Page + Service + Model) | §A |
| `chart` | Chèn biểu đồ MudBlazor v9 vào page có sẵn | §B |
| `table` | Chèn `MudTable` (search + phân trang) vào page có sẵn | §C |
| `kpi` | Chèn KPI summary row vào page có sẵn | §D |
| `dialog` | Chèn confirm dialog `MudMessageBox @ref` | §E |
| `grid` | Chèn POS status grid (Ops) | §F |

Không truyền đối số → hỏi User muốn làm loại nào, rồi đi đúng 1 mục. **KHÔNG** đọc/áp dụng các mục
còn lại (tiết kiệm context).

**Luật nền tảng áp dụng cho MỌI mục** (đọc trước khi viết markup):
`.claude/rules/blazor-web-app.md` (LUẬT THÉP §0, Responsive §10, Density §15) +
`.claude/rules/mudblazor-flat-ui.md` (mapping mockup → component, Button/Chip/Elevation) +
`.claude/skills/web/SKILLS.md` (index skill con chi tiết).

---

## §A. Tạo page mới hoàn chỉnh (`feature`)

Dùng để tạo **bất kỳ page mới nào** trong POS.Web — Store, Ops, hoặc Admin. Đi theo flow 3 phase,
sau đó tạo đủ 3 file (Page + Service + Model).

```
/blazor-ui feature
/blazor-ui feature RevenueDetail Store route=/store/revenue-detail services=ICentralSaleRepository
```

### PHASE 1 — Xác định page

Hỏi lần lượt (bỏ qua câu đã có trong lệnh):

**1. Tên feature** (PascalCase)
> Ví dụ: `RevenueDetail`, `PosMonitor`, `UserManagement`

**2. Section** — chọn 1 trong 3:

| Section | Policy | Thư mục page | Ghi chú |
|---|---|---|---|
| `Store` | `WebPolicies.StoreAndAbove` | `Pages/Store/` | Cần row-level filter theo store_codes |
| `Ops` | `WebPolicies.OpsAndAbove` | `Pages/Ops/` | ITOps + Admin, xem tất cả store |
| `Admin` | `WebPolicies.AdminOnly` | `Pages/Admin/` | Chỉ SystemAdmin |

**3. Route path**
> Theo convention: `/store/kebab-case`, `/ops/kebab-case`, `/admin/kebab-case`

**4. Services cần inject** — gợi ý từ SKILLS.md:

*Từ POS.Infrastructure:*
- `IRedisService` — cache
- `IKibanaService` — logging (**luôn inject**)
- `IFileLogHelper` — file log fallback
- `ICentralMDRepository` — master data (store config, POS setup...)
- `ICentralSaleRepository` — sales (orders, transactions, revenue...)
- `ILoyaltyRepository` — loyalty, members, points
- `IOfferStaffRepository` — staff discount
- `IWincodeRepository` — wincode / winlife

*Từ POS.Application:*
- `ICommonService`, `IHealthCheckService`, `IAkaChainLoyaltyService`
- `IGotITService`, `IUrboxService`, `IKafkaService`

*Chỉ trong POS.Web:*
- `IWebUserService` — dashboard user auth (Admin section)

> `IKibanaService` và `ISnackbar` luôn inject, không cần hỏi.

### PHASE 2 — Xác định UI sections

Hỏi từng câu Yes/No, nếu Yes hỏi thêm chi tiết:

**5. Có date filter không?**
- Nếu có: loại filter nào? Chip nhanh (Hôm nay / 7 ngày / 30 ngày) / Date picker từ–đến / Cả hai

**6. Có KPI row không?**
- Nếu có: bao nhiêu KPI? (2–4)
- Mỗi KPI: tên label + property name + format (`currency` | `number` | `percent`)
- **BẮT BUỘC** sinh theo khuôn mẫu chuẩn ở **§D** — KHÔNG tự viết `MudGrid`/`MudPaper` tùy ý.
  Xem `.claude/rules/mudblazor-flat-ui.md` mục 11 "KPI card — khuôn mẫu chuẩn".

**7. Có chart không?**
- Nếu có: loại `Line` / `Bar` / Cả hai; tiêu đề chart, trục X, trục Y; nhiều series không?

**8. Có data table không?**
- Nếu có: tên DTO/Model của row, các cột (tên hiển thị + property + kiểu); có search text? phân trang?

### PHASE 3 — Sinh code

Tạo 3 file theo đúng thứ tự:

Cấu trúc layout (theo thứ tự phần đã chọn):
1. Page header: `MudText Typo.h5` + nút **Làm mới** (nếu phù hợp)
2. Date filter (nếu chọn) — chip nhanh và/hoặc date picker
3. KPI row (nếu chọn) — **BẮT BUỘC** theo §D: `d-flex flex-wrap` wrapper + `.pos-kpi-value`/
   `.pos-kpi-label` + `PosDeltaBadge` nếu có trend — KHÔNG `MudGrid`/`MudPaper` tùy ý
4. Chart (nếu chọn) — `<Line T="double">` / `<Bar T="double">` theo MudBlazor v9 (§B)
5. Data table (nếu chọn) — `MudTable<T>` (KHÔNG `MudDataGrid` — §C)

Bắt buộc có đủ theo checklist SKILLS.md:
- `@page`, `@attribute [Authorize(Policy = WebPolicies.XXX)]`, `@rendermode InteractiveServer`
- Inject `{Feature}Service` (local service) + `IKibanaService` + `ISnackbar`
- `[CascadingParameter] Task<AuthenticationState> AuthState` (nếu Store section)
- `_loading`, `_errorMsg`, `_isEmpty` + loading/error/empty state trong markup
- `OnInitializedAsync` với `try/catch/finally`
- `_userStoreCodes` + row-level filter nếu Store section

#### File A — Page Component
`src/POS.Web/Components/Pages/{Section}/{Feature}Page.razor`

```razor
@page "/store/{feature-route}"
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth
@using POS.Web.Features.Store.{Feature}
@using Newtonsoft.Json

@inject {Feature}Service FeatureService
@inject IKibanaService KibanaService
@inject ISnackbar Snackbar

<PageTitle>{Feature} – POS Dashboard</PageTitle>

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
    @* UI sections theo thứ tự đã chọn *@
}

@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    private bool _loading = true;
    private bool _isEmpty;
    private string? _errorMsg;
    private IReadOnlyList<string> _userStoreCodes = [];
    private {Feature}ViewModel _data = new();

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
            KibanaService.LogException("{Feature}Page.OnInitialized", "", 0, "", ex.Message);
        }
        finally { _loading = false; }
    }

    private async Task LoadDataAsync()
    {
        _data = await FeatureService.Load{Feature}Async(
            _userStoreCodes.Count > 0 ? _userStoreCodes : null);
        _isEmpty = /* kiểm tra _data rỗng */;
    }
}
```

#### File B — Local Service
`src/POS.Web/Features/{Section}/{Feature}/{Feature}Service.cs`

```csharp
namespace POS.Web.Features.{Section}.{Feature};

public class {Feature}Service(
    I{Repo}Repository {repo},       // inject đã chọn ở Phase 1
    IKibanaService kibana
)
{
    public async Task<{Feature}ViewModel> Load{Feature}Async(
        IReadOnlyList<string>? storeCodes = null,
        CancellationToken ct = default)
    {
        try
        {
            // TODO: gọi repository với storeCodes filter
            // TODO: transform DB data → ViewModel
            return new {Feature}ViewModel();
        }
        catch (Exception ex)
        {
            kibana.LogException("{Feature}Service.Load{Feature}", "", 0, "", ex.Message);
            return new {Feature}ViewModel();
        }
    }
}
```

> **Lưu ý:** Service này là concrete class (không có interface), inject qua DI bằng Blazor `@inject`.
> Nếu dùng ở nhiều page → thêm `builder.Services.AddScoped<{Feature}Service>()` vào `Program.cs`.

#### File C — ViewModel
`src/POS.Web/Features/{Section}/{Feature}/{Feature}Model.cs`

```csharp
namespace POS.Web.Features.{Section}.{Feature};

// ViewModel thuần — không phải DB entity
public class {Feature}ViewModel
{
    // TODO: thêm properties theo UI đã thiết kế
    // Ví dụ KPI: public decimal TodayRevenue { get; init; }
    // Ví dụ list: public List<{Feature}RowModel> Items { get; init; } = [];
}

// Model cho từng row trong table (nếu có)
public class {Feature}RowModel
{
    // TODO: thêm properties theo cột đã chọn
}
```

### Sau khi tạo xong

1. **Thêm nav link** vào `MainLayout.razor` — đúng section, wrap `<AuthorizeView Policy="...">`
2. **Implement LoadDataAsync** — thêm method vào Repository nếu cần data mới
3. **Test**: chạy app, đăng nhập, kiểm tra route mới hiển thị đúng với role

### Lưu ý quan trọng (§A)

- Page gọi `{Feature}Service` — **KHÔNG** gọi Repository trực tiếp từ page
- `{Feature}Model.cs` = ViewModel cho UI — **KHÔNG** phải DTO từ `POS.Common`
- Chart: `<Line T="double">` / `<Bar T="double">` với `new ChartData<double>(arr)` — v9 syntax
- `_userStoreCodes` rỗng = ITOps/Admin (xem tất cả) — Ops/Admin section không cần parse
- Newtonsoft.Json (`JsonConvert.*`) — **KHÔNG** dùng `System.Text.Json`
- File Service + Model đặt trong `Features/` (không phải `Components/`) — tách biệt UI logic

---

## §B. Chèn biểu đồ MudBlazor v9 (`chart`)

Sinh code đúng syntax v9 — tránh các lỗi breaking changes từ v8.

```
/blazor-ui chart
/blazor-ui chart RevenuePage.razor type=Line title="Doanh thu theo ngày"
```

### Bước 1 — Hỏi thông tin

1. **File page cần thêm vào** — `src/POS.Web/Components/Pages/{Section}/{File}.razor`
2. **Loại biểu đồ**: `Line` (xu hướng theo thời gian) / `Bar` (so sánh) / Cả hai (2 chart trong MudGrid)
3. **Tiêu đề chart** (hiển thị trên `MudText` subtitle)
4. **Trục X** — nhãn là gì? (ngày `dd/MM`, giờ `00h–23h`, cửa hàng, danh mục...)
5. **Trục Y** — đơn vị? (triệu đồng, số đơn, %...) — ghi chú trong tiêu đề nếu cần
6. **Có nhiều series không?** Nếu có: bao nhiêu, tên từng series?

### Bước 2 — Đọc file page hiện tại

Xác định: vị trí chèn (sau KPI row, trước table...), `@using MudBlazor.Charts` đã có chưa,
`_loading`/`_errorMsg` đã khai báo chưa.

### Bước 3 — Sinh code

#### Phần using (thêm vào đầu file nếu chưa có)

```razor
@using MudBlazor.Charts
```

#### Phần Razor — Line chart

```razor
@* ── Line Chart ─────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="pa-4 mb-4">
    <MudText Typo="Typo.subtitle1" Class="mb-3">
        <MudIcon Icon="@Icons.Material.Filled.ShowChart" Size="Size.Small" Class="mr-1"/>
        Doanh thu theo ngày (triệu đồng)     @* ← thay tiêu đề *@
    </MudText>
    @if (_loading)
    {
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="280px"/>
    }
    else if (_chartEmpty)
    {
        <MudAlert Severity="Severity.Info" Dense="true">
            Không có dữ liệu trong khoảng thời gian này.
        </MudAlert>
    }
    else
    {
        <Line T="double"
              ChartSeries="@_lineSeries"
              ChartLabels="@_lineLabels"
              Width="100%" Height="280px"
              ChartOptions="@_lineOpts"/>
    }
</MudPaper>
```

#### Phần Razor — Bar chart

```razor
@* ── Bar Chart ──────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="pa-4 mb-4">
    <MudText Typo="Typo.subtitle1" Class="mb-3">
        <MudIcon Icon="@Icons.Material.Filled.BarChart" Size="Size.Small" Class="mr-1"/>
        Doanh thu theo giờ (triệu đồng)      @* ← thay tiêu đề *@
    </MudText>
    @if (_loading)
    {
        <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="280px"/>
    }
    else if (_barEmpty)
    {
        <MudAlert Severity="Severity.Info" Dense="true">Không có dữ liệu.</MudAlert>
    }
    else
    {
        <Bar T="double"
             ChartSeries="@_barSeries"
             ChartLabels="@_barLabels"
             Width="100%" Height="280px"
             ChartOptions="@_barOpts"/>
    }
</MudPaper>
```

#### Phần @code (thêm vào code block)

```csharp
// ── Chart fields — thêm vào field declarations ───────────────────────
private bool _chartEmpty = true;    // Line
private bool _barEmpty   = true;    // Bar (bỏ nếu không dùng)

private List<ChartSeries<double>> _lineSeries =
[
    new ChartSeries<double>
    {
        Name = "Doanh thu",                              // ← thay tên series
        Data = new ChartData<double>(Array.Empty<double>())
    }
    // thêm series nếu nhiều series:
    // new ChartSeries<double> { Name = "Lợi nhuận", Data = new ChartData<double>(Array.Empty<double>()) }
];

private string[] _lineLabels = [];

private List<ChartSeries<double>> _barSeries =
[
    new ChartSeries<double>
    {
        Name = "Doanh thu",
        Data = new ChartData<double>(Array.Empty<double>())
    }
];

private string[] _barLabels =
    Enumerable.Range(0, 24).Select(h => $"{h:00}h").ToArray();  // ← thay nếu khác trục X

private readonly LineChartOptions _lineOpts = new()
{
    LineStrokeWidth = 2,
    ShowLegend = false,   // true nếu nhiều series
};

private readonly BarChartOptions _barOpts = new()
{
    ShowLegend = false,
};

// ── BuildChartData — gọi trong LoadDataAsync() ────────────────────────
private void BuildChartData(List<SomeDto> data)
{
    _chartEmpty = data.Count == 0;
    if (_chartEmpty) return;

    _lineLabels = data.Select(d => d.Date.ToString("dd/MM")).ToArray();  // ← thay format
    var values  = data.Select(d => Math.Round((double)(d.Amount / 1_000_000m), 1)).ToArray();
    _lineSeries =
    [
        new ChartSeries<double>
        {
            Name = "Doanh thu",
            Data = new ChartData<double>(values)
        }
    ];
}
```

### Bước 4 — Xác nhận

Báo: vị trí cụ thể cần chèn (sau dòng nào), fields nào cần thêm vào `@code`, method
`BuildChartData` cần data từ repository nào.

> Chi tiết đầy đủ breaking changes v8→v9 (bảng so sánh, Y-axis auto-scale...):
> **`.claude/skills/web/charts.md`** — đọc file đó nếu code sinh ra không compile hoặc cần biến
> thể khác (bar-list, nhiều series).

---

## §C. Chèn data table (`table`)

Luật dự án bắt buộc `MudTable` — **KHÔNG** `MudDataGrid` (xem `.claude/rules/blazor-web-app.md` §10.B).

```
/blazor-ui table
/blazor-ui table TransactionPage.razor model=TransactionRowModel
```

### Bước 1 — Hỏi thông tin

1. **File page cần thêm vào** — `src/POS.Web/Components/Pages/{Section}/{File}.razor`
2. **Tên DTO / Model của từng row** (vd `TransactionRowModel`, `OrderSummaryDto`).
   Chưa có → gợi ý tạo `{Feature}Model.cs` trong `Features/{Section}/{Feature}/`
3. **Các cột** — mỗi cột hỏi: tên hiển thị (header), property name, kiểu dữ liệu
   (`string`/`decimal`/`int`/`DateTime`/`bool`), cột nào là **status**, cột nào cần **format**
   (datetime `dd/MM/yyyy HH:mm`, currency VND...)
4. **Có ô search text không?** Nếu có: search trên các property nào?
5. **Có phân trang không?** Nếu có: số row mặc định mỗi trang (10 / 20 / 50)

### Bước 2 — Đọc file page hiện tại

Xác định: vị trí chèn (cuối content, sau chart...), list variable đã có (`_items`, `_data`...),
using namespace còn thiếu.

### Bước 3 — Sinh code

#### Phần Razor (chèn vào markup)

```razor
@* ── Data Table ────────────────────────────────────────────────────── *@
<MudPaper Elevation="2" Class="mt-4 pa-4">
    <div class="d-flex align-center mb-3">
        <MudText Typo="Typo.subtitle1">Danh sách giao dịch</MudText>
        <MudSpacer/>
        <MudTextField @bind-Value="_searchText"
                      Placeholder="Tìm kiếm..."
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      IconSize="Size.Small"
                      Immediate="true"
                      Margin="Margin.Dense"
                      Class="mt-0" Style="max-width:250px"/>
    </div>

    <MudTable Items="@_filteredItems" Hover="true" Striped="true" Dense="true"
              Breakpoint="Breakpoint.Sm" HorizontalScrollbar="true" Loading="@_loading">
        <HeaderContent>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.OrderNo)">OrderNo</MudTableSortLabel></MudTh>
            <MudTh>StoreCode</MudTh>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.SaleDate)">SaleDate</MudTableSortLabel></MudTh>
            <MudTh><MudTableSortLabel SortBy="new Func<TransactionRowModel, object>(x => x.NetAmount)">NetAmount</MudTableSortLabel></MudTh>
            <MudTh>Trạng thái</MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd DataLabel="OrderNo">@context.OrderNo</MudTd>
            <MudTd DataLabel="StoreCode">@context.StoreCode</MudTd>
            <MudTd DataLabel="SaleDate">@context.SaleDate.ToString("yyyy-MM-dd HH:mm:ss")</MudTd>
            <MudTd DataLabel="NetAmount">@context.NetAmount.ToString("N0")</MudTd>
            <MudTd DataLabel="Trạng thái">
                <span class="pos-status-chip @GetStatusChipClass(context.Status)">@context.Status</span>
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudAlert Severity="Severity.Info" Dense="true" Class="ma-2">
                Không có dữ liệu phù hợp.
            </MudAlert>
        </NoRecordsContent>
        <PagerContent>
            <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                           InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                           RowsPerPageString="Số dòng mỗi trang:"/>
        </PagerContent>
    </MudTable>
</MudPaper>
```

> **Pagination chuẩn:** `PageSizeOptions` luôn = `new[] { 10, 20, 50, 100 }` — phải bắt đầu bằng
> `10` vì default `RowsPerPage = 10`; thiếu `10` → ô chọn số dòng/trang hỏng. Chi tiết đầy đủ
> (client/server-side paging, cột động): `.claude/skills/web/datatable.md`.

#### Phần @code (thêm vào code block)

```csharp
// Data list — thêm vào field declarations
private List<TransactionRowModel> _items = [];   // ← thay đúng type
private string _searchText = string.Empty;
private bool _loading = true;

// MudTable không có QuickFilter built-in (khác MudDataGrid) — lọc bằng property tính toán
private List<TransactionRowModel> _filteredItems =>
    string.IsNullOrWhiteSpace(_searchText)
        ? _items
        : _items.Where(x =>
            x.OrderNo.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            || x.StoreCode.Contains(_searchText, StringComparison.OrdinalIgnoreCase)
            // TODO: thêm properties cần search
          ).ToList();

// Badge dot-pill class — xem .claude/rules/mudblazor-flat-ui.md §4a (KHÔNG dùng MudChip cho badge tĩnh)
private static string GetStatusChipClass(string? status) => status switch
{
    "Thành công" or "online"  => "pos-status-success",
    "Lỗi" or "offline"        => "pos-status-error",
    "Cảnh báo" or "warning"   => "pos-status-warning",
    _                         => "pos-status-info"
};

// Trong LoadDataAsync():
// _items = await FeatureService.GetItemsAsync(...);  // TODO: implement
// _loading = false;
```

### Bước 4 — Xác nhận

Báo: vị trí cụ thể cần chèn code (sau dòng nào trong markup), properties cần thêm vào Model nếu
chưa có, `_filteredItems` cần search trên fields nào.

### Lưu ý (§C)

- `_filteredItems` lọc client-side trên data đã load — dataset lớn (>500 rows) nên filter
  server-side qua repository + `MudTable ServerData` (xem `.claude/skills/web/datatable.md`)
- Cột số tiền/ngày format thủ công trong `RowTemplate` (`ToString("N0")`/
  `ToString("yyyy-MM-dd HH:mm:ss")`) — không có `Format=` attribute như `MudDataGrid`
- Badge trạng thái dùng `<span class="pos-status-chip pos-status-{semantic}">` — KHÔNG `MudChip`
  (xem `.claude/rules/mudblazor-flat-ui.md` §4a)
- Cần export Excel → thêm `MudButton` phía trên bảng gọi `ExportExcelAsync()` (implement sau)
- Không inject `IDbConnectionFactory` — data phải đến từ Service hoặc Repository

---

## §D. Chèn KPI row (`kpi`)

> **LUẬT THÉP**: đây là khuôn mẫu **BẮT BUỘC** — xem `.claude/rules/blazor-web-app.md` mục
> "LUẬT THÉP" và `.claude/rules/mudblazor-flat-ui.md` mục 11 "KPI card — khuôn mẫu chuẩn". KHÔNG
> tự viết `MudGrid`/`MudPaper` tùy ý khi thêm KPI row.

```
/blazor-ui kpi
/blazor-ui kpi RevenuePage.razor kpis=3
```

### Bước 1 — Hỏi thông tin

1. **File page cần thêm vào** — `src/POS.Web/Components/Pages/{Section}/{File}.razor`
2. **Có bao nhiêu KPI?** (2, 3, hoặc 4)
3. **Mỗi KPI** — hỏi lần lượt: tên label hiển thị; property name trong ViewModel; format
   (`currency` | `number` | `percent`); màu accent semantic (`Primary` | `Success` | `Error` |
   `Warning` | `Info` | `Tertiary`)
4. **Có icon minh họa trong card không?** (Variant B — chỉ dùng cho Ops/Admin dashboard dạng
   cấu hình/tổng quan, KHÔNG dùng cho report doanh thu)
5. **Có trend so sánh kỳ trước không?** Nếu có: property name của giá trị kỳ trước, và delta dạng
   "điểm %" (percentage-point) hay tăng trưởng % thông thường

### Bước 2 — Đọc file page hiện tại

Xác định: vị trí chèn (sau page header / filter panel, trước bảng/chart); `_data` fields đã có để
không trùng tên; `_Imports.razor` đã có `@using POS.Web.Components.Shared` (để dùng
`<PosDeltaBadge>` không cần `@using` riêng) — nếu chưa, thêm dòng đó vào `_Imports.razor`.

### Bước 3 — Sinh code

#### Variant A — không icon (mặc định, dùng cho report/dashboard số liệu)

```razor
@* ── KPI Cards ─────────────────────────────────────────────────────── *@
<div class="d-flex flex-wrap gap-3 mb-4">
    <div style="flex:1 1 140px">
        <MudPaper Elevation="2" Class="pa-4 text-center" Style="border-left:4px solid var(--mud-palette-primary)">
            <MudText Typo="Typo.h5" Class="pos-kpi-value" Color="Color.Primary">
                @FormatCurrency(_data.TodayRevenue)   @* ← thay đúng property + format helper *@
            </MudText>
            <MudText Typo="Typo.body2" Class="pos-kpi-label" Color="Color.Secondary">Doanh thu hôm nay</MudText>
            @* Nếu có trend so sánh kỳ trước — bỏ khối này nếu không có *@
            <PosDeltaBadge Current="_data.TodayRevenue" Previous="_data.PreviousRevenue"
                           Enabled="_compareEnabled" Class="mt-1"/>
        </MudPaper>
    </div>
    @* ... lặp cho từng KPI, đổi border-left + Color theo semantic đã chọn ... *@
</div>
```

#### Variant B — có icon minh họa (Ops/Admin — vd tổng quan cấu hình/tài khoản)

```razor
<div class="d-flex flex-wrap gap-3 mb-4">
    <div style="flex:1 1 160px">
        <MudPaper Elevation="2" Class="pa-4 pos-kpi-card-icon" Style="border-left:4px solid var(--mud-palette-primary)">
            <div>
                <MudText Typo="Typo.body2" Class="pos-kpi-label" Color="Color.Secondary">Tổng cấu hình</MudText>
                <MudText Typo="Typo.h5" Class="pos-kpi-value" Color="Color.Primary">@_data.TotalCount</MudText>
            </div>
            <MudIcon Icon="@Icons.Material.Filled.Settings" Class="pos-kpi-icon" Style="color:var(--mud-palette-primary)"/>
        </MudPaper>
    </div>
    @* ... lặp cho từng KPI ... *@
</div>
```

#### Phần @code (thêm vào code block)

```csharp
// KPI fields — thêm vào ViewModel hoặc trực tiếp nếu đơn giản
// Chèn vào LoadDataAsync():
// _data.TodayRevenue = summaryResult.TodayRevenue;  // TODO: gọi thật

// Nếu có trend — cần cờ bật/tắt so sánh kỳ trước (thường đi kèm MudSwitch "So sánh kỳ trước")
private bool _compareEnabled;

// Format helpers — thêm vào cuối @code nếu chưa có
private static string FormatCurrency(decimal amount) => amount switch
{
    >= 1_000_000_000m => $"{amount / 1_000_000_000m:N1} tỷ",
    >= 1_000_000m     => $"{amount / 1_000_000m:N0} triệu",
    _                 => $"{amount:N0} ₫"
};

private static string FormatNumber(decimal value)   => value.ToString("N0");
private static string FormatPercent(decimal value)  => $"{value:F1}%";
```

**KHÔNG** viết `RenderFragment TrendBadge()` riêng trong page — luôn dùng
`<PosDeltaBadge Current="..." Previous="..." Enabled="..." LowerIsBetter="..." AsPercentPoint="..."/>`
(component có sẵn tại `Components/Shared/PosDeltaBadge.razor`).

### Bước 4 — Xác nhận

Báo: vị trí cụ thể cần chèn code Razor (dòng bao nhiêu, sau element nào); properties cần thêm vào
ViewModel; method nào trong Service cần cập nhật để trả về KPI data.

### Lưu ý (§D)

- Wrapper KPI row luôn `d-flex flex-wrap gap-3 mb-4` + `div[style="flex:1 1 Npx"]` mỗi card —
  **KHÔNG** dùng `MudGrid`/`MudItem` (đã đổi hẳn sang flex-wrap từ 2026-07-08).
- `flex:1 1 Npx` — N tùy độ dài nội dung: card số ngắn (~120-140px), card tiền tệ dài hơn
  (~150-160px), card có icon (~160px).
- Border-left color dùng CSS var: `--mud-palette-primary`, `--mud-palette-success`,
  `--mud-palette-error`, `--mud-palette-warning`, `--mud-palette-info`, `--mud-palette-tertiary` —
  **KHÔNG** hardcode hex, luôn khớp với `Color=` trên `MudText` giá trị.
- Value **luôn** `Typo="Typo.h5" Class="pos-kpi-value"`; Label **luôn** `Typo="Typo.body2"
  Class="pos-kpi-label"` — không dùng `h4`/`h6`/`caption`.
- Trend/delta → `<PosDeltaBadge>` đã có sẵn trong `Components/Shared/` — không viết lại logic.
- KPI data nên load song song với data khác bằng `Task.WhenAll`.

---

## §E. Chèn confirm dialog (`dialog`)

Sinh code theo pattern bắt buộc `MudMessageBox @ref` — **KHÔNG** dựng dialog component tùy biến
riêng và **KHÔNG** gọi `DialogService.ShowAsync<MudMessageBox>(...)` (xem
`.claude/skills/web/SKILLS.md` mục "Pattern: MudMessageBox @ref" +
`.claude/rules/mudblazor-flat-ui.md` §3 "Bẫy confirm dialog").

```
/blazor-ui dialog
/blazor-ui dialog UsersPage.razor action=DeleteUser severity=Error
```

### Bước 1 — Hỏi thông tin

1. **File cần thêm vào** — `src/POS.Web/Components/Pages/{Section}/{File}.razor`
2. **Tên hành động cần confirm** (vd "xóa user", "reset config", "tắt POS terminal")
3. **Bản chất hành động Yes** — quyết định Variant/Color của `<YesButton>`
   (bảng Button convention `.claude/rules/mudblazor-flat-ui.md` §3):
   - **Phá hủy/không hoàn tác** (xóa, hủy giao dịch, khóa) → `Variant.Outlined` + `Color.Error`
   - **Tích cực/chốt luồng, không phá hủy** (kích hoạt, mở khóa, đồng bộ lại, retry) →
     `Variant.Filled` + `Color.Primary` (hoặc `Color.Success`/`Color.Warning` nếu cần nhấn mạnh
     cảnh báo, vd "Xác nhận kết thúc ngày")
4. **Tên method thực thi sau khi user xác nhận** (vd `DeleteUserAsync`, `ResetConfigAsync`)
5. **Dialog dùng cho 1 hành động cố định hay nhiều hành động khác bản chất** (vd khóa/mở khóa)?
   Nhiều hành động → cần Title/YesText/Color động (xem Bước 3).

### Bước 2 — Đọc file page hiện tại

Xác định: vị trí chèn `<MudMessageBox @ref="...">` — đặt gần đầu content, TRƯỚC mọi `@if` bao
ngoài; đã có field `_confirmBox`/`_confirmMsg` trùng tên chưa (đổi tên nếu page đã có dialog khác).

### Bước 3 — Sinh code

#### Trường hợp tĩnh — 1 hành động cố định

```razor
@* Khai báo trong Razor template — đặt gần đầu content, TRƯỚC mọi @if bao ngoài *@
<MudMessageBox @ref="_confirmBox" Title="Xác nhận xóa user" CancelText="Hủy">
    <MessageContent>@_confirmMsg</MessageContent>
    <YesButton><MudButton Variant="Variant.Outlined" Color="Color.Error">Xóa</MudButton></YesButton>
</MudMessageBox>
```

```csharp
// Thêm vào @code
private MudMessageBox? _confirmBox;
private string _confirmMsg = string.Empty;

private async Task ConfirmDeleteUserAsync(string targetName)
{
    _confirmMsg = $"Bạn có chắc muốn xóa '{targetName}'? Không thể hoàn tác.";
    var ok = await _confirmBox!.ShowAsync();
    if (ok != true) return;

    await DeleteUserAsync(targetName);   // ← thay tên method thực thi
}
```

```razor
@* Thêm vào button trigger trong markup *@
<MudIconButton Icon="@Icons.Material.Filled.Delete"
               Color="Color.Error"
               Size="Size.Small"
               Title="Xóa"
               OnClick="@(() => ConfirmDeleteUserAsync(item.Username))"/>
@* ← thay action và parameter cho đúng *@
```

#### Trường hợp động — dialog dùng chung nhiều hành động khác bản chất (vd khóa/mở khóa)

```razor
<MudMessageBox @ref="_confirmBox" Title="@_confirmTitle" CancelText="Hủy">
    <MessageContent>@_confirmMsg</MessageContent>
    <YesButton>
        <MudButton Variant="@(_confirmYesColor == Color.Success ? Variant.Filled : Variant.Outlined)"
                   Color="@_confirmYesColor">
            @_confirmYesText
        </MudButton>
    </YesButton>
</MudMessageBox>
```

```csharp
private MudMessageBox? _confirmBox;
private string _confirmMsg = string.Empty;
private string _confirmTitle = string.Empty;
private string _confirmYesText = string.Empty;
private Color _confirmYesColor;

private async Task ConfirmToggleAsync(MyItem item)
{
    var locking = item.IsActive;
    _confirmTitle   = locking ? "Xác nhận khóa" : "Xác nhận kích hoạt";
    _confirmMsg     = $"Bạn có chắc muốn {(locking ? "khóa" : "kích hoạt")} '{item.Name}'?";
    _confirmYesText = locking ? "Khóa" : "Kích hoạt";
    _confirmYesColor = locking ? Color.Error : Color.Success;

    var ok = await _confirmBox!.ShowAsync();
    if (ok != true) return;

    await ToggleAsync(item);   // ← thay tên method thực thi
}
```

### Bước 4 — Xác nhận

Báo: đã thêm `<MudMessageBox @ref="_confirmBox">` và method confirm vào file nào; button trigger
chèn vào vị trí nào; nếu là audit CRUD (xóa/khóa/kích hoạt...) → nhắc gọi
`IAuditLogger.LogAsync(...)` sau khi thao tác thành công (`.claude/skills/web/audit-logging.md`).

### Lưu ý (§E)

- **KHÔNG** dùng `IDialogService.ShowMessageBox(...)` — overload đó không tồn tại trong MudBlazor v9.
- **KHÔNG** gọi `DialogService.ShowAsync<MudMessageBox>(title, parameters, options)` — cách này
  render nút Yes bằng markup mặc định, không có `<YesButton>` slot để chỉnh Variant/Color theo bản
  chất hành động. Đây là lỗi có thật đã xảy ra ở 8 page trong dự án.
- Luôn khai báo `<MudMessageBox @ref="_confirmBox">` trực tiếp trong Razor của page/component —
  KHÔNG tạo dialog component riêng (`PosConfirmDialog` hay tương tự) cho confirm đơn giản.
- Chọn Variant/Color theo đúng bảng Button convention — không mặc định mọi Yes button là `Color.Error`.
- Không để method thực thi chạy trước khi `ShowAsync()` trả về — luôn `if (ok != true) return;`.

---

## §F. Chèn POS status grid (`grid`)

Grid hiển thị trạng thái POS terminals cho page Ops, có filter chips (Online/Offline/Warning) và
card grid hoặc data table.

```
/blazor-ui grid
/blazor-ui grid HealthPage.razor layout=card
```

### Bước 1 — Hỏi thông tin

1. **File page cần thêm vào** — `src/POS.Web/Components/Pages/Ops/{File}.razor`
2. **Ngoài status, hiển thị thêm field nào?** (chọn nhiều): POS/Terminal ID, tên cửa hàng, mã cửa
   hàng, địa chỉ IP, thời gian kết nối cuối (Last Seen), phiên bản phần mềm
3. **Layout**: `card` (card grid, tổng quan nhiều terminal) / `table` (MudTable chi tiết có
   sort/filter/page)
4. **Có filter bar theo trạng thái không?** Nếu có: chips Tất cả / Online / Offline / Cảnh báo
   (kèm số đếm)

### Bước 2 — Đọc file page hiện tại

Xác định: vị trí chèn, Model/DTO đã dùng chưa, using namespace thiếu.

### Bước 3 — Sinh code

#### Phần Razor — Filter bar (nếu chọn)

```razor
@* ── Status Filter ──────────────────────────────────────────────────── *@
<MudPaper Elevation="0" Class="mb-3 d-flex align-center gap-2" Style="background:transparent;">
    <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mr-1">Trạng thái:</MudText>
    @foreach (var (label, filter) in _statusFilters)
    {
        var count = filter == null
            ? _allTerminals.Count
            : _allTerminals.Count(t => t.Status == filter);
        <MudChip T="string"
                 Color="@(_statusFilter == filter ? Color.Primary : Color.Default)"
                 Variant="@(_statusFilter == filter ? Variant.Filled : Variant.Outlined)"
                 OnClick="@(() => FilterStatus(filter))"
                 Size="Size.Small">
            @label (@count)
        </MudChip>
    }
</MudPaper>
```

#### Phần Razor — Card grid layout

```razor
@* ── Terminal Card Grid ──────────────────────────────────────────────── *@
@if (_filteredTerminals.Count == 0)
{
    <MudAlert Severity="Severity.Info">Không có terminal nào phù hợp.</MudAlert>
}
else
{
    <MudGrid>
        @foreach (var terminal in _filteredTerminals)
        {
            <MudItem xs="12" sm="6" md="4" lg="3">
                <MudPaper Elevation="2" Class="pa-3"
                          Style="@($"border-left:4px solid {GetStatusBorderColor(terminal.Status)}")">
                    <div class="d-flex align-center justify-space-between mb-1">
                        <MudText Typo="Typo.subtitle2">@terminal.TerminalId</MudText>
                        <MudChip T="string"
                                 Color="@GetStatusChipColor(terminal.Status)"
                                 Size="Size.Small" Variant="Variant.Filled">
                            @terminal.Status
                        </MudChip>
                    </div>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">@terminal.StoreName</MudText>
                    <MudText Typo="Typo.caption" Color="Color.Tertiary" Class="mt-1">
                        @terminal.IpAddress — Last seen: @terminal.LastSeen?.ToString("HH:mm")
                    </MudText>
                </MudPaper>
            </MudItem>
        }
    </MudGrid>
}
```

#### Phần Razor — Table layout

> Dùng `MudTable` (KHÔNG `MudDataGrid`) — luật bắt buộc dự án, xem
> `.claude/rules/blazor-web-app.md` §10.B + `.claude/skills/web/datatable.md`.

```razor
@* ── Terminal Table ──────────────────────────────────────────────────── *@
<MudTable Items="@_filteredTerminals" Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<PosTerminalModel, object>(x => x.TerminalId)">Terminal ID</MudTableSortLabel></MudTh>
        <MudTh>Cửa hàng</MudTh>
        <MudTh>IP</MudTh>
        <MudTh>Last Seen</MudTh>
        <MudTh>Trạng thái</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Terminal ID">@context.TerminalId</MudTd>
        <MudTd DataLabel="Cửa hàng">@context.StoreCode</MudTd>
        <MudTd DataLabel="IP">@context.IpAddress</MudTd>
        <MudTd DataLabel="Last Seen">@(context.LastSeen?.ToString("yyyy-MM-dd HH:mm:ss") ?? "—")</MudTd>
        <MudTd DataLabel="Trạng thái">
            <span class="pos-status-chip @GetStatusChipClass(context.Status)">@context.Status</span>
        </MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudAlert Severity="Severity.Info" Dense="true" Class="ma-2">
            Không có terminal nào.
        </MudAlert>
    </NoRecordsContent>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                       InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                       RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

#### Phần @code (thêm vào code block)

```csharp
// ── Terminal fields ───────────────────────────────────────────────────
private List<PosTerminalModel> _allTerminals     = [];
private List<PosTerminalModel> _filteredTerminals = [];
private string? _statusFilter;   // null = Tất cả

private readonly (string Label, string? Filter)[] _statusFilters =
[
    ("Tất cả",   null),
    ("Online",   "online"),
    ("Offline",  "offline"),
    ("Cảnh báo", "warning"),
];

// ── Filter ────────────────────────────────────────────────────────────
private void FilterStatus(string? status)
{
    _statusFilter     = status;
    _filteredTerminals = status == null
        ? _allTerminals
        : _allTerminals.Where(t => t.Status == status).ToList();
}

// ── Color helpers ────────────────────────────────────────────────────
private static Color GetStatusChipColor(string? status) => status switch
{
    "online"  => Color.Success,
    "offline" => Color.Error,
    "warning" => Color.Warning,
    _         => Color.Default
};

private static string GetStatusBorderColor(string? status) => status switch
{
    "online"  => "var(--mud-palette-success)",
    "offline" => "var(--mud-palette-error)",
    "warning" => "var(--mud-palette-warning)",
    _         => "var(--mud-palette-divider)"
};

// Dùng cho badge dot-pill trong MudTable — xem .claude/rules/mudblazor-flat-ui.md §4a
private static string GetStatusChipClass(string? status) => status switch
{
    "online"  => "pos-status-success",
    "offline" => "pos-status-error",
    "warning" => "pos-status-warning",
    _         => "pos-status-info"
};

// ── Trong LoadDataAsync() ────────────────────────────────────────────
// _allTerminals      = await FeatureService.GetTerminalsAsync(ct);  // TODO: implement
// _filteredTerminals = _allTerminals;
// _isEmpty = _allTerminals.Count == 0;
```

#### Model (tạo nếu chưa có)

```csharp
// src/POS.Web/Features/Ops/{Feature}/PosTerminalModel.cs
namespace POS.Web.Features.Ops.{Feature};

public class PosTerminalModel
{
    public string TerminalId { get; init; } = string.Empty;
    public string StoreCode  { get; init; } = string.Empty;
    public string StoreName  { get; init; } = string.Empty;
    public string? IpAddress { get; init; }
    public string Status     { get; init; } = "offline";   // "online"|"offline"|"warning"
    public DateTime? LastSeen { get; init; }
    public string? Version   { get; init; }
    // thêm fields đã chọn ở Bước 1
}
```

### Bước 4 — Xác nhận

Báo: file đã chỉnh sửa và vị trí chèn; Model cần tạo (nếu chưa có); method trong Service/Repository
cần implement để lấy terminal status.

### Lưu ý (§F)

- Page Ops không cần row-level filter — ITOps và Admin xem tất cả terminal
- Status value: dùng lowercase `"online"` / `"offline"` / `"warning"` nhất quán với color helpers
- Card grid phù hợp khi có < 50 terminal; table tốt hơn khi nhiều hơn
- `FilterStatus()` filter client-side — phù hợp vì data terminal load 1 lần
