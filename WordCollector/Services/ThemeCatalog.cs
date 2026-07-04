namespace WordCollector.Services;

public sealed record ThemeAccentOption(
    string Code,
    string DisplayName,
    string LightColor,
    string LightHoverColor,
    string LightPressedColor,
    string LightSoftColor,
    string DarkColor,
    string DarkHoverColor,
    string DarkPressedColor,
    string DarkSoftColor,
    string ForegroundColor);

public sealed record ThemeModeOption(string Code, string DisplayName);

public sealed record ThemePalette(
    string Mode,
    string AccentCode,
    IReadOnlyDictionary<string, string> Colors);

public static class ThemeCatalog
{
    public static IReadOnlyList<ThemeModeOption> Modes { get; } =
        new[] { new ThemeModeOption("light", "日间"), new ThemeModeOption("dark", "夜间") };

    public static IReadOnlyList<ThemeAccentOption> Accents { get; } =
        new[]
        {
            new ThemeAccentOption("blue", "海蓝", "#2563EB", "#1D4ED8", "#1E40AF", "#EFF6FF",
                "#4D7CFE", "#6B8FFE", "#3F6FEA", "#172554", "#FFFFFF"),
            new ThemeAccentOption("purple", "靛紫", "#7C3AED", "#6D28D9", "#5B21B6", "#F5F3FF",
                "#9B7BFF", "#AD93FF", "#8767ED", "#2E1F47", "#FFFFFF"),
            new ThemeAccentOption("teal", "青绿", "#0F766E", "#0D665F", "#115E59", "#F0FDFA",
                "#2DD4BF", "#5EEAD4", "#20BCA9", "#123B38", "#FFFFFF"),
            new ThemeAccentOption("amber", "琥珀", "#D97706", "#B45309", "#92400E", "#FFFBEB",
                "#F59E0B", "#FBBF24", "#D88908", "#422B0C", "#1F2937"),
            new ThemeAccentOption("rose", "玫红", "#E11D48", "#BE123C", "#9F1239", "#FFF1F2",
                "#FB7185", "#FDA4AF", "#E85E75", "#4A1D28", "#FFFFFF")
        };

    public static ThemePalette Resolve(string? mode, string? accentCode)
    {
        var normalizedMode = string.Equals(mode, "dark", StringComparison.OrdinalIgnoreCase)
            ? "dark"
            : "light";
        var accent = Accents.FirstOrDefault(option =>
                         string.Equals(option.Code, accentCode, StringComparison.OrdinalIgnoreCase))
                     ?? Accents[0];

        var colors = normalizedMode == "dark"
            ? CreateDarkPalette(accent)
            : CreateLightPalette(accent);
        return new ThemePalette(normalizedMode, accent.Code, colors);
    }

    private static IReadOnlyDictionary<string, string> CreateLightPalette(ThemeAccentOption accent) =>
        new Dictionary<string, string>
        {
            ["PrimaryBrush"] = accent.LightColor,
            ["PrimaryDarkBrush"] = accent.LightHoverColor,
            ["PrimaryPressedBrush"] = accent.LightPressedColor,
            ["PrimaryLightBrush"] = accent.LightSoftColor,
            ["PrimaryForegroundBrush"] = accent.ForegroundColor,
            ["BgBrush"] = "#F6F7F9",
            ["SurfaceBrush"] = "#FFFFFF",
            ["WindowSurfaceBrush"] = "#D9FFFFFF",
            ["WindowChromeBrush"] = "#E6FFFFFF",
            ["SurfaceAltBrush"] = "#F8FAFC",
            ["SubtleBgBrush"] = "#F1F3F6",
            ["TextPrimaryBrush"] = "#18202B",
            ["TextSecondaryBrush"] = "#344054",
            ["TextTertiaryBrush"] = "#667085",
            ["BorderBrush"] = "#DDE2E8",
            ["BorderStrongBrush"] = "#C6CDD6",
            ["BorderFocusBrush"] = accent.LightColor,
            ["HeaderBgBrush"] = "#FFFFFF",
            ["HeaderFgBrush"] = "#18202B",
            ["StatusBgBrush"] = "#F8FAFC",
            ["PopupBgBrush"] = "#FFFFFF",
            ["SuccessBrush"] = "#16855B",
            ["SuccessLightBrush"] = "#ECFDF3",
            ["DangerBrush"] = "#D92D20",
            ["DangerLightBrush"] = "#FEF3F2",
            ["DangerBorderBrush"] = "#FECDCA",
            ["FamNewBrush"] = "#D92D20",
            ["FamLearningBrush"] = "#B54708",
            ["FamMasteredBrush"] = "#16855B"
        };

    private static IReadOnlyDictionary<string, string> CreateDarkPalette(ThemeAccentOption accent) =>
        new Dictionary<string, string>
        {
            ["PrimaryBrush"] = accent.DarkColor,
            ["PrimaryDarkBrush"] = accent.DarkHoverColor,
            ["PrimaryPressedBrush"] = accent.DarkPressedColor,
            ["PrimaryLightBrush"] = accent.DarkSoftColor,
            ["PrimaryForegroundBrush"] = accent.ForegroundColor,
            ["BgBrush"] = "#0B0E13",
            ["SurfaceBrush"] = "#101318",
            ["WindowSurfaceBrush"] = "#E6101318",
            ["WindowChromeBrush"] = "#F0101318",
            ["SurfaceAltBrush"] = "#171B22",
            ["SubtleBgBrush"] = "#1D232C",
            ["TextPrimaryBrush"] = "#F2F4F7",
            ["TextSecondaryBrush"] = "#D0D5DD",
            ["TextTertiaryBrush"] = "#98A2B3",
            ["BorderBrush"] = "#303743",
            ["BorderStrongBrush"] = "#475467",
            ["BorderFocusBrush"] = accent.DarkColor,
            ["HeaderBgBrush"] = "#101318",
            ["HeaderFgBrush"] = "#F2F4F7",
            ["StatusBgBrush"] = "#12161D",
            ["PopupBgBrush"] = "#171B22",
            ["SuccessBrush"] = "#47CD89",
            ["SuccessLightBrush"] = "#153A2D",
            ["DangerBrush"] = "#F97066",
            ["DangerLightBrush"] = "#471B18",
            ["DangerBorderBrush"] = "#7A271A",
            ["FamNewBrush"] = "#F97066",
            ["FamLearningBrush"] = "#FEC84B",
            ["FamMasteredBrush"] = "#47CD89"
        };
}
