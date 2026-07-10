using System.Windows;
using System.Windows.Interop;
using WordCollector.Helpers;
using WordCollector.NativeMethods;
using WordCollector.ViewModels;

namespace WordCollector.Services;

public class HotkeyService : IDisposable
{
    private HwndSource? _hwndSource;
    private readonly MainViewModel _mainViewModel;
    private readonly SettingsService _settingsService;
    private readonly Dictionary<int, GlobalHotkeyAction> _actionsById = new();
    private readonly HashSet<int> _registeredHotkeyIds = new();

    internal static IReadOnlyList<GlobalHotkeyDefinition> GlobalHotkeys { get; } =
        new[]
        {
            new GlobalHotkeyDefinition(
                9001,
                GlobalHotkeyAction.ShowWindow,
                ParseOrThrow("Ctrl+Shift+Space"),
                "显示窗口"),
            new GlobalHotkeyDefinition(
                9004,
                GlobalHotkeyAction.QuickQuery,
                ParseOrThrow("Ctrl+Shift+Q"),
                "查询剪贴板")
        };

    public HotkeyService(MainViewModel mainViewModel, SettingsService settingsService)
    {
        _mainViewModel = mainViewModel;
        _settingsService = settingsService;
    }

    public string? Register(Window window)
    {
        if (_hwndSource != null) return ApplyFromSettings();

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return "无法获取窗口句柄，未能注册全局快捷键";

        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        return ApplyFromSettings();
    }

    /// <summary>
    /// 根据当前设置（重新）注册所有全局快捷键。设置变化后调用即可热更新，无需重启。
    /// 返回被占用/无法注册的快捷键提示，全部成功时返回 null。
    /// </summary>
    public string? ApplyFromSettings()
    {
        if (_hwndSource == null) return null;
        var handle = _hwndSource.Handle;
        if (handle == IntPtr.Zero) return null;

        foreach (var hotkeyId in _registeredHotkeyIds)
            Win32.UnregisterHotKey(handle, hotkeyId);
        _registeredHotkeyIds.Clear();
        _actionsById.Clear();

        var settings = _settingsService.Load();
        var failedHotkeys = new List<string>();

        foreach (var definition in GlobalHotkeys)
        {
            var combo = ResolveCombo(settings, definition);
            _actionsById[definition.Id] = definition.Action;

            if (Win32.RegisterHotKey(handle, definition.Id, combo.Modifiers, combo.VirtualKey))
                _registeredHotkeyIds.Add(definition.Id);
            else
                failedHotkeys.Add($"{definition.Label}（{combo.DisplayText}）");
        }

        return failedHotkeys.Count == 0
            ? null
            : $"以下快捷键无法注册，可能被其他程序占用：{string.Join("、", failedHotkeys)}";
    }

    private static HotkeyCombo ResolveCombo(Models.AppSettings settings, GlobalHotkeyDefinition definition)
    {
        var raw = definition.Action switch
        {
            GlobalHotkeyAction.ShowWindow => settings.ShowWindowHotkey,
            GlobalHotkeyAction.QuickQuery => settings.QuickQueryHotkey,
            _ => null
        };

        return HotkeyCombo.TryParse(raw, out var combo) ? combo : definition.DefaultCombo;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (!_actionsById.TryGetValue(hotkeyId, out var action))
                    return;

                switch (action)
                {
                    case GlobalHotkeyAction.ShowWindow:
                        _mainViewModel.ShowWindowCommand.Execute(null);
                        break;
                    case GlobalHotkeyAction.QuickQuery:
                        _mainViewModel.QuickQueryCommand.Execute(null);
                        break;
                }
            });

            handled = true;
        }

        return IntPtr.Zero;
    }

    public void Unregister()
    {
        if (_hwndSource == null) return;

        var handle = _hwndSource.Handle;
        if (handle != IntPtr.Zero)
        {
            foreach (var hotkeyId in _registeredHotkeyIds)
                Win32.UnregisterHotKey(handle, hotkeyId);
        }

        _hwndSource.RemoveHook(WndProc);
        _hwndSource = null;
        _registeredHotkeyIds.Clear();
        _actionsById.Clear();
    }

    public void Dispose()
    {
        Unregister();
    }

    private static HotkeyCombo ParseOrThrow(string combo) =>
        HotkeyCombo.TryParse(combo, out var parsed)
            ? parsed
            : throw new ArgumentException($"内置默认快捷键无效：{combo}");
}

internal enum GlobalHotkeyAction
{
    ShowWindow,
    QuickQuery
}

internal sealed record GlobalHotkeyDefinition(
    int Id,
    GlobalHotkeyAction Action,
    HotkeyCombo DefaultCombo,
    string Label);
