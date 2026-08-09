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
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Roboto", "Helvetica Neue", "sans-serif"],
                FontSize   = "0.875rem",
                LineHeight = "1.5",
            },
            H1 = new H1Typography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            H2 = new H2Typography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            H3 = new H3Typography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            H4 = new H4Typography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            H5 = new H5Typography
            {
                FontFamily    = ["Inter", "Roboto", "sans-serif"],
                FontWeight    = "700",
                LetterSpacing = "-0.01em",
            },
            H6 = new H6Typography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontWeight = "600",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontWeight = "600",
            },
            Subtitle2 = new Subtitle2Typography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            Body1 = new Body1Typography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontSize   = "0.875rem",
                FontWeight = "400",
            },
            Body2 = new Body2Typography
            {
                FontFamily = ["Inter", "Roboto", "sans-serif"],
                FontSize   = "0.8125rem",
            },
            Caption = new CaptionTypography
            {
                FontFamily    = ["Inter", "Roboto", "sans-serif"],
                FontSize      = "0.75rem",
                FontWeight    = "500",
                LetterSpacing = "0.02em",
            },
            Overline = new OverlineTypography { FontFamily = ["Inter", "Roboto", "sans-serif"] },
            Button = new ButtonTypography
            {
                FontFamily    = ["Inter", "Roboto", "sans-serif"],
                FontSize      = "0.875rem",
                FontWeight    = "600",
                TextTransform = "none",
            },
        },

        Shadows = new Shadow
        {
            Elevation =
            [
                "none",                                         // 0
                "0 1px 2px rgba(0,0,0,0.05)",                   // 1 — flat/bordered 
                "0 4px 6px -1px rgba(0,0,0,0.05), 0 2px 4px -1px rgba(0,0,0,0.03)", // 2 — soft card shadow
                "0 10px 15px -3px rgba(0,0,0,0.05), 0 4px 6px -2px rgba(0,0,0,0.025)", // 3
                "0 20px 25px -5px rgba(0,0,0,0.1), 0 10px 10px -5px rgba(0,0,0,0.04)", // 4 — Login card
                "0 25px 50px -12px rgba(0,0,0,0.15)",           // 5 
                "0 5px 18px rgba(26,43,69,0.08)",              // 6 (softened)
                "0 6px 20px rgba(26,43,69,0.09)",              // 7
                "0 6px 22px rgba(26,43,69,0.10)",              // 8
                "0 7px 24px rgba(26,43,69,0.11)",              // 9
                "0 8px 26px rgba(26,43,69,0.12)",              // 10 — AppBar
                "0 8px 28px rgba(26,43,69,0.13)",              // 11
                "0 9px 30px rgba(26,43,69,0.14)",              // 12 — Dialog
                "0 10px 32px rgba(26,43,69,0.15)",             // 13
                "0 10px 34px rgba(26,43,69,0.16)",             // 14
                "0 12px 36px rgba(26,43,69,0.17)",             // 15
                "0 12px 38px rgba(26,43,69,0.18)",             // 16
                "0 14px 40px rgba(26,43,69,0.19)",             // 17
                "0 14px 42px rgba(26,43,69,0.20)",             // 18
                "0 16px 44px rgba(26,43,69,0.21)",             // 19
                "0 16px 46px rgba(26,43,69,0.22)",             // 20
                "0 18px 48px rgba(26,43,69,0.23)",             // 21
                "0 18px 50px rgba(26,43,69,0.24)",             // 22
                "0 20px 52px rgba(26,43,69,0.25)",             // 23
                "0 20px 54px rgba(26,43,69,0.26)",             // 24
                "0 22px 56px rgba(26,43,69,0.27)",             // 25
            ]
        }
    };
}
