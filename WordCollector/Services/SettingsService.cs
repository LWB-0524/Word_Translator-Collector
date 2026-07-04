using System.IO;
using System.Text.Json;
using WordCollector.Models;

namespace WordCollector.Services;

public class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _settingsFilePath;
    private AppSettings? _cachedSettings;

    public SettingsService()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WordCollector");
        Directory.CreateDirectory(appData);
        _settingsFilePath = Path.Combine(appData, "settings.json");
    }

    internal SettingsService(string settingsFilePath)
    {
        _settingsFilePath = settingsFilePath;
    }

    public AppSettings Load()
    {
        if (_cachedSettings != null)
            return _cachedSettings;

        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var loadedSettings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)
                    ?? AppSettings.CreateDefaults();

                if (SecretProtector.TryUnprotect(
                        loadedSettings.AiApiKey, out var apiKey, out var wasProtected))
                {
                    loadedSettings.AiApiKey = apiKey;
                    _cachedSettings = loadedSettings;

                    if (!wasProtected && !string.IsNullOrEmpty(apiKey))
                        Save(loadedSettings);
                }
                else
                {
                    loadedSettings.AiApiKey = string.Empty;
                    _cachedSettings = loadedSettings;
                    System.Diagnostics.Debug.WriteLine(
                        "Failed to decrypt API key. The settings file may belong to another Windows user.");
                }
            }
            else
            {
                _cachedSettings = AppSettings.CreateDefaults();
                Save(_cachedSettings);
            }
        }
        catch
        {
            _cachedSettings = AppSettings.CreateDefaults();
        }

        return _cachedSettings;
    }

    public void Save(AppSettings settings)
    {
        string? temporaryPath = null;
        try
        {
            var persistedSettings = settings.Copy();
            persistedSettings.AiApiKey = SecretProtector.Protect(settings.AiApiKey);
            var json = JsonSerializer.Serialize(persistedSettings, JsonOptions);

            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            temporaryPath = _settingsFilePath + ".tmp";
            File.WriteAllText(temporaryPath, json);
            File.Move(temporaryPath, _settingsFilePath, overwrite: true);
            _cachedSettings = settings;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
            if (temporaryPath != null && File.Exists(temporaryPath))
            {
                try { File.Delete(temporaryPath); } catch { }
            }
        }
    }

    public string SettingsFilePath => _settingsFilePath;
}
