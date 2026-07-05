using MudBlazor;

namespace POS.Web.Theme;

public static class PosTheme
{
    public static MudTheme Default { get; } = new()
    {
        PaletteLight = new PaletteLight
        {
            // --- Primary brand: navy blue ---
            Primary             = "#2051A3",
            PrimaryDarken       = "#1B3A5C",
            PrimaryLighten      = "#3A6FCC",
            PrimaryContrastText = "#FFFFFF",

            // --- Secondary: muted blue-gray (accessible on white: ~5.1:1) ---
            Secondary             = "#6B7A8D",
            SecondaryDarken       = "#4A5568",
            SecondaryLighten      = "#8898AA",
            SecondaryContrastText = "#FFFFFF",

            // --- Tertiary: teal accent ---
            Tertiary             = "#1EAA90",
            TertiaryDarken       = "#17876F",
            TertiaryLighten      = "#3DC9AE",
            TertiaryContrastText = "#FFFFFF",

            // --- Status ---
            Success             = "#27AE60",
            SuccessContrastText = "#FFFFFF",
            Error               = "#DC3545",
            ErrorContrastText   = "#FFFFFF",
            Warning             = "#F39C12",
            WarningContrastText = "#1A2B45",   // dark text — #F39C12 on white is only 2.4:1
            Info                = "#3A6FCC",
            InfoContrastText    = "#FFFFFF",

            // --- Background & Surface ---
            Background    = "#F2F4F8",
            BackgroundGray = "#EEF1F7",
            Surface       = "#FFFFFF",

            // --- Drawer (sidebar): light, per MudBlazor "Mud Mini" reference ---
            DrawerBackground = "#FFFFFF",
            DrawerText       = "rgba(26,43,69,0.85)",
            DrawerIcon       = "rgba(26,43,69,0.55)",

            // --- AppBar: same light surface ---
            AppbarBackground = "#FFFFFF",
            AppbarText       = "#1A2B45",

            // --- Typography colors ---
            TextPrimary   = "#1A2B45",
            TextSecondary = "#6B7A8D",
            TextDisabled  = "#B0BAC9",

            // --- Actions & icons ---
            ActionDefault            = "#6B7A8D",
            ActionDisabled           = "#B0BAC9",
            ActionDisabledBackground = "#EEF1F7",

            // --- Dividers ---
            Divider      = "#DDE3EE",
            DividerLight = "rgba(221,227,238,0.5)",

            // --- Tables ---
            TableLines   = "#DDE3EE",
            TableStriped = "#F8F9FC",
            TableHover   = "#EEF1F7",

            // --- Overlay ---
            OverlayLight = "rgba(242,244,248,0.7)",
            OverlayDark  = "rgba(26,43,69,0.5)",
        },

        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "16px",
            DrawerWidthLeft     = "260px",
        },

        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["-apple-system", "BlinkMacSystemFont", "Segoe UI", "Roboto", "Helvetica Neue", "sans-serif"],
                FontSize   = "0.875rem",
                LineHeight = "1.45",
            },
            H5 = new H5Typography
            {
                FontWeight    = "800",
                LetterSpacing = "-0.02em",
            },
            H6 = new H6Typography
            {
                FontWeight = "600",
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontWeight = "600",
            },
            Body1 = new Body1Typography
            {
                FontSize   = "0.75rem",   // 12px — giảm ~15% từ 14px cũ. Chi phối text input/value
                                          // trong MudTextField/MudSelect/MudDatePicker + dropdown/
                                          // autocomplete popup (KHÔNG ảnh hưởng DataTable — cell
                                          // MudTable dùng size cố định riêng của MudBlazor, không
                                          // cascade từ Body1).
                FontWeight = "400",       // chữ tự nhiên, không đậm
            },
            Body2 = new Body2Typography
            {
                FontSize = "0.8125rem",
            },
            Caption = new CaptionTypography
            {
                FontSize      = "0.75rem",
                FontWeight    = "600",
                LetterSpacing = "0.04em",
            },
            Button = new ButtonTypography
            {
                FontWeight    = "600",
                LetterSpacing = "0.03em",
                TextTransform = "none",   // bỏ ALL CAPS mặc định của MudBlazor
            },
        },

        Shadows = new Shadow
        {
            Elevation =
            [
                "none",                                         // 0
                "none",                                         // 1 — borderless (bg contrast Surface vs Background)
                "none",                                         // 2 — borderless (card)
                "none",                                         // 3 — borderless
                "none",                                         // 4 — borderless
                "none",                                         // 5 — borderless
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
