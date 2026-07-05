# Theming — Custom MudBlazor Theme

> **Áp dụng khi:** cần đổi màu/typography toàn bộ MudBlazor components (primary, sidebar, appbar, success/error...) mà không sửa từng file Razor.

---

## Pattern: PosTheme.cs — định nghĩa màu tập trung cho toàn app

> **Cập nhật 2026-07-04 (v2)** — theo mẫu MudBlazor "Mud Mini" (`docs/web/images/flat1.jpg`):
> sidebar/appbar chuyển sang nền sáng, radius tăng 4-8px → 16px, shadow card chuyển hairline →
> borderless hoàn toàn. Chi tiết đầy đủ + lịch sử quyết định: `CLAUDE.md §14` +
> `.claude/rules/mudblazor-flat-ui.md` (đọc trước nếu cần rationale, file này chỉ tóm tắt pattern).

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
            DrawerBackground = "#FFFFFF",              // sidebar — SÁNG (không phải navy #1B3A5C cũ)
            DrawerText       = "rgba(26,43,69,0.85)",  // chữ tối trên nền sáng
            AppbarBackground = "#FFFFFF",               // appbar — SÁNG, đồng bộ sidebar
            AppbarText       = "#1A2B45",
            Background       = "#F2F4F8",   // nền trang
            Success          = "#27AE60",
            Error            = "#DC3545",
            Warning          = "#F39C12",
            WarningContrastText = "#1A2B45",  // QUAN TRỌNG: warning bg sáng, cần text tối
        },
        Typography = new Typography
        {
            // MudBlazor v9: FontWeight và LineHeight là STRING, không phải int/double
            Default   = new DefaultTypography { FontSize = "0.875rem", LineHeight = "1.6" },
            // BẮT BUỘC override Body1: Default.FontSize KHÔNG cascade xuống Body1
            // Thiếu dòng này → dropdown/picker/list items render 16px (MudBlazor built-in)
            // Cập nhật 2026-07-04: giảm còn 0.75rem (12px, ~15% từ 14px cũ) + FontWeight=400 —
            // chi phối text input MudTextField/MudSelect/MudDatePicker + dropdown/autocomplete
            // popup. KHÔNG ảnh hưởng DataTable (cell MudTable dùng size cố định riêng, không
            // cascade từ Body1) — xem CLAUDE.md §14 mục "Input font-size".
            Body1  = new Body1Typography  { FontSize = "0.75rem", FontWeight = "400" },
            Body2  = new Body2Typography  { FontSize = "0.8125rem" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
        },
        Shadows = new Shadow { Elevation = [ "none", /* E1-E5 = "none" (borderless), E6+ giữ shadow gốc */ ] },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }  // đã tăng từ 4px
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
- Sidebar/AppBar sáng → `DrawerText`/`AppbarText` PHẢI đặt tint tối (không để mặc định trắng —
  chữ trắng trên nền trắng sẽ vô hình)
- Sau khi thêm `Theme=` vào `MudThemeProvider`, mọi `Color.Primary`, `Color.Success`... tự đổi màu

**Anti-pattern:**
- ❌ `FontWeight = 700` → compile error (phải là `"700"`)
- ❌ `<MudThemeProvider/>` không có `Theme=` → dùng màu mặc định MudBlazor (tím/xanh MUI)
- ❌ Hardcode màu trong CSS isolation thay vì theme → màu không đồng bộ
- ❌ Đổi `DrawerBackground`/`AppbarBackground` mà quên đổi `DrawerText`/`AppbarText` theo — chữ
  biến mất trên nền mới

> Ví dụ thực tế: `src/POS.Web/Theme/PosTheme.cs`
> Style guide reference: `docs/style-guide.html`

---

## Pattern: Flat UI v2 — borderless (không còn hairline) + radius 16px

> Áp dụng khi: muốn chuyển toàn app sang phong cách Flat (không viền, không bóng đổ trên
> card/panel). **v2 (2026-07-04)**: đổi từ hairline border (v1) sang borderless hoàn toàn —
> card phân tách với nền chỉ bằng chênh lệch màu Surface `#FFFFFF` vs Background `#F2F4F8`.

**Quy tắc phân vùng elevation:**
- E0–E5: `"none"` (borderless) → card/paper/filter panel/**MudDrawer** (sidebar cũng thuộc nhóm này)
- E6+: GIỮ NGUYÊN shadow gốc → MudPopover (E8), MudDialog (E12) cần nổi lên khỏi nền

```csharp
// PosTheme.cs — giá trị đã áp dụng
LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" },  // tăng từ 4px
Shadows = new Shadow
{
    Elevation =
    [
        "none",                                         // 0
        "none",                                         // 1 — borderless
        "none",                                         // 2 — borderless (card)
        "none",                                         // 3
        "none",                                         // 4
        "none",                                         // 5
        "0 5px 18px rgba(26,43,69,0.15)",              // 6 — UNCHANGED (dropdown base)
        // ... E7-E25 giữ nguyên
    ]
}
```

**Anti-pattern:**
- ❌ Làm phẳng E8 → MudSelect/MudAutocomplete dropdown dính bẹt vào nền
- ❌ Làm phẳng E12 → MudDialog không tách khỏi overlay
- ❌ Tự thêm `border`/`box-shadow` CSS cho MudPaper/MudCard "cho chắc" — đi ngược nguyên tắc
  borderless, nền + radius 16px đã đủ tách khối
- ❌ Hardcode `border-radius:4px` (hay số bất kỳ) inline trên MudBlazor component — lệch với
  `DefaultBorderRadius=16px` toàn app, để theme tự xử lý

---

## Pattern: Button — Outlined mọi nơi (không còn Filled cho CTA)

> Cập nhật 2026-07-04. Chi tiết đầy đủ + lý do: `CLAUDE.md §14 "Quy ước Button"`.

```razor
<MudButton Variant="Variant.Outlined" Color="Color.Primary" Size="Size.Small">CTA chính</MudButton>
<MudButton Variant="Variant.Outlined">Hành động trung tính (Xóa/Hủy)</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Success">Hành động phụ có ngữ nghĩa (Export)</MudButton>
```

- **KHÔNG** còn dùng `Variant.Filled` cho `MudButton` thông thường trong page (kể cả CTA chính) —
  chỉ phân biệt chức năng bằng `Color`, không bằng filled/outline.
- Nút đi kèm `pos-page-header-title` (đã thu nhỏ, xem pattern bên dưới) dùng thêm `Size="Size.Small"`.

---

## Pattern: Page header — title/icon/button thu nhỏ

> Cập nhật 2026-07-04. `.pos-page-header-title` (CSS `app.css`) đã giảm `font-size` xuống
> `1.25rem` (từ mặc định h5 MudBlazor ~1.5rem).

```razor
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Size="Size.Small" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton Variant="Variant.Outlined" Color="Color.Primary" Size="Size.Small"
               StartIcon="@Icons.Material.Filled.Add" Class="pos-page-header-btn">
        Thêm
    </MudButton>
</div>
```

- Icon cạnh title **bắt buộc** `Size="Size.Small"` — mặc định 24px lệch tỷ lệ với title đã thu nhỏ.
- Title mặc định vẫn đậm (`Typography.H5.FontWeight=800`). Muốn chữ "tự nhiên" (không đậm) → thêm
  `Style="font-weight:400"` **cục bộ trên `MudText` đó**, không sửa `.pos-page-header-title` global.

---

## Pattern: Filter panel — nền soft-tint

```razor
<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">
    <MudGrid Spacing="2">@* filter fields *@</MudGrid>
</MudPaper>
```

- Class `pos-filter-panel` (CSS `app.css`, nền `var(--pos-primary-bg)` `#E9EEF8`) — phân biệt
  vùng nhập liệu với card dữ liệu trắng (MudTable) bên dưới, không cần viền.

---

## Pattern: Sidebar — brand header + icon set Outlined

```razor
<MudDrawer @bind-Open="_drawerOpen" Elevation="2" ClipMode="DrawerClipMode.Always">
    <div class="pos-sidebar-brand">
        <MudAvatar Color="Color.Primary" Size="Size.Small">R</MudAvatar>
        <MudText Typo="Typo.subtitle1" Class="pos-sidebar-brand-text">RPOS</MudText>
    </div>
    <MudNavMenu Margin="Margin.Dense">
        <MudNavGroup Title="..." Icon="@Icons.Material.Outlined.Store" @bind-Expanded="...">
            @* Icons.Material.OUTLINED, không phải Filled — nhẹ/mảnh hơn, khớp Mud Mini *@
        </MudNavGroup>
    </MudNavMenu>
</MudDrawer>
```

- **Icon set sidebar dùng `Icons.Material.Outlined.*`** (đổi từ `Filled` 2026-07-04) — áp dụng cho
  MỌI icon trong `MudNavGroup`/`MudNavLink` ở `MainLayout.razor`.
- `div.pos-sidebar-brand` thay thế `MudDrawerHeader` (đã bỏ — text thô không có logo). Tên brand
  ngắn gọn, KHÔNG lặp lại nguyên văn tiêu đề AppBar.
- Active/hover nav item dùng `border-radius: var(--pos-radius-lg)` (12px, không phải `-md` 8px).

---

## Pattern: Density Standard — Comfortable-tight

> Áp dụng khi: cần tối ưu density cho app dashboard (không quá rộng, mobile-safe).

| Thành phần | Giá trị |
|---|---|
| `LineHeight` (theme) | `"1.45"` desktop / `1.5` mobile (CSS var) |
| `MudTable` | `Dense="true"` — luôn |
| `MudGrid Spacing` | `Spacing="2"` (filter), `Spacing="3"` (KPI/chart) |
| Form Margin | `Margin="Margin.Dense"` trong filter panel |
| `MudAppBar` | `Dense="true"` (48px) |
| `MudNavMenu` | `Margin="Margin.Dense"` (2px inter-item) |

**app.css overrides đã có sẵn** (không thêm lại):
- `.mud-list-item` → `padding: 5px` desktop / `8px` mobile
- `.mud-drawer .mud-nav-link` → `padding: 4px; margin: 1px` desktop / `9px; 2px` mobile
- `@media (max-width: 599.98px)` → min-height 40px cho button/icon-button (WCAG 2.5.5)
- `.d-flex.flex-wrap > div > .mud-paper { height: 100% }` → KPI cards equal height

**Anti-pattern:**
- ❌ Thêm lại `@media (max-width: 599.98px)` cho từng component riêng — CSS global đã đủ
- ❌ `MudGrid` không có `Spacing` — tự default về 4 (16px), quá rộng
