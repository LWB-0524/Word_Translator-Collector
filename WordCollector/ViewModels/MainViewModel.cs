using System.Text.Json;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using WordCollector.Helpers;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly LookupService _lookupService;
    private readonly TextToSpeechService _ttsService;
    internal readonly SettingsService SettingsService;
    internal ThemeService ThemeService { get; }
    private Window? _ownerWindow;

    public TextToSpeechService TtsService => _ttsService;
    public DatabaseService DbService => _databaseService;

    public MainViewModel(
        DatabaseService databaseService,
        LookupService lookupService,
        TextToSpeechService ttsService,
        SettingsService settingsService,
        ThemeService themeService)
    {
        _databaseService = databaseService;
        _lookupService = lookupService;
        _ttsService = ttsService;
        SettingsService = settingsService;
        ThemeService = themeService;

        _ttsService.SpeakStarted += (_, _) =>
        {
            IsSpeaking = true;
            StatusMessage = "正在朗读…";
        };
        _ttsService.SpeakCompleted += (_, _) =>
        {
            IsSpeaking = false;
            StatusMessage = "朗读已停止";
        };

        QueryCommand = new RelayCommand(async () => await QueryAsync(), () => !IsQuerying);
        QueryAndActionCommand = new RelayCommand(async () => await QueryAndActionAsync(), () => !IsQuerying);
        ClearCommand = new RelayCommand(Clear);
        SpeakCommand = new RelayCommand(ToggleSpeak, () => !string.IsNullOrWhiteSpace(InputText));
        ToggleTopmostCommand = new RelayCommand(ToggleTopmost);
        ShowReviewCommand = new RelayCommand(ShowReview);
        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ShowWindowCommand = new RelayCommand(ShowWindow);
        HideWindowCommand = new RelayCommand(HideWindow);
        QuickQueryCommand = new RelayCommand(QuickQuery, () => !IsQuerying);
        SetThemeModeCommand = new RelayCommand<string>(SetThemeMode);
        SetAccentColorCommand = new RelayCommand<string>(SetAccentColor);
    }

    public void SetOwnerWindow(Window window) => _ownerWindow = window;

    /// <summary>全局快捷键设置发生变化时触发，由宿主（App）负责重新注册。</summary>
    public event Action? HotkeysChanged;

    public void NotifyHotkeysChanged() => HotkeysChanged?.Invoke();

    private string _inputText = string.Empty;
    public string InputText
    {
        get => _inputText;
        set
        {
            if (SetProperty(ref _inputText, value))
                (SpeakCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private string _meaningZh = string.Empty;
    public string MeaningZh { get => _meaningZh; set => SetProperty(ref _meaningZh, value); }

    private string _briefExplanation = string.Empty;
    public string BriefExplanation { get => _briefExplanation; set => SetProperty(ref _briefExplanation, value); }

    private string _phonetic = string.Empty;
    public string Phonetic { get => _phonetic; set => SetProperty(ref _phonetic, value); }

    private string _exampleEn = string.Empty;
    public string ExampleEn { get => _exampleEn; set => SetProperty(ref _exampleEn, value); }

    private string _exampleZh = string.Empty;
    public string ExampleZh { get => _exampleZh; set => SetProperty(ref _exampleZh, value); }

    private string _keyExpressions = string.Empty;
    public string KeyExpressions { get => _keyExpressions; set => SetProperty(ref _keyExpressions, value); }

    private string _statusMessage = string.Empty;
    public string StatusMessage { get => _statusMessage; set => SetProperty(ref _statusMessage, value); }

    private bool _isQuerying;
    public bool IsQuerying
    {
        get => _isQuerying;
        set
        {
            if (!SetProperty(ref _isQuerying, value)) return;
            (QueryCommand as RelayCommand)?.RaiseCanExecuteChanged();
            (QueryAndActionCommand as RelayCommand)?.RaiseCanExecuteChanged();
        }
    }

    private bool _isSpeaking;
    public bool IsSpeaking { get => _isSpeaking; set => SetProperty(ref _isSpeaking, value); }

    private bool _isAlwaysOnTop = true;
    public bool IsAlwaysOnTop
    {
        get => _isAlwaysOnTop;
        set
        {
            if (!SetProperty(ref _isAlwaysOnTop, value)) return;
            var settings = SettingsService.Load();
            settings.AlwaysOnTop = value;
            SettingsService.Save(settings);
            if (_ownerWindow != null)
                _ownerWindow.Topmost = value;
        }
    }

    private bool _hasApiKey;
    public bool HasApiKey { get => _hasApiKey; set => SetProperty(ref _hasApiKey, value); }

    private string _themeMode = "light";
    public string ThemeMode { get => _themeMode; private set => SetProperty(ref _themeMode, value); }

    private string _accentColor = "blue";
    public string AccentColor { get => _accentColor; private set => SetProperty(ref _accentColor, value); }

    public IReadOnlyList<ThemeAccentOption> ThemeAccents => ThemeCatalog.Accents;

    public ICommand QueryCommand { get; }
    public ICommand QueryAndActionCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SpeakCommand { get; }
    public ICommand ToggleTopmostCommand { get; }
    public ICommand ShowReviewCommand { get; }
    public ICommand ShowSettingsCommand { get; }
    public ICommand ShowWindowCommand { get; }
    public ICommand HideWindowCommand { get; }
    public ICommand QuickQueryCommand { get; }
    public ICommand SetThemeModeCommand { get; }
    public ICommand SetAccentColorCommand { get; }

    public void RefreshSettings()
    {
        var settings = SettingsService.Load();
        IsAlwaysOnTop = settings.AlwaysOnTop;
        HasApiKey = !string.IsNullOrWhiteSpace(settings.AiApiKey);
        var palette = ThemeCatalog.Resolve(settings.ThemeMode, settings.AccentColor);
        ThemeMode = palette.Mode;
        AccentColor = palette.AccentCode;
    }

    private void SetThemeMode(string? mode)
    {
        var palette = ThemeService.Apply(mode, AccentColor);
        ThemeMode = palette.Mode;
        AccentColor = palette.AccentCode;
    }

    private void SetAccentColor(string? accentColor)
    {
        var palette = ThemeService.Apply(ThemeMode, accentColor);
        ThemeMode = palette.Mode;
        AccentColor = palette.AccentCode;
    }

    private Task QueryAsync() => DoQuery(isCtrlEnter: false);
    private Task QueryAndActionAsync() => DoQuery(isCtrlEnter: true);

    private async Task DoQuery(bool isCtrlEnter)
    {
        var text = InputText?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            StatusMessage = "请先输入英文内容";
            return;
        }

        IsQuerying = true;
        StatusMessage = "正在查询…";
        ClearResult();

        try
        {
            var stopwatch = Stopwatch.StartNew();
            var response = await _lookupService.QueryAsync(text);
            stopwatch.Stop();
            var result = response.Result;
            var rawResponse = response.RawResponse;
            var error = response.Error;
            if (error != null)
            {
                StatusMessage = error;
                return;
            }

            if (result != null)
            {
                MeaningZh = result.MeaningZh;
                Phonetic = result.Phonetic;
                BriefExplanation = result.BriefExplanation;
                ExampleEn = result.ExampleEn;
                ExampleZh = result.ExampleZh;
                if (result.KeyExpressions.Count > 0)
                {
                    KeyExpressions = string.Join("；", result.KeyExpressions.Select(item =>
                        $"{item.Expression}：{item.MeaningZh}"));
                }

                await SaveToDatabase(text, result, rawResponse);
                StatusMessage = $"{GetSourceLabel(response.Source)} · {stopwatch.ElapsedMilliseconds} ms · {StatusMessage}";

                var settings = SettingsService.Load();
                if (settings.AutoSpeakAfterQuery && settings.TtsEnabled)
                {
                    _ttsService.SpeakAsync(text);
                    UpdateSpokenCount();
                }

                switch (BehaviorOptions.ResolvePostQueryAction(
                            isCtrlEnter, settings.AutoHideAfterQuery, settings.CtrlEnterBehavior))
                {
                    case PostQueryAction.Hide:
                        HideWindow();
                        break;
                    case PostQueryAction.Clear:
                        Clear();
                        break;
                }
            }
            else if (rawResponse != null)
            {
                StatusMessage = "解析失败，已显示原始解释";
                MeaningZh = rawResponse;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"查询失败：{ex.Message}";
        }
        finally
        {
            IsQuerying = false;
        }
    }

    private static string GetSourceLabel(LookupSource source) => source switch
    {
        LookupSource.Local => "本地",
        LookupSource.Dictionary => "词典",
        _ => "AI"
    };

    private async Task SaveToDatabase(string text, AiExplanationResult result, string? rawResponse)
    {
        try
        {
            var normalized = TextNormalizer.Normalize(text);
            var today = DateTime.Now.ToString("yyyy-MM-dd");
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var existing = await Task.Run(
                () => _databaseService.FindByNormalizedTextAndDate(normalized, today));

            if (existing != null)
            {
                await Task.Run(
                    () => _databaseService.UpdateLookupCount(existing.Id, existing.LookupCount + 1));
                StatusMessage = "今日已记录，已更新查询次数";
            }
            else
            {
                var historical = await Task.Run(
                    () => _databaseService.FindByNormalizedText(normalized));
                var item = new VocabularyItem
                {
                    Text = text,
                    NormalizedText = normalized,
                    ItemType = result.ItemType,
                    Phonetic = result.Phonetic,
                    MeaningZh = result.MeaningZh,
                    BriefExplanation = result.BriefExplanation,
                    DetailedExplanation = result.DetailedExplanation,
                    ExampleEn = result.ExampleEn,
                    ExampleZh = result.ExampleZh,
                    KeyExpressionsJson = result.KeyExpressions.Count > 0
                        ? JsonSerializer.Serialize(result.KeyExpressions)
                        : null,
                    RawAiResponse = rawResponse,
                    DateAdded = today,
                    CreatedAt = now,
                    UpdatedAt = now,
                    LookupCount = 1,
                    Familiarity = 0,
                    SpokenCount = 0
                };

                await Task.Run(() => _databaseService.Insert(item));
                StatusMessage = historical != null
                    ? "之前记录过，已加入今日沉淀"
                    : "已自动保存到今日沉淀";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败：{ex.Message}";
        }
    }

    private void Clear()
    {
        InputText = string.Empty;
        ClearResult();
        _ttsService.Stop();
        StatusMessage = string.Empty;
        _ownerWindow?.Activate();
    }

    private void ClearResult()
    {
        MeaningZh = string.Empty;
        Phonetic = string.Empty;
        BriefExplanation = string.Empty;
        ExampleEn = string.Empty;
        ExampleZh = string.Empty;
        KeyExpressions = string.Empty;
    }

    private void ToggleSpeak()
    {
        if (string.IsNullOrWhiteSpace(InputText))
        {
            StatusMessage = "请先输入英文内容";
            return;
        }

        if (_ttsService.IsSpeaking)
        {
            _ttsService.Stop();
            StatusMessage = "朗读已停止";
            return;
        }

        if (!SettingsService.Load().TtsEnabled)
        {
            StatusMessage = "朗读功能已禁用，请在设置中启用";
            return;
        }

        _ttsService.SpeakAsync(InputText);
        UpdateSpokenCount();
    }

    public void UpdateSpokenCount()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrWhiteSpace(text)) return;

        var normalized = TextNormalizer.Normalize(text);
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _ = Task.Run(() =>
        {
            try
            {
                var existing = _databaseService.FindByNormalizedTextAndDate(normalized, today);
                if (existing != null)
                    _databaseService.UpdateSpokenCount(existing.Id, existing.SpokenCount + 1);
            }
            catch
            {
                // 朗读计数不影响主要流程。
            }
        });
    }

    private void ToggleTopmost() => IsAlwaysOnTop = !IsAlwaysOnTop;

    public void ShowWindow()
    {
        if (_ownerWindow == null) return;
        _ownerWindow.Show();
        _ownerWindow.WindowState = WindowState.Normal;
        _ownerWindow.Activate();
        _ownerWindow.Topmost = IsAlwaysOnTop;
    }

    private async void QuickQuery()
    {
        ShowWindow();
        try
        {
            var clipboardText = Clipboard.GetText();
            if (string.IsNullOrWhiteSpace(clipboardText)) return;
            InputText = clipboardText;
            await QueryAsync();
        }
        catch
        {
            // 剪贴板可能暂时被其他进程占用。
        }
    }

    public void HideWindow()
    {
        _ttsService.Stop();
        _ownerWindow?.Hide();
    }

    private void ShowReview()
    {
        var reviewWindow = new Views.ReviewWindow(_databaseService, _ttsService, this)
        {
            Owner = _ownerWindow
        };
        reviewWindow.Show();
    }

    private void ShowSettings()
    {
        var settingsWindow = new Views.SettingsWindow(this) { Owner = _ownerWindow };
        settingsWindow.ShowDialog();
    }
}
