using System.Windows;
using System.Windows.Controls;
using WordCollector.Models;
using WordCollector.Helpers;
using WordCollector.Services;
using WordCollector.ViewModels;

namespace WordCollector.Views;

public partial class ReviewWindow : Window
{
    private readonly ReviewViewModel _viewModel;

    public ReviewWindow(DatabaseService databaseService, TextToSpeechService ttsService,
        MainViewModel mainViewModel)
    {
        InitializeComponent();

        _viewModel = new ReviewViewModel(databaseService, ttsService);
        DataContext = _viewModel;
        WindowThemeHelper.ApplyNativeTitleBar(this, mainViewModel.ThemeMode);

        Loaded += (s, e) => _viewModel.Load();
        Closed += (s, e) => _viewModel.Unload();
    }

    private void EntryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (EntryListBox.SelectedItem is VocabularyItem item)
        {
            _viewModel.SelectEntry(item);
        }
    }

    private void MarkNew_Click(object sender, RoutedEventArgs e)
    {
        MarkFamiliarity(0);
    }

    private void MarkLearning_Click(object sender, RoutedEventArgs e)
    {
        MarkFamiliarity(1);
    }

    private void MarkMastered_Click(object sender, RoutedEventArgs e)
    {
        MarkFamiliarity(2);
    }

    private void MarkFamiliarity(int level)
    {
        if (_viewModel.SelectedEntry != null)
        {
            _viewModel.MarkFamiliarityCommand.Execute((_viewModel.SelectedEntry, level));
        }
    }

    private void SortMode_Changed(object sender, SelectionChangedEventArgs e)
    {
        // Trigger reload with new sort order
        if (_viewModel != null && IsLoaded)
        {
            _viewModel.RefreshCommand.Execute(null);
        }
    }

    private void Date_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && IsLoaded)
            _viewModel.LoadDateCommand.Execute(null);
    }

    private void ItemType_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_viewModel != null && IsLoaded && sender is ComboBox comboBox)
            _viewModel.FilterByTypeCommand.Execute(comboBox.SelectedItem as string);
    }
}
