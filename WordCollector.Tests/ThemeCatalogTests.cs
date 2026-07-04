using WordCollector.Services;

namespace WordCollector.Tests;

public class ThemeCatalogTests
{
    [Fact]
    public void AccentOptions_ExposeFiveNamedThemes()
    {
        Assert.Equal(5, ThemeCatalog.Accents.Count);
        Assert.Equal(
            new[] { "blue", "purple", "teal", "amber", "rose" },
            ThemeCatalog.Accents.Select(option => option.Code));
    }

    [Fact]
    public void Resolve_ReturnsDarkPurplePalette()
    {
        var palette = ThemeCatalog.Resolve("dark", "purple");

        Assert.Equal("dark", palette.Mode);
        Assert.Equal("purple", palette.AccentCode);
        Assert.Equal("#101318", palette.Colors["SurfaceBrush"]);
        Assert.Equal("#F2F4F7", palette.Colors["TextPrimaryBrush"]);
        Assert.Equal("#9B7BFF", palette.Colors["PrimaryBrush"]);
    }

    [Fact]
    public void Resolve_FallsBackToLightBlueForUnknownValues()
    {
        var palette = ThemeCatalog.Resolve("sepia", "unknown");

        Assert.Equal("light", palette.Mode);
        Assert.Equal("blue", palette.AccentCode);
        Assert.Equal("#FFFFFF", palette.Colors["SurfaceBrush"]);
        Assert.Equal("#2563EB", palette.Colors["PrimaryBrush"]);
    }

    [Fact]
    public void Resolve_ProvidesReadableTranslucentWindowSurfacesForBothModes()
    {
        var lightPalette = ThemeCatalog.Resolve("light", "blue");
        var darkPalette = ThemeCatalog.Resolve("dark", "blue");

        Assert.Equal("#D9FFFFFF", lightPalette.Colors["WindowSurfaceBrush"]);
        Assert.Equal("#E6FFFFFF", lightPalette.Colors["WindowChromeBrush"]);
        Assert.Equal("#E6101318", darkPalette.Colors["WindowSurfaceBrush"]);
        Assert.Equal("#F0101318", darkPalette.Colors["WindowChromeBrush"]);
    }
}
