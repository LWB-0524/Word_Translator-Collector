using System.Windows;
using WordCollector.Helpers;
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
