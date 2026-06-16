# /web-add-store-page — Tạo trang Store mới trong POS.Web

Dùng lệnh này để tạo một trang mới cho phần **Store Dashboard** (`/store/*`).

---

## Cách dùng

```
/web-add-store-page
```

Hoặc cung cấp thông tin luôn:
```
/web-add-store-page TransactionHistory route=/store/transactions services=ICentralSaleRepository
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin (nếu chưa có)
1. Tên trang (PascalCase, ví dụ: `DailyRevenue`, `TransactionHistory`, `ShiftReport`)
2. Route path (ví dụ: `/store/daily-revenue`)
3. Services cần inject — gợi ý từ danh sách sau:
   - `ICentralSaleRepository` — orders, transactions từ DB sales
   - `ICentralMDRepository` — master data (stores, POS config)
   - `ILoyaltyRepository` — loyalty, members, points
   - `ICommonService` — store setup, shift, EOD
   - `IKibanaService` — luôn inject để log
   - `ISnackbar` — luôn inject để notify

### Bước 2 — Tạo file
Tạo: `src/POS.Web/Components/Pages/Store/{TênTrang}Page.razor`

Dùng template chuẩn (CLAUDE.md mục 5):
- `@page "/store/route-path"`
- `@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]`
- `@rendermode InteractiveServer`
- `@using Microsoft.AspNetCore.Authorization`
- `@using MudBlazor`
- `@using POS.Web.Auth`
- Inject đúng services đã chọn + `IKibanaService` + `ISnackbar`
- `[CascadingParameter] Task<AuthenticationState> AuthState`
- Row-level filter: lấy `_userStoreCodes` từ `store_codes` claim
- Loading state: `MudProgressLinear` khi `_loading == true`
- Error state: `MudAlert` khi `_errorMsg != null`
- `OnInitializedAsync` với `try/catch/finally`
- Method `LoadDataAsync()` với placeholder `// TODO: implement`

### Bước 3 — Xác nhận
- Báo đường dẫn file đã tạo
- Nhắc: nếu cần data mới, thêm method vào Repository tương ứng trước khi implement LoadDataAsync

---

## Template tạo ra

```razor
@page "/store/{route}"
@attribute [Authorize(Policy = WebPolicies.StoreAndAbove)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth
@using Newtonsoft.Json

@inject ICentralSaleRepository SaleRepo   // ← thay đúng services
@inject IKibanaService KibanaService
@inject ISnackbar Snackbar

<PageTitle>{TênTrang} – POS Dashboard</PageTitle>

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
    @* TODO: implement UI *@
    <MudText Typo="Typo.h5">{TênTrang}</MudText>
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
            : JsonConvert.DeserializeObject<List<string>>(json) ?? [];
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _errorMsg = "Không thể tải dữ liệu.";
            KibanaService.LogException("{TênTrang}.OnInitialized", "", 0, "", ex.Message);
        }
        finally { _loading = false; }
    }

    private async Task LoadDataAsync()
    {
        // TODO: implement — filter theo _userStoreCodes nếu !empty
        await Task.CompletedTask;
    }
}
```

---

## Lưu ý

- `_userStoreCodes` rỗng = ITOps/Admin (xem tất cả). Khác rỗng = StoreOperator (phải filter).
- KHÔNG tạo raw SQL trong page — phải qua Repository.
- Nếu cần chart: xem CLAUDE.md mục "MudBlazor v9 Breaking Changes" — dùng `<Line T="double">` / `<Bar T="double">`.
