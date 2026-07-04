using System.Text.Json;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class SettingsServiceSecurityTests
{
    [Fact]
    public void Save_EncryptsApiKeyAndLoadRestoresIt()
    {
        var path = CreateTemporarySettingsPath();
        try
        {
            var service = new SettingsService(path);
            var settings = AppSettings.CreateDefaults();
            settings.AiApiKey = "sk-sensitive-value";
            settings.ThemeMode = "dark";
            settings.AccentColor = "teal";

            service.Save(settings);

            var persisted = File.ReadAllText(path);
            Assert.DoesNotContain("sk-sensitive-value", persisted);

            var reloaded = new SettingsService(path).Load();
            Assert.Equal("sk-sensitive-value", reloaded.AiApiKey);
            Assert.Equal("dark", reloaded.ThemeMode);
            Assert.Equal("teal", reloaded.AccentColor);
        }
        finally
        {
            DeleteTemporarySettings(path);
        }
    }

    [Fact]
    public void Load_MigratesLegacyPlaintextApiKey()
    {
        var path = CreateTemporarySettingsPath();
        try
        {
            var legacy = AppSettings.CreateDefaults();
            legacy.AiApiKey = "legacy-plaintext-key";
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(legacy));

            var loaded = new SettingsService(path).Load();

            Assert.Equal("legacy-plaintext-key", loaded.AiApiKey);
            Assert.DoesNotContain("legacy-plaintext-key", File.ReadAllText(path));
        }
        finally
        {
            DeleteTemporarySettings(path);
        }
    }

    private static string CreateTemporarySettingsPath()
    {
        return Path.Combine(Path.GetTempPath(), "WordCollector.Tests", Guid.NewGuid().ToString("N"), "settings.json");
    }

    private static void DeleteTemporarySettings(string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (directory != null && Directory.Exists(directory))
            Directory.Delete(directory, recursive: true);
    }
}
