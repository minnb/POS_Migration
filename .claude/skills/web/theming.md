# Theming — Custom MudBlazor Theme

> **Áp dụng khi:** cần đổi màu/typography toàn bộ MudBlazor components (primary, sidebar, appbar, success/error...) mà không sửa từng file Razor.

---

## Pattern: PosTheme.cs — định nghĩa màu tập trung cho toàn app

> **v3 — cập nhật 2026-07-05**, theo mockup `docs/web/theme/theme_html.html` (do người dùng cung
> cấp): sidebar navy đậm, card có shadow thật, radius 2 cấp, Button `Filled` cho CTA. Thay hoàn
> toàn cho bản v2 (2026-07-04, "Mud Mini": sidebar sáng, card borderless, radius 16px, Button
> `Outlined` mọi nơi — đã lỗi thời). Chi tiết đầy đủ + lịch sử quyết định (kể cả các đề xuất đã
> cân nhắc và loại bỏ): `.claude/skills/web/ui-polish-standard.md` + `.claude/rules/mudblazor-flat-ui.md` §3/§15 (đọc trước
> nếu cần rationale, file này chỉ tóm tắt pattern).

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
            Primary          = "#2660A4",   // mockup --steel (Darken #1E50A0, Lighten #3D8FD9)
            Secondary        = "#4A6070",   // mockup --gray5
            Tertiary         = "#6040A8",   // mockup --purple
            DrawerBackground = "#0D1B2A",   // sidebar — NAVY ĐẬM (không phải #FFFFFF)
            DrawerText       = "rgba(255,255,255,0.6)",
            DrawerIcon       = "rgba(255,255,255,0.6)",
            AppbarBackground = "#FFFFFF",   // topbar vẫn sáng
            AppbarText       = "#1A2B45",
            Background       = "#F0F4F8",   // mockup --gray1
            Surface           = "#FFFFFF",
            Success          = "#1F7A4A",
            Error            = "#B52B27",
            Warning          = "#D4860A",
            Info             = "#3D8FD9",
            WarningContrastText = "#1A2B45",  // warning bg sáng, cần text tối
        },
        Typography = new Typography
        {
            // MudBlazor v9: FontWeight và LineHeight là STRING, không phải int/double.
            // BẮT BUỘC set FontFamily trên TỪNG variant — MudBlazor sinh CSS var riêng cho mỗi
            // variant (--mud-typography-h5-family, --mud-typography-body1-family...), KHÔNG cascade
            // từ Default. Chỉ set Default.FontFamily → H5/H6/Subtitle1/Body1/Body2/Caption/Button
            // vẫn giữ font mặc định MudBlazor. Xem PosTheme.cs thật: mọi variant đều set FontFamily.
            Default   = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize   = "0.8125rem",   // 13px — base mockup
                LineHeight = "1.5",         // mockup body{line-height:1.5}
            },
            H5 = new H5Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontWeight = "800", LetterSpacing = "-0.02em" },
            H6 = new H6Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontWeight = "600" },
            // BẮT BUỘC override Body1: Default.FontSize KHÔNG cascade xuống Body1
            // Thiếu dòng này → dropdown/picker/list items render 16px (MudBlazor built-in)
            // 0.78125rem = 12.5px, khớp mockup .input,.select,.textarea{font-size:12.5px}
            Body1  = new Body1Typography  { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontSize = "0.78125rem", FontWeight = "400" },
            Body2  = new Body2Typography  { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontSize = "0.8125rem" },
            Button = new ButtonTypography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"], FontSize = "0.75rem", FontWeight = "600", TextTransform = "none" },
            // ... H1..H4/Subtitle2/Caption/Overline: cũng set FontFamily (xem PosTheme.cs đầy đủ)
        },
        Shadows = new Shadow
        {
            Elevation =
            [
                "none",                              // 0
                "none",                              // 1 — flat (filter panel/toolbar)
                "0 2px 8px rgba(0,0,0,0.08)",        // 2 — card, shadow thật (mockup --shadow)
                "0 2px 8px rgba(0,0,0,0.08)",        // 3
                "0 4px 20px rgba(0,0,0,0.12)",       // 4 — shadow mạnh hơn (mockup --shadow-lg)
                "0 4px 20px rgba(0,0,0,0.12)",       // 5
                // ... E6-E25 giữ nguyên thang shadow cũ cho overlay/dropdown/dialog
            ]
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",   // Paper/Card/Dialog (control Button/Chip/Input ép 8px qua CSS)
            DrawerWidthLeft     = "260px",
            AppbarHeight        = "50px",   // khớp mockup .topbar{height:50px} — KHÔNG dùng Dense (xem SKILLS.md §Breadcrumb)
        }
    };
}
```

```razor
@* Trong MainLayout.razor và EmptyLayout.razor *@
<MudThemeProvider Theme="@PosTheme.Default"/>
```

**Checklist khi tạo/sửa theme:**
- `FontWeight` và `LineHeight` trong Typography là `string` ("600", "1.5") — không phải `int`/`double`
- `Shadows.Elevation` phải có đúng **25 phần tử** (index 0–24)
- `WarningContrastText` phải là màu tối (`#1A2B45`) vì Warning có contrast thấp với trắng
- Sidebar/AppBar khác nền → `DrawerText`/`DrawerIcon`/`AppbarText` PHẢI đặt màu tương phản đúng
  (sidebar navy → chữ trắng translucent; appbar trắng → chữ tối)
- Set `FontFamily` tường minh trên TỪNG typography variant nếu cần đổi font toàn app — MudBlazor
  sinh CSS variable riêng cho mỗi variant (`--mud-typography-h5-family`...), KHÔNG cascade từ
  `Default`
- Sau khi thêm `Theme=` vào `MudThemeProvider`, mọi `Color.Primary`, `Color.Success`... tự đổi màu

**Anti-pattern:**
- ❌ `FontWeight = 700` → compile error (phải là `"700"`)
- ❌ `<MudThemeProvider/>` không có `Theme=` → dùng màu mặc định MudBlazor (tím/xanh MUI)
- ❌ Hardcode màu trong CSS isolation thay vì theme → màu không đồng bộ
- ❌ Đổi `DrawerBackground`/`AppbarBackground` mà quên đổi `DrawerText`/`AppbarText` theo — chữ
  biến mất trên nền mới

> Ví dụ thực tế: `src/POS.Web/Theme/PosTheme.cs`
> Mockup gốc: `docs/web/theme/theme_html.html`

---

## Pattern: Elevation & Border-radius — shadow thật, radius 2 cấp

> Thay cho pattern v2 "Flat UI — borderless + radius 16px" (đã loại bỏ). v3 dùng shadow thật cho
> card, và radius **2 cấp riêng biệt** — không còn 1 giá trị dùng chung cho mọi component.

**Quy tắc phân vùng elevation:**

| Loại component | Elevation | Ý nghĩa |
|---|---|---|
| Filter panel / toolbar | `1` | Flat, `"none"` |
| Card / KPI card / data table wrap | `2` | Shadow thật `0 2px 8px rgba(0,0,0,.08)` |
| Login card / callout nổi bật | `4` | Shadow mạnh hơn `0 4px 20px rgba(0,0,0,.12)` |
| MudPopover / MudSelect / MudAutocomplete / MudMenu | **KHÔNG hạ** — giữ E8 mặc định | Cần nổi lên khỏi nền |
| MudDialog | **KHÔNG hạ** — giữ E12 mặc định | Phải nổi trên overlay |

**Border-radius — 2 cấp, KHÔNG dùng chung 1 giá trị:**
- `DefaultBorderRadius = "12px"` (theme, `PosTheme.cs`) — áp dụng Paper/Card/Dialog/Popover/Menu
- Control (Button/Chip/Input): ép riêng `8px` qua CSS (`.mud-button-root`, `.mud-chip`,
  `.mud-input-outlined-border` trong `app.css`) — **không** đổi qua `DefaultBorderRadius` (sẽ ảnh
  hưởng luôn Paper/Card)
- CSS token: `--pos-radius-sm: 8px` (control) | `--pos-radius-lg: 12px` (card, active nav)

```csharp
// PosTheme.cs
LayoutProperties = new LayoutProperties { DefaultBorderRadius = "12px" },
```

**Anti-pattern:**
- ❌ Làm phẳng E8 → MudSelect/MudAutocomplete dropdown dính bẹt vào nền
- ❌ Làm phẳng E12 → MudDialog không tách khỏi overlay
- ❌ Tự thêm `border`/`box-shadow` CSS cho MudPaper/MudCard "cho chắc" — dùng thuộc tính
  `Elevation`, theme đã cấu hình đúng shadow cho từng mức
- ❌ Hardcode `border-radius` (bất kỳ số nào) inline trên component MudBlazor — theme/CSS token
  đã xử lý; card dùng `12px` (theme), control dùng `8px` (CSS ép riêng), không trộn lẫn

---

## Pattern: Button — Filled cho CTA, Outlined cho phần còn lại

> Thay cho pattern v2 "Button — Outlined mọi nơi" (đã loại bỏ). Theo mockup, `.btn-primary` là nền
> đặc màu (Filled), không phải viền trong suốt. Chi tiết đầy đủ + lý do:
> `.claude/rules/mudblazor-flat-ui.md` §3 "Button — Filled cho CTA, Outlined cho phần còn lại".

| Loại hành động | Variant | Color | Ví dụ |
|---|---|---|---|
| CTA chính (Lưu, Thêm mới, Cập nhật, Tìm) | `Filled` | `Primary` | "Lưu", "Thêm mới", "Tìm" |
| Hành động tích cực chốt luồng (Duyệt, Kích hoạt, Xác nhận) | `Filled` | `Success` | "Duyệt", "Kích hoạt" |
| Phá hủy/không hoàn tác (Xóa, Khóa, Hủy giao dịch) | `Outlined` | `Error` | "Xóa" |
| Trung tính (Hủy/Đóng dialog, Xóa bộ lọc, Quay lại) | `Outlined` | *(không đặt Color)* | "Hủy", "Đóng" |
| Phụ có ngữ nghĩa riêng (Export Excel, Import, In) | `Outlined` | Color phù hợp | "Export Excel" |

```razor
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="SearchAsync">Tìm</MudButton>
<MudButton Variant="Variant.Outlined" OnClick="ClearFilter">Xóa</MudButton>
<MudButton Variant="Variant.Outlined" Color="Color.Error" OnClick="DeleteAsync">Xóa</MudButton>
<MudButton Variant="Variant.Filled" Color="Color.Success" OnClick="ApproveAsync">Duyệt</MudButton>
```

- "Lưu" LUÔN là CTA (`Filled`/`Primary`), KHÔNG phải `Success`. "Sửa"/"Thêm dòng" cũng xếp CTA.
- Nút điều hướng thuần túy (quay lại, chuyển trang) xếp Trung tính.
- Không rõ 1 nút thuộc loại nào → ưu tiên Trung tính (`Outlined`, không đặt `Color`).
- **Bẫy confirm dialog**: `DialogService.ShowAsync<MudMessageBox>(...)` render nút Yes bằng markup
  mặc định, không sửa được — **KHÔNG** dùng cách gọi này. Luôn khai báo
  `<MudMessageBox @ref="_confirmBox">` trực tiếp + `<YesButton>` tường minh, chọn Variant/Color
  theo bảng trên. Pattern đầy đủ: `.claude/skills/web/SKILLS.md` §"MudMessageBox @ref".

---

## Pattern: Page header — title/icon/button

```razor
<div class="pos-page-header mb-4">
    <MudText Typo="Typo.h5" Class="pos-page-header-title" Style="font-weight:400">
        <MudIcon Icon="@Icons.Material.Filled.XYZ" Size="Size.Small" Class="mr-2" Style="vertical-align:middle"/>
        Tên trang
    </MudText>
    <MudButton Variant="Variant.Filled" Color="Color.Primary" Size="Size.Small"
               StartIcon="@Icons.Material.Filled.Add" Class="pos-page-header-btn">
        Thêm
    </MudButton>
</div>
```

- `.pos-page-header-title` (CSS `app.css`) đã giảm `font-size` xuống `1.25rem` (từ mặc định h5
  MudBlazor ~1.5rem). Icon cạnh title **bắt buộc** `Size="Size.Small"`.
- Title mặc định vẫn đậm (`Typography.H5.FontWeight=800`). Muốn chữ "tự nhiên" (không đậm) → thêm
  `Style="font-weight:400"` **cục bộ trên `MudText` đó**, không sửa `.pos-page-header-title` global.
- Nút CTA trong header dùng `Variant="Variant.Filled" Color="Color.Primary"` + `Size="Size.Small"`
  (đảo ngược v2, xem bảng Button convention ở trên).

---

## Pattern: Filter panel — nền trắng + border

> Thay cho pattern v2 "soft-tint" (đã loại bỏ) — theo mockup `.filter-bar` (nền trắng, border 1px,
> không tint màu).

```razor
<MudPaper Elevation="1" Class="pos-filter-panel pa-4 mb-4">
    <MudGrid Spacing="2">@* filter fields *@</MudGrid>
</MudPaper>
```

- Class `pos-filter-panel` (CSS `app.css`) = nền **trắng** (`var(--pos-surface)`) + `border: 1px
  solid var(--pos-border)` — phân biệt vùng nhập liệu với card dữ liệu bên dưới bằng viền, không
  bằng màu tint.
- `Elevation="1"` = flat (không shadow), khớp quy tắc elevation ở mục trên.

---

## Pattern: Sidebar — navy đậm, brand text-only, 3 cấp phân biệt icon

```razor
<MudDrawer @bind-Open="_drawerOpen" Elevation="0" ClipMode="DrawerClipMode.Always">
    <div class="pos-sidebar-brand">
        @* text-only 2 dòng: tên app + subtitle — KHÔNG icon/avatar (khớp .logo mockup) *@
        <MudText Typo="Typo.subtitle1">RPOS</MudText>
        <MudText Typo="Typo.caption">Dashboard</MudText>
    </div>
    <MudNavMenu Margin="Margin.Dense">
        @* L1 — CHỮ IN HOA, KHÔNG icon (label tĩnh, không click — xem SKILLS.md "Sidebar nav 3 cấp") *@
        <div class="pos-nav-section-label">CỬA HÀNG</div>
        @* L2 — MudNavGroup có icon riêng theo nhóm, luôn hiển thị dưới L1 *@
        <MudNavGroup Title="Vận hành" Icon="@Icons.Material.Outlined.MonitorHeart" Class="pos-nav-l2" HideExpandIcon="true" @bind-Expanded="_expandStoreOps">
            @* L3 — leaf link, icon ChevronRight đồng nhất *@
            <MudNavLink Href="/store/business-day" Icon="@Icons.Material.Outlined.ChevronRight" Match="NavLinkMatch.All">Xác nhận kết thúc ngày</MudNavLink>
        </MudNavGroup>
    </MudNavMenu>
    <div class="pos-sidebar-footer">
        @* avatar chữ cái đầu + tên + role + nút logout — dời từ MudAppBar xuống đây *@
    </div>
</MudDrawer>
```

- `DrawerBackground = "#0D1B2A"` (navy đậm) — **KHÔNG** còn sidebar sáng như v2.
  `MudDrawer Elevation="0"` — sidebar tự tách khỏi nền sáng bằng màu, không cần shadow riêng.
- Active nav item: nền **đặc** `var(--pos-primary)`, chữ/icon trắng, `border-radius:
  var(--pos-radius-sm)` (8px — **không phải** `-lg` 12px). Hover (không active): nền
  `var(--pos-drawer-hover)` (`#1E3448`).
- **Icon set giữ `Icons.Material.Outlined.*`** (mockup dùng emoji nhưng dự án cố ý KHÔNG dùng
  emoji — xem anti-pattern bên dưới).
- `pos-sidebar-brand`: text-only, KHÔNG avatar (khác v2). `pos-sidebar-footer`
  (`margin-top:auto`): avatar chữ cái đầu (tròn, nền Primary) + tên + role + nút logout — user-info
  đã dời từ `MudAppBar` xuống đây, AppBar giờ chỉ còn menu-toggle + title/breadcrumb + spacer.
- Cấu trúc 3 cấp đầy đủ (L1 label tĩnh không click / L2 luôn hiện có icon / L3 leaf chevron), quy
  tắc `UpdateExpanded`, và breadcrumb động: xem `.claude/skills/web/SKILLS.md` §"Sidebar nav
  (MainLayout) — 3 cấp" + §"AppBar — Breadcrumb động" (phần LOGIC điều hướng, không đổi ở đây).

**Anti-pattern:**
- ❌ Truyền emoji/text thường vào tham số `Icon=` của `MudNavLink`/`MudNavGroup`/`MudIcon` — tham
  số này nhận **SVG path**, không phải ligature; icon sẽ biến mất hoàn toàn (không lỗi, không cảnh
  báo). Đã thử 1 lần với emoji ở v3, phải rollback — giữ nguyên `Icons.Material.Outlined.*`.
- ❌ Sidebar dùng `MudAvatar` tròn ở brand header (kiểu v2) — v3 brand text-only.
- ❌ Đặt user-info ở `MudAppBar` (kiểu v2) — v3 đã dời xuống `pos-sidebar-footer`.
- ❌ Radius active nav dùng `-lg` (12px) — phải dùng `-sm` (8px).

---

## Pattern: Density Standard — Comfortable-tight

> Áp dụng khi: cần tối ưu density cho app dashboard (không quá rộng, mobile-safe). Không đổi qua
> các bản theme (v2 → v3 giữ nguyên).

| Thành phần | Giá trị |
|---|---|
| `LineHeight` (theme) | `"1.5"` |
| `MudTable` | `Dense="true"` — luôn |
| `MudGrid Spacing` | `Spacing="2"` (filter), `Spacing="3"` (KPI/chart) |
| Form Margin | `Margin="Margin.Dense"` trong filter panel |
| `MudAppBar` | chiều cao set qua `LayoutProperties.AppbarHeight` trong `PosTheme.cs` (không còn `Dense="true"` — xem breadcrumb pattern ở SKILLS.md) |
| `MudNavMenu` | `Margin="Margin.Dense"` (2px inter-item) |

**app.css overrides đã có sẵn** (không thêm lại):
- `.mud-list-item` → `padding: 5px` desktop / `8px` mobile
- `.mud-drawer .mud-nav-link` → `padding: 4px; margin: 1px` desktop / `9px; 2px` mobile
- `@media (max-width: 599.98px)` → min-height 40px cho button/icon-button (WCAG 2.5.5)
- `.d-flex.flex-wrap > div > .mud-paper { height: 100% }` → KPI cards equal height

**Anti-pattern:**
- ❌ Thêm lại `@media (max-width: 599.98px)` cho từng component riêng — CSS global đã đủ
- ❌ `MudGrid` không có `Spacing` — tự default về 4 (16px), quá rộng
