using MudBlazor;

namespace POS.Web.Theme;

public static class PosTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // --- Primary brand: steel blue (theme_html mockup --steel) ---
            Primary             = "#2660A4",
            PrimaryDarken       = "#1E50A0",
            PrimaryLighten      = "#3D8FD9",
            PrimaryContrastText = "#FFFFFF",

            // --- Secondary: neutral gray-blue (theme_html --gray5) ---
            Secondary             = "#4A6070",
            SecondaryDarken       = "#37495A",
            SecondaryLighten      = "#6A8498",
            SecondaryContrastText = "#FFFFFF",

            // --- Tertiary: purple accent (theme_html --purple) ---
            Tertiary             = "#6040A8",
            TertiaryDarken       = "#4A3080",
            TertiaryLighten      = "#8A6FC8",
            TertiaryContrastText = "#FFFFFF",

            // --- Status (theme_html --green/--red/--gold/--sky) ---
            Success             = "#1F7A4A",
            SuccessContrastText = "#FFFFFF",
            Error               = "#B52B27",
            ErrorContrastText   = "#FFFFFF",
            Warning             = "#D4860A",
            WarningContrastText = "#1A2B38",
            Info                = "#3D8FD9",
            InfoContrastText    = "#FFFFFF",

            // --- Background & Surface (theme_html --gray1) ---
            Background    = "#F0F4F8",
            BackgroundGray = "#E4E9EF",
            Surface       = "#FFFFFF",

            // --- Drawer (sidebar): dark navy, per theme_html mockup ---
            DrawerBackground = "#0D1B2A",
            DrawerText       = "rgba(255,255,255,0.6)",
            DrawerIcon       = "rgba(255,255,255,0.6)",

            // --- AppBar: light surface (theme_html .topbar) ---
            AppbarBackground = "#FFFFFF",
            AppbarText       = "#1A2B38",

            // --- Typography colors ---
            TextPrimary   = "#1A2B38",
            TextSecondary = "#6A8498",
            TextDisabled  = "#8FA3B4",

            // --- Actions & icons ---
            ActionDefault            = "#4A6070",
            ActionDisabled           = "#8FA3B4",
            ActionDisabledBackground = "#E4E9EF",

            // --- Dividers ---
            Divider      = "#E4E9EF",
            DividerLight = "rgba(228,233,239,0.6)",

            // --- Tables ---
            TableLines   = "#E4E9EF",
            TableStriped = "#F6F8FA",
            TableHover   = "#F8FAFB",

            // --- Overlay ---
            OverlayLight = "rgba(240,244,248,0.7)",
            OverlayDark  = "rgba(13,27,42,0.5)",
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "12px",
            DrawerWidthLeft     = "260px",
            AppbarHeight        = "50px", // khớp mockup .topbar{height:50px}
        },

        Typography = new Typography
        {
            // theme_html.html: body{font-family:'Segoe UI',system-ui,sans-serif}. MudBlazor sinh
            // CSS variable RIÊNG cho mỗi typography variant (--mud-typography-h5-family,
            // --mud-typography-body1-family...) — KHÔNG kế thừa từ Default. Phải set FontFamily
            // trên TỪNG variant, nếu không phần lớn chữ hiển thị thật (H5/H6/Subtitle1/Body1/
            // Body2/Caption/Button) vẫn giữ font mặc định của MudBlazor.
            Default = new DefaultTypography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize   = "0.8125rem",   // 13px — khớp base mockup (cũ 0.875rem/14px)
                LineHeight = "1.5",         // khớp mockup body{line-height:1.5}
            },
            H1 = new H1Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            H5 = new H5Typography
            {
                FontFamily    = ["Segoe UI", "system-ui", "sans-serif"],
                FontWeight    = "800",
                LetterSpacing = "-0.02em",
            },
            H6 = new H6Typography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontWeight = "600",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontWeight = "600",
            },
            Subtitle2 = new Subtitle2Typography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            Body1 = new Body1Typography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize   = "0.78125rem", // 12.5px — khớp mockup .input,.select,.textarea. Chi phối
                                          // text input/value trong MudTextField/MudSelect/
                                          // MudDatePicker + dropdown/autocomplete popup (KHÔNG ảnh
                                          // hưởng DataTable — cell MudTable dùng size cố định riêng
                                          // của MudBlazor, không cascade từ Body1).
                FontWeight = "400",       // chữ tự nhiên, không đậm
            },
            Body2 = new Body2Typography
            {
                FontFamily = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize   = "0.8125rem",
            },
            Caption = new CaptionTypography
            {
                FontFamily    = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize      = "0.75rem",
                FontWeight    = "600",
                LetterSpacing = "0.04em",
            },
            Overline = new OverlineTypography { FontFamily = ["Segoe UI", "system-ui", "sans-serif"] },
            Button = new ButtonTypography
            {
                FontFamily    = ["Segoe UI", "system-ui", "sans-serif"],
                FontSize      = "0.75rem", // 12px — khớp mockup .btn
                FontWeight    = "600",
                TextTransform = "none",   // bỏ ALL CAPS mặc định của MudBlazor
            },
        },

        Shadows = new Shadow
        {
            Elevation =
            [
                "none",                                         // 0
                "none",                                         // 1 — flat/bordered (filter panel, toolbar)
                "0 2px 8px rgba(0,0,0,0.08)",                   // 2 — card shadow thật (KPI card, table-wrap, alert) — theme_html --shadow
                "0 2px 8px rgba(0,0,0,0.08)",                   // 3 — = elevation 2
                "0 4px 20px rgba(0,0,0,0.12)",                  // 4 — theme_html --shadow-lg (Login card)
                "0 4px 20px rgba(0,0,0,0.12)",                  // 5 — = elevation 4
                "0 5px 18px rgba(26,43,69,0.15)",              // 6
                "0 6px 20px rgba(26,43,69,0.16)",              // 7
                "0 6px 22px rgba(26,43,69,0.17)",              // 8
                "0 7px 24px rgba(26,43,69,0.18)",              // 9
                "0 8px 26px rgba(26,43,69,0.19)",              // 10 — AppBar
                "0 8px 28px rgba(26,43,69,0.20)",              // 11
                "0 9px 30px rgba(26,43,69,0.21)",              // 12 — Dialog
                "0 10px 32px rgba(26,43,69,0.22)",             // 13
                "0 10px 34px rgba(26,43,69,0.23)",             // 14
                "0 12px 36px rgba(26,43,69,0.24)",             // 15
                "0 12px 38px rgba(26,43,69,0.25)",             // 16
                "0 14px 40px rgba(26,43,69,0.26)",             // 17
                "0 14px 42px rgba(26,43,69,0.27)",             // 18
                "0 16px 44px rgba(26,43,69,0.28)",             // 19
                "0 16px 46px rgba(26,43,69,0.29)",             // 20
                "0 18px 48px rgba(26,43,69,0.30)",             // 21
                "0 18px 50px rgba(26,43,69,0.31)",             // 22
                "0 20px 52px rgba(26,43,69,0.32)",             // 23
                "0 20px 54px rgba(26,43,69,0.33)",             // 24
                "0 22px 56px rgba(26,43,69,0.35)",             // 25
            ]
        }
    };
}
