# POS.Web — Blazor Server Dashboard

> Webapp quản trị nội bộ: `src/POS.Web/` — .NET 10, Blazor Server, MudBlazor 9.5.0

> ## ⚡ Tóm tắt luật thép (đọc nhanh)
> **✅ DO:**
> - Trước khi viết markup page/component mới: đọc `.claude/skills/web/SKILLS.md` +
>   `.claude/rules/mudblazor-flat-ui.md` mục 0 (§0 "LUẬT THÉP")
> - Đăng nhập: dùng bridge token qua minimal API endpoint, KHÔNG gọi `SignInAsync` trong
>   InteractiveServer component (§2)
> - Page mới theo template 3-state `_loading`/`_errorMsg`/content + filter theo `store_codes`
>   claim (§5)
> - Inject service trực tiếp qua DI (POS.Application/POS.Infrastructure) — KHÔNG gọi HTTP đến
>   POS.Api (§4)
> - DataTable dùng `MudTable` với `HorizontalScrollbar="true"` (§10.B); page header dùng
>   `div.pos-page-header` (§10.A); mọi component mới tuân Density Standard (§15) và Responsive
>   Checklist (§10.G)
> - Page có Create/Update/Delete: inject `IAuditLogger`, `await AuditLogger.LogAsync(...)` sau
>   mỗi thao tác ghi thành công (§16)
>
> **❌ DON'T:**
> - Cấm dùng `System.Text.Json` — phải `Newtonsoft.Json` (§9, §11)
> - Cấm `MudStack Row="true" Justify.SpaceBetween` cho page header, cấm tự viết
>   `<table class="pos-table">` cho DataTable mới, cấm `MudChart ChartType="..."` (v8 syntax —
>   xem §6 breaking changes v9) (§11)
> - Cấm `ResetValueOnEmptyText="true"` cùng `MinCharacters="0"` trên `MudAutocomplete` (gây circuit
>   crash) — luôn `.Take(N)` trong `SearchFunc` (§13)
> - Cấm quên `@attribute [Authorize(...)]` hoặc `@rendermode InteractiveServer` trên page mới (§11)
>
> *(Chi tiết đầy đủ — bảng/code/lý do — xem các mục đánh số bên dưới, KHÔNG bị đổi.)*

## 🔒 LUẬT THÉP — BẮT BUỘC TUYỆT ĐỐI khi thiết kế UI trang/component mới

> Áp dụng cho **MỌI** page hoặc component UI mới trong `src/POS.Web/` — không có ngoại lệ. Vi
> phạm mục này coi như task **CHƯA XONG** dù build/test xanh.

**Cổng chặn bắt buộc (theo đúng thứ tự):**

1. **TRƯỚC khi viết markup** cho page/component mới → đọc `.claude/skills/web/SKILLS.md` (pattern
   page bắt buộc, roles/policy, loading/error/empty state, DataTable chuẩn) và
   `.claude/rules/mudblazor-flat-ui.md` mục 0 (mapping HTML mockup → MudBlazor Component).
2. **Nếu page có KPI/summary card** → **BẮT BUỘC** dùng đúng "KPI card — khuôn mẫu chuẩn" ở
   `.claude/rules/mudblazor-flat-ui.md` mục 11 — **KHÔNG** tự viết `MudGrid`/`MudPaper` inline tùy
   ý. Dùng `<PosDeltaBadge>` (`src/POS.Web/Components/Shared/PosDeltaBadge.razor`) cho trend/delta —
   **KHÔNG** tự viết `RenderFragment TrendBadge()` cục bộ trong `@code` của page.
3. **Mọi thành phần UI khác** (page header, filter panel, button, table, chip trạng thái, dialog
   xác nhận, typography KPI/card-title/section-label...) → áp dụng đúng checklist tại
   `.claude/rules/mudblazor-flat-ui.md` mục 11.1 và `.claude/skills/web/ui-polish-standard.md`
   mục 1 — **KHÔNG** phát minh class/spacing/màu mới khi đã có sẵn.
4. **Không chắc** một pattern UI đã có chuẩn chưa → tìm trong 2 file rule trên trước, **KHÔNG**
   đoán rồi tự viết CSS/markup mới. Pattern thật sự chưa có → thêm vào đúng file rule **trong
   cùng commit** (không tạo file rule song song khác).
5. Dùng lệnh `/web-add-feature` khi tạo page hoàn toàn mới, `/web-ui-kpi-row` khi chỉ thêm KPI
   row vào page đã có.

> **Toàn bộ quy tắc Theme (màu/Input/Button/Card/Elevation/Sidebar/Density)**: xem
> `.claude/rules/mudblazor-flat-ui.md` — nguồn sự thật duy nhất, đã cập nhật v3 + lịch sử quyết
> định. **KHÔNG** lặp lại nội dung đó ở file này.

---

## 1. Stack & Packages

| Package | Version | Ghi chú |
|---------|---------|---------|
| .NET | 10.0 | `net10.0` target framework |
| MudBlazor | 9.5.0 | UI component library — **v9 có breaking changes** |
| BCrypt.Net-Next | 4.2.0 | Hash mật khẩu DashboardUsers |
| Newtonsoft.Json | 13.0.4 | Serialization — giống toàn solution |

## 2. Kiến trúc Auth

```
DB: RPOSMasterData.dbo.DashboardUsers
  ↓
IWebUserService.ValidateLoginAsync(username, password)
  → BCrypt.Verify(password, hash)
  → trả DashboardUser (Id, Username, Role, StoreCodes, FullName)
  ↓
Login.razor (InteractiveServer — KHÔNG gọi SignInAsync trực tiếp)
  → tạo one-time token → IMemoryCache (TTL 30s)
  → Nav.NavigateTo("/account/signin/{token}", forceLoad: true)
  ↓
GET /account/signin/{token} (minimal API endpoint — HTTP pipeline thật)
  → ctx.SignInAsync(CookieAuth, principal, IsPersistent=true)
  → Redirect "/"
  ↓
Cookie session: 8h, SlidingExpiration, HttpOnly, SameSite=Strict
```

> **Lý do bridge token**: Blazor InteractiveServer chạy trên WebSocket circuit — `HttpContext` đã degraded, gọi `SignInAsync` lúc này throw → circuit crash. Phải thoát ra HTTP pipeline thật để set cookie.

## 3. Roles và Access Rules

| Role | Constant | Policy | Xem được |
|------|----------|--------|---------|
| Vận hành cửa hàng | `WebRoles.StoreOperator` | `WebPolicies.StoreAndAbove` | Store/* (filter theo `store_codes` claim) |
| IT Ops | `WebRoles.ITOps` | `WebPolicies.OpsAndAbove` | Store/* + Ops/* (xem tất cả store) |
| System Admin | `WebRoles.SystemAdmin` | `WebPolicies.AdminOnly` | Tất cả |

```csharp
// src/POS.Web/Auth/WebRoles.cs
WebRoles.StoreOperator = "StoreOperator"
WebRoles.ITOps         = "ITOps"
WebRoles.SystemAdmin   = "SystemAdmin"

WebPolicies.StoreAndAbove = "StoreAndAbove"  // cả 3 role
WebPolicies.OpsAndAbove   = "OpsAndAbove"    // ITOps + SystemAdmin
WebPolicies.AdminOnly     = "AdminOnly"      // SystemAdmin only
```

## 4. Services inject được trong POS.Web

POS.Web đăng ký `AddInfrastructure()` + `AddApplication()` → inject trực tiếp qua DI:

**Từ POS.Infrastructure:**
- `IRedisService` — cache (HashGet/Set, StringGet/Set, KeyExists...)
- `IKibanaService` — structured logging → Elasticsearch
- `IFileLogHelper` — file log fallback
- `IRabbitMQProducer` — message queue
- `IKafkaProducer` — Kafka producer
- `ICentralMDRepository` — master data (store config, POS setup...)
- `ICentralSaleRepository` — sales data (orders, transactions...)
- `ILoyaltyRepository` — loyalty (members, points, wincode...)
- `IOfferStaffRepository` — staff discount
- `IWincodeRepository` — wincode/winlife
- `CentralMDConnectionFactory` — inject concrete (không qua interface)
- `LoyaltyConnectionFactory` — inject concrete (không qua interface)

**Từ POS.Application:**
- `ICommonService` — POS common ops (store setup, shift, EOD...)
- `IHealthCheckService` — kiểm tra sức khỏe hạ tầng
- `IAkaChainLoyaltyService` — FMV/AkaChain loyalty
- `IGotITService` — GotIT voucher partner
- `IUrboxService` — Urbox voucher partner
- `IKafkaService` — Kafka publisher
- `IDataRawService` — file sale processing
- `ISyncDataPosService` — POS sync

**Chỉ trong POS.Web:**
- `IWebUserService` — dashboard user auth (login, get user, get store codes)

## 5. Template Page Component chuẩn

```razor
@page "/store/ten-trang"
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth

@inject ICentralSaleRepository SaleRepo
@inject IKibanaService KibanaService
@inject ISnackbar Snackbar

<PageTitle>Tên trang – POS Dashboard</PageTitle>

@if (_loading)
{
    <MudProgressLinear Indeterminate="true" Color="Color.Primary" Class="mb-4"/>
}
else if (_errorMsg != null)
{
    <MudAlert Severity="Severity.Error">@_errorMsg</MudAlert>
}
else
{
    @* nội dung thật *@
}

@code {
    [CascadingParameter]
    private Task<AuthenticationState> AuthState { get; set; } = null!;

    private bool _loading = true;
    private string? _errorMsg;
    private IReadOnlyList<string> _userStoreCodes = [];

    protected override async Task OnInitializedAsync()
    {
        var state = await AuthState;
        var json = state.User.FindFirst("store_codes")?.Value;
        _userStoreCodes = string.IsNullOrEmpty(json)
            ? []
            : Newtonsoft.Json.JsonConvert.DeserializeObject<List<string>>(json) ?? [];
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

    private async Task LoadDataAsync() { /* ... */ }
}
```

> `_userStoreCodes` rỗng = ITOps/Admin (xem tất cả). Khác rỗng = StoreOperator (filter theo list).

## 6. MudBlazor v9 — Breaking Changes BẮT BUỘC biết

### Charts (thay đổi lớn nhất)

```razor
@* ĐÚNG — v9 *@
@using MudBlazor.Charts

<Line T="double"
      ChartSeries="@_series"
      ChartLabels="@_labels"
      Width="100%" Height="280px"
      ChartOptions="@_lineOpts"/>

<Bar T="double"
     ChartSeries="@_series"
     ChartLabels="@_labels"
     Width="100%" Height="280px"
     ChartOptions="@_barOpts"/>

@* SAI — v8 syntax, KHÔNG dùng *@
@* <MudChart ChartType="ChartType.Line" ChartSeries<double>="..." .../>  *@
```

```csharp
// @code — v9
// ChartSeries<T>.Data là ChartData<T>, KHÔNG phải double[]
private List<ChartSeries<double>> _series =
[
    new ChartSeries<double>
    {
        Name = "Label",
        Data = new ChartData<double>(Array.Empty<double>())  // phải dùng constructor
    }
];

// Options: dùng concrete class ở MudBlazor namespace
private readonly LineChartOptions _lineOpts = new() { LineStrokeWidth = 2, ShowLegend = false };
private readonly BarChartOptions  _barOpts  = new() { ShowLegend = false };

// Kiểm tra empty: dùng bool flag (KHÔNG dùng .Data.Length)
private bool _isEmpty;
// Trong LoadData: _isEmpty = data.Count == 0;
```

| Thứ | v8 (sai) | v9 (đúng) |
|-----|----------|-----------|
| Chart component | `<MudChart ChartType="ChartType.Line">` | `<Line T="double">` hoặc `<Bar T="double">` |
| Series attribute | `ChartSeries<double>="@..."` | `ChartSeries="@..."` (với `T="double"` trên component) |
| X-axis labels | `XAxisLabels` | `ChartLabels` |
| Data type | `double[]` | `ChartData<double>(double[])` |
| Options (line) | `ChartOptions { LineStrokeWidth, YAxisTicks }` | `LineChartOptions { LineStrokeWidth, ShowLegend }` |
| Options (bar) | `ChartOptions { YAxisTicks }` | `BarChartOptions { ShowLegend }` |
| Empty check | `series[0].Data.Length == 0` | bool flag set trong LoadData |

### Chip component

```razor
@* ĐÚNG *@
<MudChip T="string" Color="..." ...>@label</MudChip>

@* SAI (v8) *@
@* <MudChip Color="..." ...>@label</MudChip> *@
```

## 7. Logging convention trong POS.Web

```csharp
// Load data
KibanaService.LogInfo("PageName.LoadData", _userStoreCodes.FirstOrDefault() ?? "all",
    $"Loading data: {count} items");

// Exception
KibanaService.LogException("PageName.MethodName", "", 0, "", ex.Message);
```

## 8. Quy tắc đặt tên

| Thành phần | Convention | Ví dụ |
|-----------|-----------|-------|
| Page component | `{Domain}Page.razor` | `RevenuePage.razor` |
| Folder | `Components/Pages/{Section}/` | `Components/Pages/Store/` |
| Route | `/section/kebab-case` | `/store/daily-revenue` |

## 9. Serialization trong POS.Web

Dùng **Newtonsoft.Json** (`JsonConvert.*`) — KHÔNG dùng `System.Text.Json`.
Nhất quán với POS.Api và POS terminals.

## 10. Responsive UI Standard — BẮT BUỘC với mọi page mới

> Mọi page/component mới trong POS.Web PHẢI tuân theo chuẩn này. Tự áp dụng khi tạo page.

### Breakpoints (MudBlazor built-in)

| Tên | Phạm vi | Target |
|-----|---------|--------|
| **xs** | < 600px | Mobile dọc (iPhone, Android) |
| **sm** | 600–959px | Mobile ngang / Tablet nhỏ |
| **md** | 960px+ | Desktop chuẩn |

### A. Page Header — Title + Action Button

**KHÔNG** dùng `MudStack Row="true" Justify.SpaceBetween` → tiêu đề bị squeeze, văn bản xuống 2 dòng trên mobile.

**DÙNG** `div.pos-page-header` (CSS đã có trong `app.css`):

```razor
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title" Style="font-weight:400">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Size="Size.Small" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small"
               StartIcon="@Icons.Material.Filled.Add"
               Class="pos-page-header-btn">
        Thêm
    </MudButton>
</div>
```

- Desktop (sm+): title bên trái, button bên phải — cùng hàng
- Mobile (xs): title full-width hàng trên, button full-width hàng dưới
- Page chỉ có title (không có button) → dùng `MudText Typo.h5` trực tiếp, không cần `pos-page-header`.
- Chi tiết font-size/font-weight/lịch sử: `.claude/rules/mudblazor-flat-ui.md` mục 6.

### B. DataTable — dùng `MudTable` với `HorizontalScrollbar="true"`

```razor
@* BẮT BUỘC: DataTable dùng MudTable (không tự viết <table class="pos-table">) *@
<MudTable Items="@_items" Hover="true" Striped="true" Dense="true"
          Breakpoint="Breakpoint.Sm" Loading="@_loading"
          HorizontalScrollbar="true">
    <HeaderContent>
        <MudTh><MudTableSortLabel SortBy="new Func<MyDto, object>(x => x.FieldA)">Cột A</MudTableSortLabel></MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Cột A">@context.FieldA</MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager PageSizeOptions="new[] { 10, 20, 50, 100 }"
                       InfoFormat="{first_item}–{last_item} / {all_items} dòng"
                       RowsPerPageString="Số dòng mỗi trang:"/>
    </PagerContent>
</MudTable>
```

> Chi tiết đầy đủ (client-side / server-side / dynamic columns / footer tổng): `.claude/skills/web/datatable.md`.
> Không có `HorizontalScrollbar="true"` → table bị clip trên mobile.
> **Ngoại lệ:** pivot report (cột-ngày động) vẫn dùng `<table class="pos-table rpt-pivot-table">` trong wrapper `overflow-x:auto`.

**Pagination — chuẩn BẮT BUỘC:** `MudTablePager` luôn dùng `PageSizeOptions="new[] { 10, 20, 50, 100 }"`. **Phải bắt đầu bằng `10`** vì `MudTable.RowsPerPage` mặc định = `10`; nếu list không chứa `10`, ô chọn "Số dòng mỗi trang" hiển thị trống / chọn không có tác dụng. KHÔNG hard-set `RowsPerPage="..."` một chiều trên `MudTable` (re-render sẽ reset lựa chọn).

### C. Filter Panel

```razor
@* Nhóm nút cuối filter *@
<MudItem xs="12" sm="12" md="2" Class="d-flex align-center">
    <MudStack Row="true" Spacing="1" Class="w-100">
        <MudButton ... FullWidth="true">Tìm</MudButton>
        <MudButton ... FullWidth="true">Xóa</MudButton>
    </MudStack>
</MudItem>
```

### D. Button Rules

| Tình huống | Rule |
|-----------|------|
| CTA trong page header | Class `pos-page-header-btn` → tự full-width trên xs |
| Nhóm nút Tìm/Xóa trong filter | `MudStack Row Spacing="1" Class="w-100"` + `FullWidth="true"` mỗi nút |
| Icon button trong table row | Không thay đổi — `MudIconButton Size.Small` đủ vùng chạm |
| Button standalone ngoài form | Bọc trong `MudItem xs="12" sm="auto"` hoặc `Class="w-100 w-sm-auto"` |

### E. Chip / Badge Row

Mọi container chip phải có `flex-wrap`:

```razor
@* ĐÚNG *@
<div class="d-flex align-center gap-2 flex-wrap mb-4">
    <MudChip T="string" .../>
</div>

@* SAI — chips tràn ngang trên mobile *@
<div class="d-flex align-center gap-2 mb-4">
    <MudChip T="string" .../>
</div>
```

### F. Sidebar Drawer — Init theo viewport thực

```razor
@inject IBrowserViewportService ViewportService
@implements IAsyncDisposable
```

```csharp
protected override async Task OnAfterRenderAsync(bool firstRender)
{
    if (firstRender)
    {
        var bp = await ViewportService.GetCurrentBreakpointAsync();
        _drawerOpen = bp >= Breakpoint.Md;   // mở sẵn trên desktop, đóng trên mobile
        StateHasChanged();
    }
}

public async ValueTask DisposeAsync()
{
    Nav.LocationChanged -= OnLocationChanged;
}
```

### G. Checklist — kiểm tra trước khi hoàn thành page mới

```
□ Page header có button  → dùng div.pos-page-header (KHÔNG MudStack Row)
□ DataTable → MudTable có HorizontalScrollbar="true" (pivot table thì wrapper overflow-x:auto)
□ Filter panel button group → xs="12" sm="12" md="2" + FullWidth="true"
□ Chip container → có class "flex-wrap"
□ Không hardcode width (px) cho layout — dùng %, MudGrid, flex: 1
□ Summary/info text nhiều phần → d-flex flex-wrap gap-2 (KHÔNG &nbsp;|&nbsp;)
□ Sidebar drawer (MainLayout) → dùng IBrowserViewportService để init
```

## 11. KHÔNG làm những điều sau (POS.Web)

- ❌ Gọi `SignInAsync` trong Blazor InteractiveServer component — dùng bridge token (xem mục 2)
- ❌ Dùng `System.Text.Json` — phải dùng `Newtonsoft.Json`
- ❌ Quên `@rendermode InteractiveServer` trên page có tương tác
- ❌ Quên `@attribute [Authorize(...)]` trên page mới
- ❌ Inject `IDbConnectionFactory` — factory đăng ký là concrete, inject `CentralMDConnectionFactory`
- ❌ Raw SQL trong page/component — phải qua Repository hoặc Service
- ❌ Gọi HTTP đến POS.Api từ POS.Web — inject service trực tiếp qua DI
- ❌ Bỏ qua row-level filter với StoreOperator
- ❌ Dùng `ChartSeries<double>` như attribute HTML trong Razor (v9 syntax sai)
- ❌ Dùng `MudChart ChartType="..."` và `ChartOptions { YAxisTicks, LineStrokeWidth }` — đã đổi trong v9
- ❌ Dùng `MudStack Row="true" Justify.SpaceBetween` cho header title+button — dùng `div.pos-page-header`
- ❌ Tự viết `<table class="pos-table">` cho DataTable mới — dùng `MudTable`
- ❌ MudTable thiếu `HorizontalScrollbar="true"` — table bị clip mobile
- ❌ Chip container không có `flex-wrap` — chips tràn ngang trên mobile
- ❌ `MudTablePager` có `PageSizeOptions` không chứa `10` — ô chọn số dòng/trang hỏng; luôn dùng `{ 10, 20, 50, 100 }`

## 12. Slash Commands (POS.Web)

| Command | Mục đích |
|---------|---------|
| `/web-add-store-page` | Tạo page mới trong Store section |
| `/web-add-ops-page` | Tạo page mới trong Ops section |
| `/web-add-admin-page` | Tạo page mới trong Admin section |
| `/web-add-feature` | Tạo feature đầy đủ (page + service + model) |
| `/web-check-status` | Build + audit trạng thái POS.Web |
| `/web-gen-hash` | Tạo BCrypt hash cho SQL khởi tạo user dashboard |
| `/add-dto-common` | Thêm DTO mới vào POS.Common (xem `.claude/commands/add-dto-common.md`) |

## 13. MudAutocomplete — BẮT BUỘC tránh circuit crash

> Rút ra từ sự cố thực tế: click ô store picker làm **chết Blazor circuit** ("Failed to rejoin / Failed to resume").

- ❌ **KHÔNG** dùng `ResetValueOnEmptyText="true"` cùng `MinCharacters="0"` — text rỗng khi focus → reset value lặp vô hạn → re-render loop → circuit bị tear-down. Dùng `Clearable="true"` cho nút xóa là đủ.
- ✅ **LUÔN `.Take(N)`** (vd 50) trong `SearchFunc` để bound kết quả. `MaxItems` chỉ giới hạn **hiển thị**, KHÔNG giới hạn dữ liệu component xử lý — list nghìn store vẫn được materialize đầy đủ nếu không `.Take()`.
- ✅ Đặt `MaxItems` hợp lý (vd 50) khớp với `.Take()`.
- 📌 Pattern chuẩn `SearchFunc`:
  ```csharp
  private Task<IEnumerable<StoreDto>> SearchStoreAsync(string value, CancellationToken ct)
  {
      IEnumerable<StoreDto> matches = string.IsNullOrWhiteSpace(value)
          ? _allStores
          : _allStores.Where(s =>
              (s.StoreNo?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false) ||
              (s.Name?.Contains(value, StringComparison.OrdinalIgnoreCase) ?? false));
      return Task.FromResult(matches.Take(50));
  }
  ```
- 📌 `Program.cs` đã bật `DetailedErrors` (Dev) + nới `HubOptions.MaximumReceiveMessageSize=512KB`. Khi circuit crash, **đọc server log** để lấy exception thật (client chỉ thấy "Failed to rejoin").

## 14. UI Polish — Chuẩn đồng bộ giao diện

> **Chi tiết đầy đủ: `.claude/skills/web/ui-polish-standard.md`** — đọc trước khi sửa markup
> bất kỳ trang nào nhận yêu cầu "sync UI", "đồng bộ giao diện", "làm đẹp".

**Nguyên tắc cốt lõi:**
- GIỮ NGUYÊN 100% `@code { }` — không thêm method/biến/helper. Chỉ sửa markup Razor.
- Màu chip dùng **ternary inline** tại `Color=` — không thêm helper vào `@code`.
- `div.pos-page-header` **là chuẩn dự án** — KHÔNG đổi sang `MudStack Justify.SpaceBetween`.

**Verification bắt buộc sau mỗi task:**
```powershell
dotnet build src/POS.Web/POS.Web.csproj -nologo -clp:ErrorsOnly   # phải 0 error
dotnet test tests/POS.ContractTests -nologo                         # phải xanh
```

## 15. Density Standard — BẮT BUỘC với mọi component/page mới

> Mục tiêu: **gọn vừa phải, nhất quán** — không nén quá tay; mobile giữ vùng chạm tối thiểu 40px.
> Tự áp dụng — không cần nhắc. (Xem thêm pattern Density trong `.claude/skills/web/theming.md`.)

### Con số chuẩn (Comfortable-tight)

| Thành phần | Desktop | Mobile (xs ≤ 599px) |
|-----------|---------|---------------------|
| **LineHeight** | `1.45` (theme) | `1.5` (CSS override) |
| **MudTable** | `Dense="true"` — luôn | `Dense="true"` — giữ (card view trên mobile) |
| **MudGrid Spacing** | `Spacing="2"` (form/filter), `Spacing="3"` (KPI/chart) | Giống desktop |
| **Form field Margin** | `Margin="Margin.Dense"` — luôn | Giống desktop |
| **MudAppBar** | `Dense="true"` (48px) | `Dense="true"` |
| **MudNavMenu** | `Margin="Margin.Dense"` | Giống desktop |

### Thang spacing markup ưu tiên

| Mục đích | Class dùng | Tránh |
|---------|-----------|-------|
| Separator giữa sections | `mb-4` (24px) | `mb-5`, `mb-6` |
| Filter panel / card inner | `pa-4` (24px) | `pa-5`, `pa-6` |
| Separator phụ / field gap | `mb-3` (16px) | |
| Icon trước text | `mr-2` (8px) | `mr-3`, `mr-4` |
| Flex gap trong row | `gap-2` (8px) | `gap-4` trở lên |

> Không có `pa-5`, `pa-6`, `mb-5`, `mb-6` trong dự án này.

### Filter panel — button alignment chuẩn

MudItem chứa button Tìm/Xóa **phải** dùng `Class="d-flex align-center"` (CSS global tự bottom-align trên sm+).

### KPI card row — equal height chuẩn

Dùng `d-flex flex-wrap` với wrapper `div[flex:1]`. CSS global tự stretch `MudPaper` fill chiều cao đồng nhất
(xem khuôn mẫu chuẩn đầy đủ ở `.claude/rules/mudblazor-flat-ui.md` mục 11 "KPI card").

### Mobile — giữ vùng chạm tối thiểu

CSS global (`app.css`) đã tự xử lý trên `@media (max-width: 599.98px)`:

| Element | Desktop | Mobile |
|---------|---------|--------|
| `MudButton` | 36px | min 40px |
| `MudIconButton` | 36px | 40×40px |
| Dropdown list item | 5px padding | 8px padding |
| Sidebar nav link | 4px padding | 9px padding |
| LineHeight | 1.45 | 1.5 |

**Không tự thêm media query riêng cho từng component** — CSS global đã đủ.

### Cấm

- ❌ `Dense="false"` trên MudTable — mặc định Dense nếu không set, nhưng không đặt ngược lại
- ❌ `MudGrid` không có `Spacing` — luôn đặt `Spacing="2"` hoặc `Spacing="3"`
- ❌ Form field không có `Margin="Margin.Dense"` trong filter panel
- ❌ Hardcode `min-height` hay `height` trên button/input — để CSS global xử lý

## 16. Audit Log — CRUD Operations (BẮT BUỘC với mọi page ghi dữ liệu)

> **Chi tiết đầy đủ: `.claude/skills/web/audit-logging.md`** — đọc file này trước khi tạo
> bất kỳ page nào có thao tác Create / Update / Delete.

**Quy tắc bắt buộc:**
- Mọi page CRUD **BẮT BUỘC** inject `IAuditLogger` và gọi `await AuditLogger.LogAsync(...)` sau
  mỗi thao tác ghi DB thành công. Không log khi thao tác thất bại.
- Serialize bằng **Newtonsoft.Json** — KHÔNG System.Text.Json.
- Form dialog PHẢI trả DTO đầy đủ: `MudDialog.Close(DialogResult.Ok(_model))` — KHÔNG `Ok(true)`.
- Snapshot `oldValue` cho UPDATE: dùng biến `item` đã có trong page — KHÔNG fetch lại DB.
- Chạy migration `src/POS.Web/Auth/migration_dashboard_audit_log.sql` trên `RPOSMasterData`
  TRƯỚC KHI deploy tính năng có audit. Nếu chưa chạy → log fail silently, không crash app.

**Reference implementation:** `src/POS.Web/Components/Pages/Ops/PosDataSetupPage.razor`

**KHÔNG làm:**
- ❌ Gọi `AuditLogger.LogAsync` mà không `await`
- ❌ Log trước khi xác nhận DB op thành công
- ❌ Dialog trả `Ok(true)` — page không có newValue để log UPDATE/CREATE

## 17. Performance, DataTable & Component standards (gộp từ web numbered layer 2026-07-13)

> Các luật bắt buộc dưới đây trước đây nằm rải trong `skills/web/01/02/03/04-*.md` (lớp
> "rules-index" trùng lặp — đã gộp về đây làm canonical, các file numbered đó đã xóa). Code mẫu +
> pattern thực thi tương ứng ở `.claude/skills/web/{datatable,form-input,component-patterns}.md`.

### 17.1 Hiệu năng & anti-crash (bổ sung §13)
- **Redis trong circuit**: LUÔN dùng bản `...Async` (`StringGetAsync`/`HashSetAsync`...) — các
  method sync của `IRedisService` block bằng `.GetAwaiter().GetResult()`, rủi ro treo circuit khi
  Redis chậm.
- **`CancellationToken` cho call dài**: truyền `CancellationToken` vào Service/Repository call
  DB/HTTP tốn thời gian; component tạo `CancellationTokenSource` riêng, `Cancel()` trong
  `Dispose`/`DisposeAsync`, bắt `OperationCanceledException` bỏ qua khi đã dispose.
- **`@key` BẮT BUỘC** trong mọi `@foreach` viết tay sinh UI element (row/card/component con lặp) —
  dùng định danh ổn định (Id/mã, KHÔNG index). (`<MudTable>` tự diffing nội bộ — không cần `@key` thủ công.)
- **`IAsyncDisposable` BẮT BUỘC** cho component đăng ký event vòng đời dài hơn 1 render
  (`NavigationManager.LocationChanged`, `IJSObjectReference`, timer/`PeriodicTimer`) — gỡ đăng ký
  trong `DisposeAsync()`.
- **JS Interop**: 1-shot → static extension trên `IJSRuntime`; giữ state/module → service riêng
  implement `IAsyncDisposable`. CẤM `IJSRuntime.InvokeAsync` rải rác trong `@code` nhiều page cho
  cùng 1 tác vụ; CẤM `alert()`/`confirm()` JS native cho thông báo lỗi (dùng `ISnackbar`).
- **State chia sẻ giữa component trong 1 circuit**: dùng **Scoped Service** (`AddScoped<T>`) —
  KHÔNG `static`/`Singleton` (Blazor Server 1 circuit = 1 user, Singleton bị chia chéo giữa user).

### 17.2 DataTable & Lists — chuẩn hành vi
- **`MudTable` là mặc định BẮT BUỘC** cho danh sách dữ liệu. `MudDataGrid` chỉ khi nghiệp vụ thật
  sự cần grouping động/advanced filter cấp user — xác nhận nhu cầu trước khi phá nhất quán 100% MudTable.
- **`<NoRecordsContent>` BẮT BUỘC** — KHÔNG để bảng trống trơn. Dùng pattern icon +
  `var(--mud-palette-text-secondary)` (`ui-polish-standard.md` §3), KHÔNG hex cứng.
- **Debounce ô search text tự do**: BẮT BUỘC `Immediate="true" DebounceInterval="500"` trên `MudTextField`.
- **`ServerData` (trả `TableData<T>`) BẮT BUỘC** cho danh sách có khả năng vượt 100 dòng — CẤM
  `.ToListAsync()` không giới hạn rồi tự phân trang UI. Truyền `CancellationToken` do `MudTable` cấp
  **tận xuống Repository**. Gắn `Loading="@_loading"`.
- **Row actions**: cột thao tác là **cột cuối**, căn giữa; `MudIconButton Size="Size.Small"` **BẮT
  BUỘC bọc `MudTooltip`** (KHÔNG chỉ `Title=`); màu theo ngữ nghĩa (Xem→Default/Info, Sửa→Primary,
  Xóa→Error); Xóa/Deactive luôn kèm `MudMessageBox @ref` xác nhận; bảng có `OnRowClick` → nút trong
  cell gắn `@onclick:stopPropagation="true"`.
- **CẤM** hiển thị summary dạng text "Tìm thấy X dòng" — số liệu tổng qua **KPI Cards** (mục 11
  `mudblazor-flat-ui.md`).

### 17.3 Chuẩn đặt tên cột DataTable
Tiêu đề `<MudTh>` dùng **tên field tiếng Anh** khớp cột DB/DTO; `DataLabel` trong `<MudTd>` khớp
tiêu đề `<MudTh>`. Áp dụng cho page menu **Vận hành** và **Quản trị**. Mapping ngoại lệ:

| Bảng DB | DB Column | Header cột |
|---|---|---|
| `Store` | `[No]` | `StoreNo` |
| `Store` | `[ClosingMethod]` | `Status` |
| `POSTerminal` | `[No]` | `PosNo` |
| `Branch` | `[No]` | `BranchNo` |

### 17.4 Chuẩn format hiển thị DateTime

| Loại trường | Format | Ví dụ |
|---|---|---|
| Cột datetime trong datatable (Created/Updated/timestamp) | `"yyyy-MM-dd HH:mm:ss"` | `2025-06-25 14:30:00` |
| Ngày thuần (business date, date picker label) | `"dd/MM/yyyy"` | `25/06/2025` |
| Label chart / trục X | `"dd/MM"` | `25/06` |
| Timestamp UI phụ (KPI "Lần cuối", header in/out) | `"HH:mm:ss"` | `14:30:00` |

Nullable → null-coalescing `?.ToString(...) ?? "—"`.

### 17.5 MudBlazor component mapping (nhu cầu chức năng → component)

| Cần làm | MudBlazor component |
|---|---|
| Bảng dữ liệu có sort/filter/page | `MudTable<T>` + `MudTableSortLabel` + `MudTablePager` |
| Bảng server-side paging | `MudTable<T>` với `ServerData` + `@ref` + `ReloadServerData()` |
| Biểu đồ đường / cột | `<Line T="double">` / `<Bar T="double">` (v9 — cần `@using MudBlazor.Charts`) |
| Số liệu tổng quan (KPI card) | `MudPaper` + `.pos-kpi-value`/`.pos-kpi-label` (khuôn mẫu mục 11 `mudblazor-flat-ui.md`) |
| Thông báo popup | `ISnackbar` (`Snackbar.Add(...)`) |
| Dialog xác nhận đơn giản | `MudMessageBox @ref` trong Razor + `await _msgBox!.ShowAsync()` |
| Dialog form đầy đủ | `IDialogService` + `DialogService.ShowAsync<T>()` |
| Input text / Dropdown / Date | `MudTextField<T>` / `MudSelect<T>` / `MudDatePicker` |
| Chip lọc tương tác | `MudChip T="string"` (bắt buộc `T=`) |
| Badge trạng thái tĩnh | `<span class="pos-status-chip pos-status-{success,error,warning,info}">` (§4a mudblazor-flat-ui) |
| Loading | `MudProgressLinear`/`MudProgressCircular Indeterminate="true"` |
| Alert cố định / Card / Paper / Grid | `MudAlert` / `MudCard` / `MudPaper Elevation="2"` / `MudGrid`+`MudItem` |
| Cây phân cấp (duyệt lazy từng cấp) | `MudTreeView<string>` + `ServerData` (pattern: `skills/web/component-patterns.md`) |

> Bảng map theo **nhu cầu chức năng**. Map theo **markup mockup** (`div.sidebar`/`div.card`/
> `button.btn-primary`) → dùng bảng ở `mudblazor-flat-ui.md` mục 0.
