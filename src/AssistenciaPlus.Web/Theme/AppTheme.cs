using MudBlazor;

namespace AssistenciaPlus.Web.Theme;

public static class AppTheme
{
    // ── Colors de l'escola ─────────────────────────────────
    // Basats en el branding institucional de l'Escola Marta Mata
    public const string PrimaryColor = "#003F8A";      // Blau institucional
    public const string SecondaryColor = "#E05B1A";    // Taronja càlid
    public const string SuccessColor = "#2E7D32";      // Verd (present)
    public const string ErrorColor = "#C62828";        // Vermell (absent)
    public const string WarningColor = "#F57F17";      // Ambre (tard)

    public static MudTheme Create() => new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = PrimaryColor,
            PrimaryContrastText = "#FFFFFF",
            PrimaryDarken = "#002B6A",
            PrimaryLighten = "#1A5FAD",
            Secondary = SecondaryColor,
            SecondaryContrastText = "#FFFFFF",
            Tertiary = "#2E7D32",
            TertiaryContrastText = "#FFFFFF",
            Background = "#F4F6FA",
            Surface = "#FFFFFF",
            AppbarBackground = PrimaryColor,
            AppbarText = "#FFFFFF",
            DrawerBackground = "#FAFBFD",
            DrawerText = "#1A2744",
            DrawerIcon = PrimaryColor,
            TextPrimary = "#1A2744",
            TextSecondary = "#4A5568",
            TextDisabled = "#A0AEC0",
            ActionDefault = PrimaryColor,
            ActionDisabled = "#CBD5E0",
            ActionDisabledBackground = "#EDF2F7",
            Error = ErrorColor,
            ErrorContrastText = "#FFFFFF",
            Warning = "#F57F17",
            WarningContrastText = "#1A2744",   // fosc sobre ambre — contrast 5:1 (WCAG AA)
            Info = "#0277BD",
            InfoContrastText = "#FFFFFF",
            Success = SuccessColor,
            SuccessContrastText = "#FFFFFF",
            TableLines = "#E2E8F0",
            TableStriped = "#F7FAFC",
            TableHover = "#EBF4FF",
            Divider = "#E2E8F0",
            DividerLight = "#F0F4F8",
            OverlayLight = "rgba(0,63,138,0.04)",
            OverlayDark = "rgba(0,0,0,0.5)",
            GrayLight = "#E2E8F0",
            GrayLighter = "#F7FAFC",
            Dark = "#1A2744",
            DarkContrastText = "#FFFFFF",
            White = "#FFFFFF",
            Black = "#000000"
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.6",
                LetterSpacing = "normal"
            },
            H1 = new H1Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "2rem",
                FontWeight = "700",
                LineHeight = "1.2"
            },
            H2 = new H2Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1.75rem",
                FontWeight = "700",
                LineHeight = "1.25"
            },
            H3 = new H3Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1.5rem",
                FontWeight = "600",
                LineHeight = "1.3"
            },
            H4 = new H4Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1.25rem",
                FontWeight = "600",
                LineHeight = "1.35"
            },
            H5 = new H5Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1.1rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            H6 = new H6Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "600",
                LineHeight = "1.4"
            },
            Body1 = new Body1Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "400",
                LineHeight = "1.6"
            },
            Body2 = new Body2Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "400",
                LineHeight = "1.5"
            },
            Button = new ButtonTypography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "0.9375rem",
                FontWeight = "600",
                LineHeight = "1.75",
                TextTransform = "none",
                LetterSpacing = "0.02em"
            },
            Subtitle1 = new Subtitle1Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "1rem",
                FontWeight = "500",
                LineHeight = "1.5"
            },
            Subtitle2 = new Subtitle2Typography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "0.875rem",
                FontWeight = "500",
                LineHeight = "1.5"
            },
            Caption = new CaptionTypography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "0.75rem",
                FontWeight = "400",
                LineHeight = "1.4"
            },
            Overline = new OverlineTypography
            {
                FontFamily = ["Roboto", "sans-serif"],
                FontSize = "0.75rem",
                FontWeight = "600",
                LineHeight = "2",
                LetterSpacing = "0.1em",
                TextTransform = "uppercase"
            }
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = "264px",
            DrawerWidthRight = "264px",
            AppbarHeight = "64px"
        }
    };
}
