using Microsoft.Maui.Graphics;

#pragma warning disable S107, S1192

namespace MothballMobile.Infrastructure.Settings;

public static class ThemePaletteApplier
{
    public static void Apply(ResourceDictionary resources, ThemePalette palette, AppTheme mode)
    {
        var tokens = GetTokens(palette);

        foreach (var token in tokens)
        {
            resources[token.Key] = Color.FromArgb(token.Value);
        }

        var activeSuffix = mode == AppTheme.Dark ? "Dark" : string.Empty;
        foreach (var key in ActiveTokenKeys)
        {
            var activeKey = key + activeSuffix;
            if (tokens.TryGetValue(activeKey, out var activeValue))
            {
                resources[key] = Color.FromArgb(activeValue);
            }
        }

        resources["PrimaryBrush"] = new SolidColorBrush((Color)resources["Primary"]);
        resources["SecondaryBrush"] = new SolidColorBrush((Color)resources["Secondary"]);
        resources["TertiaryBrush"] = new SolidColorBrush((Color)resources["Tertiary"]);
        resources["SurfaceBrush"] = new SolidColorBrush((Color)resources["Surface"]);
        resources["BackgroundBrush"] = new SolidColorBrush((Color)resources["Background"]);
    }

    private static readonly string[] ActiveTokenKeys =
    [
        "Primary", "OnPrimary", "PrimaryContainer", "OnPrimaryContainer",
        "Secondary", "OnSecondary", "SecondaryContainer", "OnSecondaryContainer",
        "Tertiary", "OnTertiary", "TertiaryContainer", "OnTertiaryContainer",
        "Background", "OnBackground", "Surface", "OnSurface", "SurfaceVariant", "PickerSurface", "OnSurfaceVariant", "Outline", "OutlineVariant",
        "InverseSurface", "InverseOnSurface", "InversePrimary",
    ];

    private static IReadOnlyDictionary<string, string> GetTokens(ThemePalette palette)
        => palette switch
        {
            ThemePalette.OliveWorkshop => OliveLight,
            ThemePalette.BlueprintLedger => BlueprintLight,
            ThemePalette.TerracottaArchive => TerracottaLight,
            ThemePalette.SaffronUtility => SaffronLight,
            ThemePalette.CoastalInventory => CoastalLight,
            ThemePalette.BerryArchive => BerryLight,
            _ => OliveLight,
        };

    private static readonly IReadOnlyDictionary<string, string> OliveLight = Create(
        primary: "#4C662B", onPrimary: "#FFFFFF", primaryContainer: "#CDEDA3", onPrimaryContainer: "#102000",
        secondary: "#586249", onSecondary: "#FFFFFF", secondaryContainer: "#E9EFDC", onSecondaryContainer: "#18200F",
        tertiary: "#8B4F3F", onTertiary: "#FFFFFF", tertiaryContainer: "#FFDAD6", onTertiaryContainer: "#410006",
        background: "#FBFCF5", onBackground: "#1A1C17", surface: "#FBFCF5", onSurface: "#1A1C17",
        surfaceVariant: "#E1E4D8", pickerSurface: "#D8DCCD", onSurfaceVariant: "#45483E", outline: "#76786D", outlineVariant: "#C6C8BA",
        inverseSurface: "#2F312C", inverseOnSurface: "#F1F2E9", inversePrimary: "#B1D182",
        primaryDark: "#B1D182", onPrimaryDark: "#1E3700", primaryContainerDark: "#354F16", onPrimaryContainerDark: "#CDEDA3",
        secondaryDark: "#C0C9B0", onSecondaryDark: "#2A3321", secondaryContainerDark: "#414A37", onSecondaryContainerDark: "#DDE6CC",
        tertiaryDark: "#FFB4A8", onTertiaryDark: "#561F16", tertiaryContainerDark: "#73352A", onTertiaryContainerDark: "#FFDAD6",
        backgroundDark: "#1A1C17", onBackgroundDark: "#E3E4D9", surfaceDark: "#1A1C17", onSurfaceDark: "#E3E4D9",
        surfaceVariantDark: "#45483E", pickerSurfaceDark: "#36392F", onSurfaceVariantDark: "#C5C8B8", outlineDark: "#909387", outlineVariantDark: "#45483E",
        inverseSurfaceDark: "#E3E4D9", inverseOnSurfaceDark: "#2F312C", inversePrimaryDark: "#4C662B");

    private static readonly IReadOnlyDictionary<string, string> BlueprintLight = Create(
        primary: "#3F5F90", onPrimary: "#FFFFFF", primaryContainer: "#D7E2FF", onPrimaryContainer: "#001A41",
        secondary: "#58616F", onSecondary: "#FFFFFF", secondaryContainer: "#E1E8F7", onSecondaryContainer: "#171C25",
        tertiary: "#76558A", onTertiary: "#FFFFFF", tertiaryContainer: "#F4D9FF", onTertiaryContainer: "#2D123D",
        background: "#FAF8FF", onBackground: "#1A1B20", surface: "#FAF8FF", onSurface: "#1A1B20",
        surfaceVariant: "#E1E2EA", pickerSurface: "#D6D8E0", onSurfaceVariant: "#45464F", outline: "#767780", outlineVariant: "#C6C6D0",
        inverseSurface: "#2F3035", inverseOnSurface: "#F1F0F7", inversePrimary: "#AEC6FF",
        primaryDark: "#AEC6FF", onPrimaryDark: "#092F62", primaryContainerDark: "#264777", onPrimaryContainerDark: "#D7E2FF",
        secondaryDark: "#C1C9D8", onSecondaryDark: "#2C313B", secondaryContainerDark: "#3D4759", onSecondaryContainerDark: "#DFE6F7",
        tertiaryDark: "#E2B9F2", onTertiaryDark: "#422050", tertiaryContainerDark: "#593F67", onTertiaryContainerDark: "#F4D9FF",
        backgroundDark: "#1A1B20", onBackgroundDark: "#E3E2E9", surfaceDark: "#1A1B20", onSurfaceDark: "#E3E2E9",
        surfaceVariantDark: "#45464F", pickerSurfaceDark: "#34353D", onSurfaceVariantDark: "#C6C6D0", outlineDark: "#90919A", outlineVariantDark: "#45464F",
        inverseSurfaceDark: "#E3E2E9", inverseOnSurfaceDark: "#2F3035", inversePrimaryDark: "#3F5F90");

    private static readonly IReadOnlyDictionary<string, string> TerracottaLight = Create(
        primary: "#8B4F3F", onPrimary: "#FFFFFF", primaryContainer: "#FFDBD0", onPrimaryContainer: "#370E05",
        secondary: "#765950", onSecondary: "#FFFFFF", secondaryContainer: "#F4DFDA", onSecondaryContainer: "#2A1713",
        tertiary: "#625D91", onTertiary: "#FFFFFF", tertiaryContainer: "#E6DEFB", onTertiaryContainer: "#211047",
        background: "#FFF8F6", onBackground: "#211A18", surface: "#FFF8F6", onSurface: "#211A18",
        surfaceVariant: "#F0DEDA", pickerSurface: "#DFCBC6", onSurfaceVariant: "#514541", outline: "#84736E", outlineVariant: "#D7C2BC",
        inverseSurface: "#362E2B", inverseOnSurface: "#FAEEEA", inversePrimary: "#FFB4A0",
        primaryDark: "#FFB4A0", onPrimaryDark: "#571F12", primaryContainerDark: "#733626", onPrimaryContainerDark: "#FFDBD0",
        secondaryDark: "#E4BDB4", onSecondaryDark: "#432A24", secondaryContainerDark: "#5A4039", onSecondaryContainerDark: "#F4DFDA",
        tertiaryDark: "#C8C2FA", onTertiaryDark: "#302D5F", tertiaryContainerDark: "#4A3B6D", onTertiaryContainerDark: "#E6DEFB",
        backgroundDark: "#211A18", onBackgroundDark: "#EEE0DC", surfaceDark: "#211A18", onSurfaceDark: "#EEE0DC",
        surfaceVariantDark: "#514541", pickerSurfaceDark: "#3A302D", onSurfaceVariantDark: "#D7C2BC", outlineDark: "#9F8C86", outlineVariantDark: "#514541",
        inverseSurfaceDark: "#EEE0DC", inverseOnSurfaceDark: "#362E2B", inversePrimaryDark: "#8B4F3F");

    private static readonly IReadOnlyDictionary<string, string> SaffronLight = Create(
        primary: "#765900", onPrimary: "#FFFFFF", primaryContainer: "#FFDF8E", onPrimaryContainer: "#261A00",
        secondary: "#6B5E3A", onSecondary: "#FFFFFF", secondaryContainer: "#F4E4B7", onSecondaryContainer: "#211B08",
        tertiary: "#39675C", onTertiary: "#FFFFFF", tertiaryContainer: "#B9E0D2", onTertiaryContainer: "#002019",
        background: "#FFFBF2", onBackground: "#1D1B16", surface: "#FFFBF2", onSurface: "#1D1B16",
        surfaceVariant: "#EEE6D2", pickerSurface: "#DDD4BD", onSurfaceVariant: "#4D4739", outline: "#7E7765", outlineVariant: "#D0C7B2",
        inverseSurface: "#333027", inverseOnSurface: "#FAF0DD", inversePrimary: "#E8C45F",
        primaryDark: "#E8C45F", onPrimaryDark: "#3D2F00", primaryContainerDark: "#5C4600", onPrimaryContainerDark: "#FFDF8E",
        secondaryDark: "#D7C79A", onSecondaryDark: "#393016", secondaryContainerDark: "#50461F", onSecondaryContainerDark: "#F4E4B7",
        tertiaryDark: "#9DD1C0", onTertiaryDark: "#07372E", tertiaryContainerDark: "#245047", onTertiaryContainerDark: "#B9E0D2",
        backgroundDark: "#1D1B16", onBackgroundDark: "#E8E2D4", surfaceDark: "#1D1B16", onSurfaceDark: "#E8E2D4",
        surfaceVariantDark: "#4D4739", pickerSurfaceDark: "#373228", onSurfaceVariantDark: "#D0C7B2", outlineDark: "#98907B", outlineVariantDark: "#4D4739",
        inverseSurfaceDark: "#E8E2D4", inverseOnSurfaceDark: "#333027", inversePrimaryDark: "#765900");

    private static readonly IReadOnlyDictionary<string, string> CoastalLight = Create(
        primary: "#245A63", onPrimary: "#FFFFFF", primaryContainer: "#A7EAF1", onPrimaryContainer: "#002023",
        secondary: "#506367", onSecondary: "#FFFFFF", secondaryContainer: "#D5E9E9", onSecondaryContainer: "#102022",
        tertiary: "#5A5A89", onTertiary: "#FFFFFF", tertiaryContainer: "#DCDCF9", onTertiaryContainer: "#18174A",
        background: "#F7FCFC", onBackground: "#171D1E", surface: "#F7FCFC", onSurface: "#171D1E",
        surfaceVariant: "#DEE9E9", pickerSurface: "#CDDADA", onSurfaceVariant: "#3F4849", outline: "#6F797A", outlineVariant: "#BEC8C9",
        inverseSurface: "#2C3233", inverseOnSurface: "#EEF4F4", inversePrimary: "#8FD1D9",
        primaryDark: "#8FD1D9", onPrimaryDark: "#00363D", primaryContainerDark: "#0B4D55", onPrimaryContainerDark: "#A7EAF1",
        secondaryDark: "#B9CDCE", onSecondaryDark: "#233537", secondaryContainerDark: "#35494B", onSecondaryContainerDark: "#D5E9E9",
        tertiaryDark: "#C1C1F2", onTertiaryDark: "#2B2B59", tertiaryContainerDark: "#46466D", onTertiaryContainerDark: "#DCDCF9",
        backgroundDark: "#171D1E", onBackgroundDark: "#DFE4E4", surfaceDark: "#171D1E", onSurfaceDark: "#DFE4E4",
        surfaceVariantDark: "#3F4849", pickerSurfaceDark: "#2E3637", onSurfaceVariantDark: "#BEC8C9", outlineDark: "#899394", outlineVariantDark: "#3F4849",
        inverseSurfaceDark: "#DFE4E4", inverseOnSurfaceDark: "#2C3233", inversePrimaryDark: "#245A63");

    private static readonly IReadOnlyDictionary<string, string> BerryLight = Create(
        primary: "#7E4056", onPrimary: "#FFFFFF", primaryContainer: "#FFD9E3", onPrimaryContainer: "#32101E",
        secondary: "#735660", onSecondary: "#FFFFFF", secondaryContainer: "#F3DFE5", onSecondaryContainer: "#28171D",
        tertiary: "#65652D", onTertiary: "#FFFFFF", tertiaryContainer: "#E5E2BB", onTertiaryContainer: "#1D1E00",
        background: "#FFF8F9", onBackground: "#211A1D", surface: "#FFF8F9", onSurface: "#211A1D",
        surfaceVariant: "#F0DEE3", pickerSurface: "#DFCBD1", onSurfaceVariant: "#514348", outline: "#83747A", outlineVariant: "#D8C2C9",
        inverseSurface: "#352D30", inverseOnSurface: "#FAEEF1", inversePrimary: "#F1B0C2",
        primaryDark: "#F1B0C2", onPrimaryDark: "#4A1B2D", primaryContainerDark: "#653047", onPrimaryContainerDark: "#FFD9E3",
        secondaryDark: "#DFBFCB", onSecondaryDark: "#402A32", secondaryContainerDark: "#513A43", onSecondaryContainerDark: "#F3DFE5",
        tertiaryDark: "#C8C580", onTertiaryDark: "#323300", tertiaryContainerDark: "#4B4D1C", onTertiaryContainerDark: "#E5E2BB",
        backgroundDark: "#211A1D", onBackgroundDark: "#EEDFE3", surfaceDark: "#211A1D", onSurfaceDark: "#EEDFE3",
        surfaceVariantDark: "#514348", pickerSurfaceDark: "#3A3034", onSurfaceVariantDark: "#D8C2C9", outlineDark: "#A08D94", outlineVariantDark: "#514348",
        inverseSurfaceDark: "#EEDFE3", inverseOnSurfaceDark: "#352D30", inversePrimaryDark: "#7E4056");

    private static IReadOnlyDictionary<string, string> Create(
        string primary, string onPrimary, string primaryContainer, string onPrimaryContainer,
        string secondary, string onSecondary, string secondaryContainer, string onSecondaryContainer,
        string tertiary, string onTertiary, string tertiaryContainer, string onTertiaryContainer,
        string background, string onBackground, string surface, string onSurface, string surfaceVariant, string pickerSurface, string onSurfaceVariant, string outline, string outlineVariant,
        string inverseSurface, string inverseOnSurface, string inversePrimary,
        string primaryDark, string onPrimaryDark, string primaryContainerDark, string onPrimaryContainerDark,
        string secondaryDark, string onSecondaryDark, string secondaryContainerDark, string onSecondaryContainerDark,
        string tertiaryDark, string onTertiaryDark, string tertiaryContainerDark, string onTertiaryContainerDark,
        string backgroundDark, string onBackgroundDark, string surfaceDark, string onSurfaceDark, string surfaceVariantDark, string pickerSurfaceDark, string onSurfaceVariantDark, string outlineDark, string outlineVariantDark,
        string inverseSurfaceDark, string inverseOnSurfaceDark, string inversePrimaryDark)
        => new Dictionary<string, string>
        {
            ["Primary"] = primary, ["OnPrimary"] = onPrimary, ["PrimaryContainer"] = primaryContainer, ["OnPrimaryContainer"] = onPrimaryContainer,
            ["Secondary"] = secondary, ["OnSecondary"] = onSecondary, ["SecondaryContainer"] = secondaryContainer, ["OnSecondaryContainer"] = onSecondaryContainer,
            ["Tertiary"] = tertiary, ["OnTertiary"] = onTertiary, ["TertiaryContainer"] = tertiaryContainer, ["OnTertiaryContainer"] = onTertiaryContainer,
            ["Background"] = background, ["OnBackground"] = onBackground, ["Surface"] = surface, ["OnSurface"] = onSurface, ["SurfaceVariant"] = surfaceVariant, ["PickerSurface"] = pickerSurface, ["OnSurfaceVariant"] = onSurfaceVariant, ["Outline"] = outline, ["OutlineVariant"] = outlineVariant,
            ["InverseSurface"] = inverseSurface, ["InverseOnSurface"] = inverseOnSurface, ["InversePrimary"] = inversePrimary,
            ["PrimaryDark"] = primaryDark, ["OnPrimaryDark"] = onPrimaryDark, ["PrimaryContainerDark"] = primaryContainerDark, ["OnPrimaryContainerDark"] = onPrimaryContainerDark,
            ["SecondaryDark"] = secondaryDark, ["OnSecondaryDark"] = onSecondaryDark, ["SecondaryContainerDark"] = secondaryContainerDark, ["OnSecondaryContainerDark"] = onSecondaryContainerDark,
            ["TertiaryDark"] = tertiaryDark, ["OnTertiaryDark"] = onTertiaryDark, ["TertiaryContainerDark"] = tertiaryContainerDark, ["OnTertiaryContainerDark"] = onTertiaryContainerDark,
            ["BackgroundDark"] = backgroundDark, ["OnBackgroundDark"] = onBackgroundDark, ["SurfaceDark"] = surfaceDark, ["OnSurfaceDark"] = onSurfaceDark, ["SurfaceVariantDark"] = surfaceVariantDark, ["PickerSurfaceDark"] = pickerSurfaceDark, ["OnSurfaceVariantDark"] = onSurfaceVariantDark, ["OutlineDark"] = outlineDark, ["OutlineVariantDark"] = outlineVariantDark,
            ["InverseSurfaceDark"] = inverseSurfaceDark, ["InverseOnSurfaceDark"] = inverseOnSurfaceDark, ["InversePrimaryDark"] = inversePrimaryDark,
            ["Gray100"] = background, ["Gray200"] = surfaceVariant, ["Gray300"] = outlineVariant, ["Gray400"] = outlineDark, ["Gray500"] = outline, ["Gray600"] = onSurfaceVariant, ["Gray900"] = onSurface,
            ["PrimaryDarkText"] = onSurface, ["SecondaryDarkText"] = onSurfaceVariant, ["MidnightBlue"] = onPrimaryContainer, ["OffBlack"] = onSurface, ["Magenta"] = tertiary,
        };
}