# /web-add-admin-page — Tạo trang Admin mới trong POS.Web

Dùng lệnh này để tạo một trang mới cho phần **Admin** (`/admin/*`).
Chỉ SystemAdmin được truy cập (`WebPolicies.AdminOnly`).

---

## Cách dùng

```
/web-add-admin-page
```

Hoặc cung cấp thông tin luôn:
```
/web-add-admin-page UserManagement route=/admin/users
```

---

## Quy trình Claude thực hiện

### Bước 1 — Hỏi thông tin (nếu chưa có)
1. Tên trang (PascalCase, ví dụ: `UserManagement`, `SystemConfig`, `AuditLog`)
2. Route path (ví dụ: `/admin/users`)
3. Services cần inject — gợi ý:
   - `IWebUserService` — quản lý DashboardUsers (đọc/tạo/cập nhật)
   - `ICentralMDRepository` — store/config data
   - `IKibanaService` — luôn inject để log
   - `ISnackbar` — luôn inject

### Bước 2 — Tạo file
Tạo: `src/POS.Web/Components/Pages/Admin/{TênTrang}Page.razor`

Dùng template chuẩn (CLAUDE.md mục 5):
- `@attribute [Authorize(Policy = WebPolicies.AdminOnly)]` ← chỉ SystemAdmin!
- Không cần row-level filter
- Loading + Error state chuẩn
- `OnInitializedAsync` với `try/catch/finally`

### Bước 3 — Xác nhận
Báo đường dẫn file đã tạo.

---

## Template tạo ra

```razor
@page "/admin/{route}"
@attribute [Authorize(Policy = WebPolicies.AdminOnly)]
@rendermode InteractiveServer

@using Microsoft.AspNetCore.Authorization
@using MudBlazor
@using POS.Web.Auth

@inject IWebUserService UserService       // ← thay đúng services
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

- Admin section dành cho SystemAdmin only — route `/admin/*`.
- `IWebUserService` chỉ có trong POS.Web (không có trong POS.Api hay POS.Application).
- Trang Users đã có tại `src/POS.Web/Components/Pages/Admin/UsersPage.razor` — không tạo lại.
