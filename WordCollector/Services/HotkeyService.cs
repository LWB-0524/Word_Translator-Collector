using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using WordCollector.NativeMethods;
using WordCollector.ViewModels;

namespace WordCollector.Services;

public class HotkeyService : IDisposable
{
    private HwndSource? _hwndSource;
    private readonly MainViewModel _mainViewModel;
    private readonly HashSet<int> _registeredHotkeyIds = new();

    internal static IReadOnlyList<GlobalHotkeyDefinition> GlobalHotkeys { get; } =
        new[]
        {
            new GlobalHotkeyDefinition(
                9001,
                Win32.MOD_CONTROL | Win32.MOD_SHIFT,
                Win32.VK_SPACE,
                "Ctrl+Shift+Space",
                GlobalHotkeyAction.ShowWindow),
            new GlobalHotkeyDefinition(
                9004,
                Win32.MOD_CONTROL | Win32.MOD_SHIFT,
                Win32.VK_Q,
                "Ctrl+Shift+Q",
                GlobalHotkeyAction.QuickQuery)
        };

    public HotkeyService(MainViewModel mainViewModel)
    {
        _mainViewModel = mainViewModel;
    }

    public string? Register(Window window)
    {
        if (_hwndSource != null) return null;

        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return "无法获取窗口句柄，未能注册全局快捷键";

        _hwndSource = HwndSource.FromHwnd(handle);
        _hwndSource?.AddHook(WndProc);

        var failedHotkeys = new List<string>();
        foreach (var definition in GlobalHotkeys)
        {
            if (Win32.RegisterHotKey(
                    handle, definition.Id, definition.Modifiers, (uint)definition.VirtualKey))
            {
                _registeredHotkeyIds.Add(definition.Id);
            }
            else
            {
                failedHotkeys.Add(definition.DisplayText);
            }
        }

        return failedHotkeys.Count == 0
            ? null
            : $"以下快捷键被其他程序占用：{string.Join("、", failedHotkeys)}";
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == Win32.WM_HOTKEY)
        {
            int hotkeyId = wParam.ToInt32();

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var definition = GlobalHotkeys.FirstOrDefault(item => item.Id == hotkeyId);
                switch (definition?.Action)
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
    }

    public void Dispose()
    {
        Unregister();
    }
}

internal enum GlobalHotkeyAction
{
    ShowWindow,
    QuickQuery
}

internal sealed record GlobalHotkeyDefinition(
    int Id,
    uint Modifiers,
    int VirtualKey,
    string DisplayText,
    GlobalHotkeyAction Action);
