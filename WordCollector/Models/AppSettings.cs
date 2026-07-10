using System.Text.Json.Serialization;
using WordCollector.Helpers;

namespace WordCollector.Models;

public class AppSettings
{
    // AI Configuration
    public string AiProvider { get; set; } = "openai";
    public string AiApiKey { get; set; } = string.Empty;
    public string AiBaseUrl { get; set; } = "https://api.openai.com";
    public string AiModel { get; set; } = "gpt-4o-mini";

    // TTS Configuration
    public bool TtsEnabled { get; set; } = true;
    public string TtsVoiceName { get; set; } = "auto";
    public int TtsRate { get; set; } = 0; // -10 to 10, 0 = normal
    public bool AutoSpeakAfterQuery { get; set; } = false;

    // Window Behavior
    public bool AlwaysOnTop { get; set; } = true;
    public bool AutoHideAfterQuery { get; set; } = false;

    // Appearance
    public string ThemeMode { get; set; } = "light";
    public string AccentColor { get; set; } = "blue";

    // Ctrl+Enter Behavior: "hide", "clear", "keep"
    public string CtrlEnterBehavior { get; set; } = "clear";

    // Global Hotkeys (canonical form, e.g. "Ctrl+Shift+Space")
    public string ShowWindowHotkey { get; set; } = "Ctrl+Shift+Space";
    public string QuickQueryHotkey { get; set; } = "Ctrl+Shift+Q";

    // Window Position (saved on exit)
    public double WindowTop { get; set; } = 200;
    public double WindowLeft { get; set; } = 600;
    public double WindowWidth { get; set; } = WindowSizePolicy.DefaultWidth;
    public double WindowHeight { get; set; } = WindowSizePolicy.DefaultHeight;

    internal AppSettings Copy() => (AppSettings)MemberwiseClone();

    public static AppSettings CreateDefaults()
    {
        return new AppSettings
        {
            AiProvider = "openai",
            AiApiKey = string.Empty,
            AiBaseUrl = "https://api.openai.com",
            AiModel = "gpt-4o-mini",
            TtsEnabled = true,
            TtsVoiceName = "auto",
            TtsRate = 0,
            AutoSpeakAfterQuery = false,
            AlwaysOnTop = true,
            AutoHideAfterQuery = false,
            ThemeMode = "light",
            AccentColor = "blue",
            CtrlEnterBehavior = "clear",
            ShowWindowHotkey = "Ctrl+Shift+Space",
            QuickQueryHotkey = "Ctrl+Shift+Q",
            WindowTop = 200,
            WindowLeft = 600,
            WindowWidth = WindowSizePolicy.DefaultWidth,
            WindowHeight = WindowSizePolicy.DefaultHeight
        };
    }
}
