using System.Threading;
using System.Windows;
using System.Windows.Interop;
using WordCollector.NativeMethods;
using WordCollector.Services;
using WordCollector.ViewModels;
using WordCollector.Views;

namespace WordCollector;

public partial class App : Application
{
    private static readonly Mutex SingleInstanceMutex = new(true, "WordCollector_SingleInstance_Mutex");
    private static readonly uint ShowInstanceMessage =
        Win32.RegisterWindowMessage("WordCollector.ShowExistingInstance");
    private static bool _ownsMutex;
    private bool _isShuttingDown;

    private MainWindow? _mainWindow;
    private MainViewModel? _mainViewModel;
    private TrayService? _trayService;
    private HotkeyService? _hotkeyService;
    private TextToSpeechService? _ttsService;
    private SettingsService? _settingsService;
    private ThemeService? _themeService;
    private AiService? _aiService;
    private DictionaryLookupService? _dictionaryLookupService;
    private LookupService? _lookupService;
#if DEBUG
    private string? _visualQaScreen;
#endif

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
#if DEBUG
        _visualQaScreen = e.Args
            .FirstOrDefault(argument => argument.StartsWith("--visual-qa=", StringComparison.OrdinalIgnoreCase))
            ?.Split('=', 2)[1]
            ?? Environment.GetEnvironmentVariable("WORDCOLLECTOR_VISUAL_QA_SCREEN")
            ?? Environment.GetEnvironmentVariable("WORDCOLLECTOR_VISUAL_QA");
#endif

        if (!SingleInstanceMutex.WaitOne(TimeSpan.Zero, true))
        {
            // 通知已运行的实例显示主窗口，本实例静默退出。
            if (ShowInstanceMessage != 0)
                Win32.PostMessage(
                    (IntPtr)Win32.HWND_BROADCAST, ShowInstanceMessage, IntPtr.Zero, IntPtr.Zero);
            _isShuttingDown = true;
            Shutdown();
            return;
        }
        _ownsMutex = true;

        DatabaseService databaseService;
        try
        {
            _settingsService = new SettingsService();
            _themeService = new ThemeService(_settingsService);
            _themeService.ApplySavedTheme();
            databaseService = new DatabaseService();
            _ttsService = new TextToSpeechService(_settingsService);
            _aiService = new AiService(_settingsService);
            _dictionaryLookupService = new DictionaryLookupService();
            _lookupService = new LookupService(databaseService, _dictionaryLookupService, _aiService);

#if DEBUG
            if (!string.IsNullOrWhiteSpace(_visualQaScreen))
                _themeService.Apply(
                    string.Equals(_visualQaScreen, "light", StringComparison.OrdinalIgnoreCase) ? "light" : "dark",
                    "blue");
#endif
        }
        catch (Exception ex)
        {
            MessageBox.Show($"初始化失败：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            _isShuttingDown = true;
            Shutdown();
            return;
        }

        _mainViewModel = new MainViewModel(
            databaseService, _lookupService, _ttsService, _settingsService, _themeService);

#if DEBUG
        if (!string.IsNullOrWhiteSpace(_visualQaScreen))
        {
            _mainViewModel.InputText = "break the ice";
            _mainViewModel.Phonetic = "/breɪk ði aɪs/";
            _mainViewModel.MeaningZh = "打破僵局；活跃气氛";
            _mainViewModel.BriefExplanation = "常用于初次见面或气氛拘谨时，通过轻松的话题让交流自然起来。";
            _mainViewModel.ExampleEn = "A quick game helped break the ice.";
            _mainViewModel.ExampleZh = "一个小游戏帮助大家打破了僵局。";
            _mainViewModel.KeyExpressions = "break the ice：打破僵局";
            _mainViewModel.StatusMessage = "已自动保存到今日沉淀";
        }
#endif

        _mainWindow = new MainWindow(_mainViewModel);

#if DEBUG
        if (string.Equals(_visualQaScreen, "settings", StringComparison.OrdinalIgnoreCase))
        {
            var settingsWindow = new SettingsWindow(_mainViewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };
            settingsWindow.Show();
            settingsWindow.Activate();
            return;
        }

        if (string.Equals(_visualQaScreen, "review", StringComparison.OrdinalIgnoreCase))
        {
            var reviewWindow = new ReviewWindow(databaseService, _ttsService, _mainViewModel)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Topmost = true
            };
            reviewWindow.Show();
            reviewWindow.Activate();
            return;
        }
#endif

        _trayService = new TrayService(_settingsService);
        _trayService.Initialize(
            _mainWindow,
            () => _mainViewModel.ShowReviewCommand.Execute(null),
            () => _mainViewModel.ShowSettingsCommand.Execute(null),
            DoExit);

        _hotkeyService = new HotkeyService(_mainViewModel, _settingsService);
        _mainWindow.SourceInitialized += (_, _) =>
        {
            var hotkeyWarning = _hotkeyService.Register(_mainWindow);
            if (hotkeyWarning != null)
                _trayService.ShowBalloonTip("WordCollector 快捷键", hotkeyWarning);

            // 监听来自后启动实例的广播，激活主窗口。
            var handle = new WindowInteropHelper(_mainWindow).Handle;
            if (handle != IntPtr.Zero)
                HwndSource.FromHwnd(handle)?.AddHook(ShowInstanceHook);
        };

        // 设置里改动全局快捷键后即时重新注册。
        _mainViewModel.HotkeysChanged += () =>
        {
            var warning = _hotkeyService?.ApplyFromSettings();
            if (warning != null)
                _trayService?.ShowBalloonTip("WordCollector 快捷键", warning);
        };

        _mainWindow.Closing += (_, args) =>
        {
            _ttsService?.Stop();
            SaveWindowBounds();
            if (_isShuttingDown) return;
            args.Cancel = true;
            _mainWindow.Hide();
        };

        var voiceHint = _ttsService.GetEnglishVoiceHint();
        if (voiceHint != null)
            _trayService.ShowBalloonTip("WordCollector", voiceHint);

        // 窗口位置、尺寸和置顶状态已在 MainWindow 构造函数中根据设置初始化。
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    public static void RequestExit()
    {
        if (Current is App app)
            app.DoExit();
    }

    private IntPtr ShowInstanceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (ShowInstanceMessage != 0 && (uint)msg == ShowInstanceMessage)
        {
            _mainViewModel?.ShowWindow();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void SaveWindowBounds()
    {
        if (_mainWindow == null || _settingsService == null) return;
        var settings = _settingsService.Load();
        if (_mainWindow.IsVisible)
        {
            settings.WindowLeft = _mainWindow.Left;
            settings.WindowTop = _mainWindow.Top;
        }
        settings.WindowWidth = _mainWindow.Width;
        settings.WindowHeight = _mainWindow.Height;
        _settingsService.Save(settings);
    }

    private void DoExit()
    {
        try
        {
            _ttsService?.Stop();
            _ttsService?.Dispose();
            _aiService?.Dispose();
            _dictionaryLookupService?.Dispose();
            _hotkeyService?.Dispose();
            _trayService?.Dispose();
            SaveWindowBounds();
        }
        catch
        {
            // 清理失败不应阻止应用退出。
        }

        _isShuttingDown = true;
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyService?.Dispose();
        _trayService?.Dispose();
        _ttsService?.Dispose();
        _aiService?.Dispose();
        _dictionaryLookupService?.Dispose();

        if (_ownsMutex)
        {
            try { SingleInstanceMutex.ReleaseMutex(); } catch { }
            SingleInstanceMutex.Dispose();
        }

        base.OnExit(e);
    }
}
