# Theming — Custom MudBlazor Theme

> **Áp dụng khi:** cần đổi màu/typography toàn bộ MudBlazor components (primary, sidebar, appbar, success/error...) mà không sửa từng file Razor.

---

## Pattern: PosTheme.cs — định nghĩa màu tập trung cho toàn app

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
            DrawerBackground = "#1B3A5C",   // sidebar
            AppbarBackground = "#1B3A5C",   // appbar
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
            //                   thay vì 14px như DataTable và filter labels
            Body1  = new Body1Typography  { FontSize = "0.875rem" },
            Body2  = new Body2Typography  { FontSize = "0.8125rem" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" },
        },
        Shadows = new Shadow { Elevation = [ "none", /* ... 24 entries */ ] },
        LayoutProperties = new LayoutProperties { DefaultBorderRadius = "8px" }
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
- Sau khi thêm `Theme=` vào `MudThemeProvider`, mọi `Color.Primary`, `Color.Success`... tự đổi màu

**Anti-pattern:**
- ❌ `FontWeight = 700` → compile error (phải là `"700"`)
- ❌ `<MudThemeProvider/>` không có `Theme=` → dùng màu mặc định MudBlazor (tím/xanh MUI)
- ❌ Hardcode màu trong CSS isolation thay vì theme → màu không đồng bộ

> Ví dụ thực tế: `src/POS.Web/Theme/PosTheme.cs`
> Style guide reference: `docs/style-guide.html`
