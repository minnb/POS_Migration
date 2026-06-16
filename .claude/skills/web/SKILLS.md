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

## Shared components có sẵn

> **Hiện tại (2026-06-17): chưa có thư mục `Components/Shared/`.**
> Các page hiện dùng MudBlazor components trực tiếp và inline pattern.

**Khi tạo component Shared mới**, đặt tại `src/POS.Web/Components/Shared/` và đặt tên `Pos{Name}.razor`.
Sau khi tạo, cập nhật bảng này với tag và parameters.

| Component | Trạng thái | Ghi chú |
|---|---|---|
| `PosKpiCard` | Chưa tạo | Dùng `MudPaper` + `MudText` inline tạm |
| `PosStatusChip` | Chưa tạo | Dùng `MudChip T="string"` inline tạm |
| `PosPageHeader` | Chưa tạo | Dùng `MudText Typo="Typo.h5"` inline tạm |
| `PosEmptyState` | Chưa tạo | Dùng `MudAlert Severity.Info` inline tạm |
| `PosDateFilter` | Chưa tạo | Dùng `MudChip` filter inline tạm (xem `RevenuePage.razor`) |

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
| Bảng dữ liệu có sort / filter / page | `MudDataGrid<T>` |
| Bảng đơn giản | `MudTable<T>` |
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
- ❌ Raw SQL trong page/component → phải đi qua Repository hoặc Service
- ❌ Thêm nav link mới mà quên wrap `<AuthorizeView Policy="...">` trong `MainLayout.razor`

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
