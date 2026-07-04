using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using WordCollector.Helpers;
using WordCollector.ViewModels;

namespace WordCollector.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
        viewModel.SetOwnerWindow(this);

        var settings = viewModel.SettingsService.Load();
        Left = settings.WindowLeft;
        Top = settings.WindowTop;
        var windowSize = WindowSizePolicy.Resolve(settings.WindowWidth, settings.WindowHeight);
        Width = windowSize.Width;
        Height = windowSize.Height;

        viewModel.RefreshSettings();
        UpdatePinButton();
        viewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(MainViewModel.IsSpeaking))
                SpeakButtonText.Text = viewModel.IsSpeaking ? "\uE71A" : "\uE767";
            if (args.PropertyName == nameof(MainViewModel.IsAlwaysOnTop))
                UpdatePinButton();
        };

        Loaded += (_, _) =>
        {
            InputTextBox.Focus();
            viewModel.RefreshSettings();
        };

        Closing += (_, _) =>
        {
            var latestSettings = viewModel.SettingsService.Load();
            latestSettings.WindowLeft = Left;
            latestSettings.WindowTop = Top;
            latestSettings.WindowWidth = Width;
            latestSettings.WindowHeight = Height;
            viewModel.SettingsService.Save(latestSettings);
        };

    }

    private void UpdatePinButton()
    {
        var isPinned = _viewModel.IsAlwaysOnTop;
        PinButton.Content = isPinned ? "● 置顶" : "置顶";
        PinButton.FontWeight = isPinned ? FontWeights.Bold : FontWeights.SemiBold;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 1 || IsInsideButton(e.OriginalSource as DependencyObject))
            return;
        DragMove();
    }

    private static bool IsInsideButton(DependencyObject? source)
    {
        for (var current = source; current != null; current = VisualTreeHelper.GetParent(current))
        {
            if (current is ButtonBase)
                return true;
        }
        return false;
    }

    private void InputTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var command = Keyboard.Modifiers == ModifierKeys.Control
                ? _viewModel.QueryAndActionCommand
                : _viewModel.QueryCommand;
            command.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.L && Keyboard.Modifiers == ModifierKeys.Control)
        {
            _viewModel.ClearCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
            _viewModel.HideWindowCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void QueryButton_Click(object sender, RoutedEventArgs e) => _viewModel.QueryCommand.Execute(null);
    private void PinButton_Click(object sender, RoutedEventArgs e) => _viewModel.ToggleTopmostCommand.Execute(null);
    private void ReviewButton_Click(object sender, RoutedEventArgs e) => _viewModel.ShowReviewCommand.Execute(null);
    private void SettingsButton_Click(object sender, RoutedEventArgs e) => _viewModel.ShowSettingsCommand.Execute(null);
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void ExitButton_Click(object sender, RoutedEventArgs e) => App.RequestExit();
}
