using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using WordCollector.Helpers;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly SettingsService _settingsService;
    private readonly TextToSpeechService _ttsService;
    private readonly MainViewModel _mainViewModel;
    private readonly ThemeService _themeService;

    public SettingsViewModel(
        SettingsService settingsService,
        TextToSpeechService ttsService,
        MainViewModel mainViewModel,
        ThemeService themeService)
    {
        _settingsService = settingsService;
        _ttsService = ttsService;
        _mainViewModel = mainViewModel;
        _themeService = themeService;

        var settings = settingsService.Load();
        AiProvider = settings.AiProvider;
        AiApiKey = settings.AiApiKey;
        AiBaseUrl = settings.AiBaseUrl;
        AiModel = settings.AiModel;
        TtsEnabled = settings.TtsEnabled;
        TtsVoiceName = BehaviorOptions.ToDisplayVoiceName(settings.TtsVoiceName);
        TtsRate = settings.TtsRate;
        AutoSpeakAfterQuery = settings.AutoSpeakAfterQuery;
        AlwaysOnTop = settings.AlwaysOnTop;
        AutoHideAfterQuery = settings.AutoHideAfterQuery;
        var palette = ThemeCatalog.Resolve(settings.ThemeMode, settings.AccentColor);
        ThemeMode = palette.Mode;
        AccentColor = palette.AccentCode;
        CtrlEnterBehavior = BehaviorOptions.ToDisplayCtrlEnterBehavior(settings.CtrlEnterBehavior);

        AvailableVoices = new ObservableCollection<string> { "自动" };
        try
        {
            foreach (var voice in _ttsService.GetAvailableEnglishVoices())
                AvailableVoices.Add(voice.VoiceInfo.Name);
            foreach (var voice in _ttsService.GetAllVoices())
            {
                if (!AvailableVoices.Contains(voice.VoiceInfo.Name))
                    AvailableVoices.Add(voice.VoiceInfo.Name);
            }
        }
        catch
        {
            // 系统未提供语音时保留“自动”选项。
        }

        CtrlEnterBehaviors = new ObservableCollection<string>(BehaviorOptions.CtrlEnterBehaviors);
        SaveCommand = new RelayCommand(async () => await SaveAsync());
        TestConnectionCommand = new RelayCommand(async () => await TestConnectionAsync());
        ResetDefaultsCommand = new RelayCommand(ResetDefaults);
    }

    private string _aiProvider = "openai";
    public string AiProvider
    {
        get => _aiProvider;
        set
        {
            var previousProvider = _aiProvider;
            if (!SetProperty(ref _aiProvider, value)) return;

            var previousDefaults = AiEndpointResolver.GetProviderDefaults(previousProvider);
            var newDefaults = AiEndpointResolver.GetProviderDefaults(value);
            if (newDefaults.BaseUrl != null &&
                (string.IsNullOrWhiteSpace(AiBaseUrl) || AiBaseUrl == previousDefaults.BaseUrl))
                AiBaseUrl = newDefaults.BaseUrl;
            if (newDefaults.Model != null &&
                (string.IsNullOrWhiteSpace(AiModel) || AiModel == previousDefaults.Model))
                AiModel = newDefaults.Model;
        }
    }

    private string _aiApiKey = string.Empty;
    public string AiApiKey { get => _aiApiKey; set => SetProperty(ref _aiApiKey, value); }

    private string _aiBaseUrl = "https://api.openai.com";
    public string AiBaseUrl { get => _aiBaseUrl; set => SetProperty(ref _aiBaseUrl, value); }

    private string _aiModel = "gpt-4o-mini";
    public string AiModel { get => _aiModel; set => SetProperty(ref _aiModel, value); }

    private bool _ttsEnabled = true;
    public bool TtsEnabled { get => _ttsEnabled; set => SetProperty(ref _ttsEnabled, value); }

    private string _ttsVoiceName = "自动";
    public string TtsVoiceName { get => _ttsVoiceName; set => SetProperty(ref _ttsVoiceName, value); }

    private int _ttsRate;
    public int TtsRate { get => _ttsRate; set => SetProperty(ref _ttsRate, value); }

    private bool _autoSpeakAfterQuery;
    public bool AutoSpeakAfterQuery { get => _autoSpeakAfterQuery; set => SetProperty(ref _autoSpeakAfterQuery, value); }

    private bool _alwaysOnTop = true;
    public bool AlwaysOnTop { get => _alwaysOnTop; set => SetProperty(ref _alwaysOnTop, value); }

    private bool _autoHideAfterQuery;
    public bool AutoHideAfterQuery { get => _autoHideAfterQuery; set => SetProperty(ref _autoHideAfterQuery, value); }

    private string _ctrlEnterBehavior = BehaviorOptions.ClearDisplayText;
    public string CtrlEnterBehavior { get => _ctrlEnterBehavior; set => SetProperty(ref _ctrlEnterBehavior, value); }

    private string _themeMode = "light";
    public string ThemeMode { get => _themeMode; set => SetProperty(ref _themeMode, value); }

    private string _accentColor = "blue";
    public string AccentColor { get => _accentColor; set => SetProperty(ref _accentColor, value); }

    private string _connectionStatus = string.Empty;
    public string ConnectionStatus { get => _connectionStatus; set => SetProperty(ref _connectionStatus, value); }

    private bool _isTesting;
    public bool IsTesting { get => _isTesting; set => SetProperty(ref _isTesting, value); }

    public ObservableCollection<string> AvailableVoices { get; }
    public IReadOnlyList<string> AiProviders => AiEndpointResolver.Providers;
    public IReadOnlyList<ThemeModeOption> ThemeModes => ThemeCatalog.Modes;
    public IReadOnlyList<ThemeAccentOption> ThemeAccents => ThemeCatalog.Accents;
    public ObservableCollection<string> CtrlEnterBehaviors { get; }

    public ICommand SaveCommand { get; }
    public ICommand TestConnectionCommand { get; }
    public ICommand ResetDefaultsCommand { get; }

    public async Task SaveAsync()
    {
        var candidate = new AppSettings
        {
            AiProvider = AiProvider,
            AiBaseUrl = AiBaseUrl,
            AiModel = AiModel
        };
        if (!AiEndpointResolver.TryResolve(candidate, out _, out _, out var validationError))
        {
            MessageBox.Show(validationError, "设置无效", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var settings = _settingsService.Load();
        settings.AiProvider = AiProvider;
        settings.AiApiKey = AiApiKey;
        settings.AiBaseUrl = AiBaseUrl;
        settings.AiModel = AiModel;
        settings.TtsEnabled = TtsEnabled;
        settings.TtsVoiceName = BehaviorOptions.ToStoredVoiceName(TtsVoiceName);
        settings.TtsRate = TtsRate;
        settings.AutoSpeakAfterQuery = AutoSpeakAfterQuery;
        settings.AlwaysOnTop = AlwaysOnTop;
        settings.AutoHideAfterQuery = AutoHideAfterQuery;
        var palette = ThemeCatalog.Resolve(ThemeMode, AccentColor);
        settings.ThemeMode = palette.Mode;
        settings.AccentColor = palette.AccentCode;
        settings.CtrlEnterBehavior = BehaviorOptions.ToStoredCtrlEnterBehavior(CtrlEnterBehavior);

        _settingsService.Save(settings);
        _themeService.Apply(settings.ThemeMode, settings.AccentColor, persist: false);
        _ttsService.ApplySettings();
        _mainViewModel.RefreshSettings();

        MessageBox.Show("设置已保存", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
        await Task.CompletedTask;
    }

    public async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(AiApiKey))
        {
            ConnectionStatus = "请先输入 API Key";
            return;
        }

        var savedSettings = _settingsService.Load();
        var originalProvider = savedSettings.AiProvider;
        var originalKey = savedSettings.AiApiKey;
        var originalUrl = savedSettings.AiBaseUrl;
        var originalModel = savedSettings.AiModel;

        savedSettings.AiProvider = AiProvider;
        savedSettings.AiApiKey = AiApiKey;
        savedSettings.AiBaseUrl = AiBaseUrl;
        savedSettings.AiModel = AiModel;
        IsTesting = true;
        ConnectionStatus = "正在测试连接…";

        try
        {
            _settingsService.Save(savedSettings);
            using var aiService = new AiService(_settingsService);
            var ok = await aiService.TestConnectionAsync();
            ConnectionStatus = ok ? "连接成功" : "连接失败，请检查 API Key、Base URL 和网络";
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"测试失败：{ex.Message}";
        }
        finally
        {
            IsTesting = false;
            savedSettings.AiProvider = originalProvider;
            savedSettings.AiApiKey = originalKey;
            savedSettings.AiBaseUrl = originalUrl;
            savedSettings.AiModel = originalModel;
            _settingsService.Save(savedSettings);
        }
    }

    public void ResetDefaults()
    {
        var result = MessageBox.Show(
            "确定要恢复默认设置吗？这会清除所有自定义配置。",
            "确认恢复默认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        var defaults = AppSettings.CreateDefaults();
        _settingsService.Save(defaults);
        AiProvider = defaults.AiProvider;
        AiApiKey = defaults.AiApiKey;
        AiBaseUrl = defaults.AiBaseUrl;
        AiModel = defaults.AiModel;
        TtsEnabled = defaults.TtsEnabled;
        TtsVoiceName = "自动";
        TtsRate = defaults.TtsRate;
        AutoSpeakAfterQuery = defaults.AutoSpeakAfterQuery;
        AlwaysOnTop = defaults.AlwaysOnTop;
        AutoHideAfterQuery = defaults.AutoHideAfterQuery;
        ThemeMode = defaults.ThemeMode;
        AccentColor = defaults.AccentColor;
        CtrlEnterBehavior = BehaviorOptions.ToDisplayCtrlEnterBehavior(defaults.CtrlEnterBehavior);

        _ttsService.ApplySettings();
        _themeService.Apply(defaults.ThemeMode, defaults.AccentColor, persist: false);
        _mainViewModel.RefreshSettings();
    }
}
