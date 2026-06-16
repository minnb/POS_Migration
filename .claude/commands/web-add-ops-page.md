# /web-add-ops-page — Tạo trang Ops mới trong POS.Web

Dùng lệnh này để tạo một trang mới cho phần **Ops Dashboard** (`/ops/*`).
Chỉ ITOps và SystemAdmin được truy cập (`WebPolicies.OpsAndAbove`).

---

## Cách dùng

```
/web-add-ops-page
```

Hoặc cung cấp thông tin luôn:
```
/web-add-ops-page SystemHealth route=/ops/system-health services=IHealthCheckService,IRedisService
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin (nếu chưa có)
1. Tên trang (PascalCase, ví dụ: `SystemHealth`, `LogViewer`, `CacheMonitor`)
2. Route path (ví dụ: `/ops/system-health`)
3. Services cần inject — gợi ý:
   - `IHealthCheckService` — kiểm tra Redis, RabbitMQ, DB connectivity
   - `IRedisService` — xem cache stats
   - `IKibanaService` — luôn inject để log
   - `ICentralMDRepository` — store/POS master data
   - `ISnackbar` — luôn inject

### Bước 2 — Tạo file
Tạo: `src/POS.Web/Components/Pages/Ops/{TênTrang}Page.razor`

Dùng template chuẩn (CLAUDE.md mục 5):
- `@attribute [Authorize(Policy = WebPolicies.OpsAndAbove)]` ← khác Store!
- Không cần row-level filter (`_userStoreCodes`) — ITOps xem tất cả
- Loading + Error state chuẩn
- `OnInitializedAsync` với `try/catch/finally`

### Bước 3 — Xác nhận
Báo đường dẫn file đã tạo.

---

## Template tạo ra

```razor
@page "/ops/{route}"
@attribute [Authorize(Policy = WebPolicies.OpsAndAbove)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth

@inject IHealthCheckService HealthCheck   // ← thay đúng services
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
    private bool _loading = true;
    private string? _errorMsg;

    protected override async Task OnInitializedAsync()
    {
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
        // TODO: implement
        await Task.CompletedTask;
    }
}
```

---

## Lưu ý

- ITOps không có `store_codes` restriction — không cần row-level filter.
- KHÔNG đặt trang Ops ở route `/store/*` — dùng `/ops/*`.
