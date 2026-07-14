using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WordCollector.Helpers;
using WordCollector.NativeMethods;
using WordCollector.ViewModels;

namespace WordCollector.Views;

public partial class SettingsWindow : Window
{
    private readonly SettingsViewModel _viewModel;

    public SettingsWindow(MainViewModel mainViewModel)
    {
        InitializeComponent();

        _viewModel = new SettingsViewModel(
            mainViewModel.SettingsService,
            mainViewModel.TtsService,
            mainViewModel,
            mainViewModel.ThemeService);
        DataContext = _viewModel;

        // Set API key from settings (not bound directly for security)
        var settings = mainViewModel.SettingsService.Load();
        ApiKeyBox.Password = settings.AiApiKey;
        WindowThemeHelper.ApplyNativeTitleBar(this, settings.ThemeMode);
    }

    /// <summary>
    /// Sync the PasswordBox value into the ViewModel before any operation
    /// </summary>
    private void SyncPassword()
    {
        _viewModel.AiApiKey = ApiKeyBox.Password;
    }

    private async void TestConnection_Click(object sender, RoutedEventArgs e)
    {
        SyncPassword();
        await _viewModel.TestConnectionAsync();
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        SyncPassword();
        await _viewModel.SaveAsync();
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ResetDefaults();
        ApiKeyBox.Password = string.Empty;
    }

    private void ImportCivilEngineering_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.ImportCivilEngineeringCommand.Execute(null);
    }

    /// <summary>
    /// 快捷键录制：吞掉键盘输入，把“修饰键 + 主键”翻译成组合并写回 ViewModel。
    /// Esc 取消本次录制；Backspace/Delete 恢复该快捷键的默认值。
    /// </summary>
    private void HotkeyBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox box) return;
        e.Handled = true;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        var target = box.Tag as string;

        if (key == Key.Escape)
        {
            SetHotkeyHint(null);
            Keyboard.ClearFocus();
            return;
        }

        if (key is Key.Back or Key.Delete)
        {
            ApplyHotkey(target, target == "QuickQuery" ? "Ctrl+Shift+Q" : "Ctrl+Shift+Space");
            SetHotkeyHint(null);
            return;
        }

        // 仅按下修饰键时，等待主键。
        if (IsModifierKey(key))
            return;

        uint modifiers = 0;
        if ((Keyboard.Modifiers & ModifierKeys.Control) != 0) modifiers |= Win32.MOD_CONTROL;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) != 0) modifiers |= Win32.MOD_ALT;
        if ((Keyboard.Modifiers & ModifierKeys.Shift) != 0) modifiers |= Win32.MOD_SHIFT;
        if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin)) modifiers |= Win32.MOD_WIN;

        if (modifiers == 0)
        {
            SetHotkeyHint("请至少包含一个修饰键（Ctrl / Alt / Shift / Win）。");
            return;
        }

        var virtualKey = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (!HotkeyCombo.TryCreate(modifiers, virtualKey, out var combo))
        {
            SetHotkeyHint("不支持该按键，请换一个主键（字母、数字、F1–F24、方向键等）。");
            return;
        }

        ApplyHotkey(target, combo.ToStorageString());
        SetHotkeyHint(null);
    }

    private void ApplyHotkey(string? target, string storage)
    {
        if (target == "QuickQuery")
            _viewModel.QuickQueryHotkey = storage;
        else
            _viewModel.ShowWindowHotkey = storage;
    }

    private void SetHotkeyHint(string? message)
    {
        if (string.IsNullOrEmpty(message))
        {
            HotkeyHint.Text = string.Empty;
            HotkeyHint.Visibility = Visibility.Collapsed;
        }
        else
        {
            HotkeyHint.Text = message;
            HotkeyHint.Visibility = Visibility.Visible;
        }
    }

    private static bool IsModifierKey(Key key) =>
        key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin
            or Key.System;

    private void SlowRate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TtsRate = -5;
    }

    private void NormalRate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TtsRate = 0;
    }

    private void FastRate_Click(object sender, RoutedEventArgs e)
    {
        _viewModel.TtsRate = 5;
    }
}
