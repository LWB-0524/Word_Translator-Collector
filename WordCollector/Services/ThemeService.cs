using System.Windows;
using System.Windows.Media;

namespace WordCollector.Services;

public sealed class ThemeService
{
    private readonly SettingsService _settingsService;

    public ThemeService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public ThemePalette CurrentPalette { get; private set; } = ThemeCatalog.Resolve("light", "blue");

    public void ApplySavedTheme()
    {
        var settings = _settingsService.Load();
        Apply(settings.ThemeMode, settings.AccentColor, persist: false);
    }

    public ThemePalette Apply(string? mode, string? accentCode, bool persist = true)
    {
        var palette = ThemeCatalog.Resolve(mode, accentCode);
        CurrentPalette = palette;

        if (Application.Current != null)
        {
            foreach (var (key, colorValue) in palette.Colors)
            {
                var color = (Color)ColorConverter.ConvertFromString(colorValue)!;
                var brush = new SolidColorBrush(color);
                brush.Freeze();
                Application.Current.Resources[key] = brush;
            }
        }

        if (persist)
        {
            var settings = _settingsService.Load();
            settings.ThemeMode = palette.Mode;
            settings.AccentColor = palette.AccentCode;
            _settingsService.Save(settings);
        }

        return palette;
    }
}
