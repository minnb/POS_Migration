---
name: web-sidebar-nav
description: Logic C# sidebar 3 cấp (MudNavGroup expand/collapse theo route) và breadcrumb động trong MainLayout.razor. Đọc khi thêm nhóm/leaf mới vào sidebar. Phần visual/CSS xem rules/mudblazor-flat-ui.md §5.
---

# Sidebar Nav (MainLayout) — logic C# 3 cấp + Breadcrumb

> Áp dụng khi: thêm nhóm mới vào sidebar hoặc thêm leaf `MudNavLink` vào nhóm. File này chỉ giữ
> **logic C#** (`UpdateExpanded`, breadcrumb map) — cấu trúc 3 cấp/màu/CSS visual đã chốt ở
> `.claude/rules/mudblazor-flat-ui.md` §5, không lặp lại ở đây.

---

## Sidebar nav 3 cấp — logic `UpdateExpanded`

```razor
@* L1 — nhãn section tĩnh (KHÔNG MudNavGroup, KHÔNG icon, KHÔNG click) *@
<div class="pos-nav-section-label">VẬN HÀNH</div>

@* L2 — MudNavGroup có icon riêng theo nhóm + Class="pos-nav-l2" + HideExpandIcon ẩn mũi tên phải *@
<MudNavGroup Title="Giám sát" Icon="@Icons.Material.Outlined.MonitorHeart" Class="pos-nav-l2" HideExpandIcon="true" @bind-Expanded="_expandOpsMonitor">
    @* L3 — leaf link: icon = ChevronRight đồng nhất. BẮT BUỘC Match="NavLinkMatch.All" *@
    <MudNavLink Href="/ops/health" Icon="@Icons.Material.Outlined.ChevronRight" Match="NavLinkMatch.All">System health</MudNavLink>
    <MudNavLink Href="/ops/alerts" Icon="@Icons.Material.Outlined.ChevronRight" Match="NavLinkMatch.All">Alerts</MudNavLink>
</MudNavGroup>

@* Nhóm QUẢN TRỊ — L1 nhãn tĩnh + leaf MudNavLink top-level (KHÔNG MudNavGroup bọc), icon riêng *@
<div class="pos-nav-section-label">QUẢN TRỊ</div>
<MudNavLink Href="/admin/users" Icon="@Icons.Material.Outlined.People" Match="NavLinkMatch.All">Users</MudNavLink>
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
> - ❌ Dùng `MudNavGroup` bọc L1 (kiểu v2) — L1 phải là `<div class="pos-nav-section-label">` tĩnh.
> - ❌ Đặt `ChevronRight` cho `MudNavGroup` L2 (kiểu v2 cũ) — L2 phải có icon Material riêng theo nhóm
>     (`MonitorHeart`/`ReceiptLong`...); chỉ L3 leaf mới dùng `ChevronRight` đồng nhất.
> - ❌ Quên `Class="pos-nav-l2"` trên `MudNavGroup` L2 — CSS indent/font-size L2 chọn qua marker class này.
> - ❌ Bỏ `HideExpandIcon="true"` trên `MudNavGroup` L2 — để mặc định sẽ hiện thêm mũi tên `ArrowDropDown` bên phải, thừa vì đã có icon trái + accordion tự mở theo route.
> - ❌ Xóa `@bind-Expanded` để "tắt tính năng gì đó" — đây là cơ chế accordion tự mở nhánh chứa route
>     đang active và tự đóng nhánh khác; muốn ẩn UI thì sửa `HideExpandIcon`/CSS, không xóa binding.
> - ❌ Thêm `MudNavLink` mới vào markup mà quên thêm route đó vào `UpdateExpanded()` — điều hướng tới route mới sẽ không mở đúng nhánh cha, có thể khiến TOÀN BỘ sidebar collapse (mọi flag đều tính lại từ URI mỗi lần navigate, không giữ trạng thái cũ).
> - ❌ `MudNavLink` thiếu `Match="NavLinkMatch.All"` — mặc định `NavLinkMatch.Prefix` (như `NavLink` gốc) khiến 1 route ngắn (vd `/promotion/coupons`) bị đánh dấu active luôn khi đang ở route dài hơn cùng tiền tố (`/promotion/coupons/issue`) → 2 leaf link cùng sáng active. Áp dụng cho MỌI leaf link, kể cả link chưa có route trùng tiền tố hiện tại (phòng khi thêm route mới sau này).
> Ví dụ thực tế: `src/POS.Web/Components/Layout/MainLayout.razor`

---

## AppBar — Breadcrumb động (thay text tĩnh)

> Áp dụng khi: cần hiển thị vị trí hiện tại (Section / Trang) trong `MudAppBar` thay vì tiêu đề
> app tĩnh.

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
