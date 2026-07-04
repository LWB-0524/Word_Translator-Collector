using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using WordCollector.Helpers;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.ViewModels;

public class ReviewViewModel : BaseViewModel
{
    private readonly DatabaseService _databaseService;
    private readonly TextToSpeechService _ttsService;
    private List<VocabularyItem> _allEntries = new();

    public ReviewViewModel(DatabaseService databaseService, TextToSpeechService ttsService)
    {
        _databaseService = databaseService;
        _ttsService = ttsService;

        LoadTodayCommand = new RelayCommand(LoadToday);
        LoadDateCommand = new RelayCommand(LoadSelectedDate);
        SearchCommand = new RelayCommand(Search);
        FilterByTypeCommand = new RelayCommand<string>(FilterByType);
        DeleteCommand = new RelayCommand<VocabularyItem>(Delete);
        SpeakCommand = new RelayCommand<VocabularyItem>(Speak);
        MarkFamiliarityCommand = new RelayCommand<(VocabularyItem, int)>(MarkFamiliarity);
        ExportMarkdownCommand = new RelayCommand(ExportMarkdown);
        ExportCsvCommand = new RelayCommand(ExportCsv);
        SaveEditCommand = new RelayCommand<VocabularyItem>(SaveEdit);
        RefreshCommand = new RelayCommand(Refresh);

        SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
    }

    private ObservableCollection<VocabularyItem> _entries = new();
    public ObservableCollection<VocabularyItem> Entries { get => _entries; set => SetProperty(ref _entries, value); }

    private ObservableCollection<string> _dates = new();
    public ObservableCollection<string> Dates { get => _dates; set => SetProperty(ref _dates, value); }

    public IReadOnlyList<string> ItemTypeOptions => ReviewOptions.ItemTypes;
    public IReadOnlyList<string> SortModeOptions => ReviewOptions.SortModes;

    private string _selectedDate = string.Empty;
    public string SelectedDate { get => _selectedDate; set => SetProperty(ref _selectedDate, value); }

    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

    private string _selectedType = "全部";
    public string SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }

    private string _sortMode = "时间最新";
    public string SortMode { get => _sortMode; set => SetProperty(ref _sortMode, value); }

    private VocabularyItem? _selectedEntry;
    public VocabularyItem? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (!SetProperty(ref _selectedEntry, value)) return;
            OnPropertyChanged(nameof(IsDetailVisible));
            OnPropertyChanged(nameof(DetailItem));
        }
    }

    private VocabularyItem? _detailItem;
    public VocabularyItem? DetailItem { get => _detailItem; set => SetProperty(ref _detailItem, value); }
    public bool IsDetailVisible => SelectedEntry != null;

    private bool _isLoading;
    public bool IsLoading { get => _isLoading; set => SetProperty(ref _isLoading, value); }

    private string _statusText = string.Empty;
    public string StatusText { get => _statusText; set => SetProperty(ref _statusText, value); }

    private bool _isSpeaking;
    public bool IsSpeaking { get => _isSpeaking; set => SetProperty(ref _isSpeaking, value); }

    public ICommand LoadTodayCommand { get; }
    public ICommand LoadDateCommand { get; }
    public ICommand SearchCommand { get; }
    public ICommand FilterByTypeCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand SpeakCommand { get; }
    public ICommand MarkFamiliarityCommand { get; }
    public ICommand ExportMarkdownCommand { get; }
    public ICommand ExportCsvCommand { get; }
    public ICommand SaveEditCommand { get; }
    public ICommand RefreshCommand { get; }

    public void Load()
    {
        _ttsService.SpeakStarted += (_, _) => IsSpeaking = true;
        _ttsService.SpeakCompleted += (_, _) => IsSpeaking = false;

        Dates.Clear();
        foreach (var date in _databaseService.GetDistinctDates())
            Dates.Add(date);
        LoadToday();
    }

    private void LoadToday()
    {
        SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
        LoadSelectedDate();
    }

    private void LoadSelectedDate()
    {
        if (string.IsNullOrWhiteSpace(SelectedDate)) return;
        IsLoading = true;
        try
        {
            _allEntries = _databaseService.GetByDate(SelectedDate);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusText = $"加载失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void Search()
    {
        IsLoading = true;
        try
        {
            var typeFilter = SelectedType switch
            {
                "单词" => "word",
                "词组" => "phrase",
                "句子" => "sentence",
                _ => null
            };
            _allEntries = _databaseService.Search(SearchText, SelectedDate, SelectedDate, typeFilter, null);
            ApplyFilters();
        }
        catch (Exception ex)
        {
            StatusText = $"搜索失败：{ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void FilterByType(string? type)
    {
        SelectedType = type ?? "全部";
        if (string.IsNullOrWhiteSpace(SearchText))
            ApplyFilters();
        else
            Search();
    }

    private void ApplyFilters()
    {
        Entries = new ObservableCollection<VocabularyItem>(
            ReviewOptions.Apply(_allEntries, SelectedType, SortMode));
        StatusText = $"共 {Entries.Count} 条记录";
    }

    private void Refresh()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
            LoadSelectedDate();
        else
            Search();
    }

    private void Delete(VocabularyItem? item)
    {
        if (item == null) return;
        var result = MessageBox.Show(
            $"确定要删除“{item.Text}”吗？",
            "确认删除",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (result != MessageBoxResult.Yes) return;

        try
        {
            _databaseService.Delete(item.Id);
            _allEntries.Remove(item);
            Entries.Remove(item);
            if (SelectedEntry?.Id == item.Id)
                ClearSelection();
            StatusText = "已删除";
        }
        catch (Exception ex)
        {
            StatusText = $"删除失败：{ex.Message}";
        }
    }

    private void Speak(VocabularyItem? item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.Text)) return;
        if (_ttsService.IsSpeaking)
        {
            _ttsService.Stop();
            return;
        }

        _ttsService.SpeakAsync(item.Text);
        try
        {
            _databaseService.UpdateSpokenCount(item.Id, item.SpokenCount + 1);
            item.SpokenCount++;
        }
        catch
        {
            // 计数不影响朗读。
        }
    }

    private void MarkFamiliarity((VocabularyItem, int) tuple)
    {
        var (item, level) = tuple;
        try
        {
            item.Familiarity = level;
            _databaseService.Update(item);
            OnPropertyChanged(nameof(Entries));
        }
        catch (Exception ex)
        {
            StatusText = $"更新失败：{ex.Message}";
        }
    }

    private void SaveEdit(VocabularyItem? item)
    {
        if (item == null) return;
        if (string.IsNullOrWhiteSpace(item.Text) || string.IsNullOrWhiteSpace(item.MeaningZh))
        {
            StatusText = "英文原文和中文释义不能为空";
            return;
        }

        try
        {
            item.Text = item.Text.Trim();
            item.NormalizedText = TextNormalizer.Normalize(item.Text);
            _databaseService.Update(item);
            StatusText = "已保存";
        }
        catch (Exception ex)
        {
            StatusText = $"保存失败：{ex.Message}";
        }
    }

    private void ExportMarkdown() => Export("Markdown|*.md", "md");
    private void ExportCsv() => Export("CSV|*.csv", "csv");

    private void Export(string filter, string format)
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = filter,
            DefaultExt = format,
            FileName = $"WordCollector_{SelectedDate}.{format}"
        };
        if (dialog.ShowDialog() != true) return;

        try
        {
            var exportService = new ExportService();
            var content = format == "md"
                ? exportService.GenerateMarkdown(_allEntries, SelectedDate)
                : exportService.GenerateCsv(_allEntries);
            File.WriteAllText(dialog.FileName, content);
            StatusText = $"导出成功：{dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusText = $"导出失败：{ex.Message}";
        }
    }

    public void SelectEntry(VocabularyItem entry)
    {
        SelectedEntry = entry;
        DetailItem = entry;
    }

    public void ClearSelection()
    {
        SelectedEntry = null;
        DetailItem = null;
    }
}
