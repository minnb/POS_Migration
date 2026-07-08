# /web-add-feature — Tạo page mới hoàn chỉnh cho POS.Web

Dùng lệnh này để tạo **bất kỳ page mới nào** trong POS.Web — Store, Ops, hoặc Admin.
Command hỏi theo flow 3 phase, sau đó tạo đủ 3 file (Page + Service + Model).

---

## Cách dùng

```
/web-add-feature
```

Hoặc cung cấp thông tin ngay:
```
/web-add-feature RevenueDetail Store route=/store/revenue-detail services=ICentralSaleRepository
```

---

## Quy trình Claude thực hiện

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
- `IHealthCheckService` ← qua Application
- `ICommonService` ← qua Application

*Từ POS.Application:*
- `ICommonService`, `IHealthCheckService`, `IAkaChainLoyaltyService`
- `IGotITService`, `IUrboxService`, `IKafkaService`

*Chỉ trong POS.Web:*
- `IWebUserService` — dashboard user auth (Admin section)

> `IKibanaService` và `ISnackbar` luôn inject, không cần hỏi.

---

### PHASE 2 — Xác định UI sections

Hỏi từng câu Yes/No, nếu Yes hỏi thêm chi tiết:

**5. Có date filter không?**
- Nếu có: loại filter nào?
  - Chip nhanh (Hôm nay / 7 ngày / 30 ngày)
  - Date picker từ/đến
  - Cả hai

**6. Có KPI row không?**
- Nếu có: bao nhiêu KPI? (2–4)
- Mỗi KPI: tên label + property name + format (`currency` | `number` | `percent`)
- **BẮT BUỘC** sinh theo khuôn mẫu chuẩn ở `/web-ui-kpi-row` (`.claude/commands/web-ui-kpi-row.md`)
  — KHÔNG tự viết `MudGrid`/`MudPaper` tùy ý. Xem chi tiết ở
  `.claude/rules/mudblazor-flat-ui.md` mục 11 "KPI card — khuôn mẫu chuẩn".

**7. Có chart không?**
- Nếu có: loại `Line` / `Bar` / Cả hai
- Tiêu đề chart, trục X (ngày/giờ/cửa hàng...), trục Y (doanh thu/số lượng...)
- Có nhiều series không? Tên từng series?

**8. Có data table không?**
- Nếu có: tên DTO/Model của row, các cột (tên hiển thị + property + kiểu)
- Có search text không? Có phân trang không?

---

### PHASE 3 — Sinh code

Tạo 3 file theo đúng thứ tự:

#### File A — Page Component
`src/POS.Web/Components/Pages/{Section}/{Feature}Page.razor`

Cấu trúc layout (theo thứ tự phần đã chọn):
1. Page header: `MudText Typo.h5` + nút **Làm mới** (nếu phù hợp)
2. Date filter (nếu chọn) — chip nhanh và/hoặc date picker
3. KPI row (nếu chọn) — **BẮT BUỘC** theo khuôn mẫu `/web-ui-kpi-row`: `d-flex flex-wrap` wrapper +
   `.pos-kpi-value`/`.pos-kpi-label` + `PosDeltaBadge` nếu có trend (xem
   `.claude/rules/mudblazor-flat-ui.md` mục 11) — KHÔNG `MudGrid`/`MudPaper` tùy ý
4. Chart (nếu chọn) — `<Line T="double">` / `<Bar T="double">` theo MudBlazor v9
5. Data table (nếu chọn) — `MudTable<T>` (KHÔNG `MudDataGrid` — xem `datatable.md`)

Bắt buộc có đủ theo checklist SKILLS.md:
- `@page`, `@attribute [Authorize(Policy = WebPolicies.XXX)]`, `@rendermode InteractiveServer`
- Inject `{Feature}Service` (local service) + `IKibanaService` + `ISnackbar`
- `[CascadingParameter] Task<AuthenticationState> AuthState` (nếu Store section)
- `_loading`, `_errorMsg`, `_isEmpty` + loading/error/empty state trong markup
- `OnInitializedAsync` với `try/catch/finally`
- `_userStoreCodes` + row-level filter nếu Store section

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

---

### Sau khi tạo xong

1. **Thêm nav link** vào `MainLayout.razor` — trong `<MudNavGroup>` đúng section, wrap `<AuthorizeView Policy="...">`
2. **Implement LoadDataAsync** — thêm method vào Repository nếu cần data mới
3. **Test**: chạy app, đăng nhập, kiểm tra route mới hiển thị đúng với role

---

## Lưu ý quan trọng

- Page gọi `{Feature}Service` — **KHÔNG** gọi Repository trực tiếp từ page
- `{Feature}Model.cs` = ViewModel cho UI — **KHÔNG** phải DTO từ `POS.Common` (DTO đã có ở đó)
- Chart: dùng `<Line T="double">` / `<Bar T="double">` với `new ChartData<double>(arr)` — MudBlazor v9 syntax
- `_userStoreCodes` rỗng = ITOps/Admin (xem tất cả) — Ops/Admin section không cần parse
- Newtonsoft.Json (`JsonConvert.*`) — **KHÔNG** dùng `System.Text.Json`
- File Service + Model đặt trong `Features/` (không phải `Components/`) — tách biệt UI logic
