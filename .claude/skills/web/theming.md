---
name: web-theming
description: Cách sửa PosTheme.cs (màu/typography/elevation toàn app) trong POS.Web. Rule/pattern hiện hành (Button/Sidebar/Elevation/Density) là .claude/rules/mudblazor-flat-ui.md — file này chỉ có code C# + checklist khi sửa theme.
---

# Theming — Custom MudBlazor Theme

> **Áp dụng khi:** cần đổi màu/typography toàn bộ MudBlazor components (primary, sidebar, appbar, success/error...) mà không sửa từng file Razor.
> **Nguồn sự thật cho MỌI rule/pattern hiện hành** (Button convention, Elevation, Sidebar visual,
> Filter panel, Density Standard): **`.claude/rules/mudblazor-flat-ui.md`** — file này CHỈ giữ code
> `PosTheme.cs` thực tế + checklist khi sửa theme, không lặp lại rule đã có ở đó.

---

## Pattern: PosTheme.cs — định nghĩa màu tập trung cho toàn app

> **v3 — cập nhật 2026-07-05**, theo mockup `docs/web/theme/theme_html.html` (do người dùng cung
> cấp): sidebar navy đậm, card có shadow thật, radius 2 cấp, Button `Filled` cho CTA. Thay hoàn
> toàn cho bản v2 (2026-07-04, "Mud Mini": sidebar sáng, card borderless, radius 16px, Button
> `Outlined` mọi nơi — đã lỗi thời). Lịch sử quyết định đầy đủ (kể cả các đề xuất đã cân nhắc và
> loại bỏ): `docs/web/theme/theme-decision-log.md`.

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
            AppbarHeight        = "50px",   // khớp mockup .topbar{height:50px} — KHÔNG dùng Dense (xem sidebar-nav.md §Breadcrumb)
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

## Rule/pattern hiện hành — chỉ tham chiếu, không lặp lại

> Toàn bộ rule Button convention, Page header, Filter panel, Sidebar (visual), Density Standard đã
> chốt tại **`.claude/rules/mudblazor-flat-ui.md`** §3/§6/§7/§5/§15 — đọc trực tiếp file đó khi cần
> áp dụng, KHÔNG lặp lại bảng/code ở đây (từng gây lệch khi 1 nơi cập nhật, nơi kia quên).
> Logic C# sidebar 3 cấp (`UpdateExpanded`) + breadcrumb động: `.claude/skills/web/sidebar-nav.md`.
