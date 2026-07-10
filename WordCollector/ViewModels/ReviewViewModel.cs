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
    private EventHandler? _speakStartedHandler;
    private EventHandler? _speakCompletedHandler;

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
        StartReviewCommand = new RelayCommand(StartReview);
        ExitReviewCommand = new RelayCommand(ExitReview);
        GradeReviewCommand = new RelayCommand<string>(GradeReview);

        SelectedDate = DateTime.Now.ToString("yyyy-MM-dd");
    }

    private ObservableCollection<VocabularyItem> _entries = new();
    public ObservableCollection<VocabularyItem> Entries { get => _entries; set => SetProperty(ref _entries, value); }

    private ObservableCollection<string> _dates = new();
    public ObservableCollection<string> Dates { get => _dates; set => SetProperty(ref _dates, value); }

    public IReadOnlyList<string> ItemTypeOptions => ReviewOptions.ItemTypes;
    public IReadOnlyList<string> SortModeOptions => ReviewOptions.SortModes;
    public IReadOnlyList<string> FamiliarityOptions => ReviewOptions.Familiarities;

    private string _selectedDate = string.Empty;
    public string SelectedDate { get => _selectedDate; set => SetProperty(ref _selectedDate, value); }

    private string _searchText = string.Empty;
    public string SearchText { get => _searchText; set => SetProperty(ref _searchText, value); }

    private string _selectedType = "全部";
    public string SelectedType { get => _selectedType; set => SetProperty(ref _selectedType, value); }

    private string _sortMode = "时间最新";
    public string SortMode { get => _sortMode; set => SetProperty(ref _sortMode, value); }

    private string _selectedFamiliarity = "全部";
    public string SelectedFamiliarity
    {
        get => _selectedFamiliarity;
        set
        {
            if (SetProperty(ref _selectedFamiliarity, value))
                Refresh();
        }
    }

    private bool _searchAllDates;
    public bool SearchAllDates
    {
        get => _searchAllDates;
        set
        {
            if (SetProperty(ref _searchAllDates, value))
                Refresh();
        }
    }

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

    private bool _isReviewMode;
    public bool IsReviewMode
    {
        get => _isReviewMode;
        private set
        {
            if (SetProperty(ref _isReviewMode, value))
                OnPropertyChanged(nameof(IsBrowseMode));
        }
    }

    public bool IsBrowseMode => !IsReviewMode;

    private VocabularyStatistics _statistics = VocabularyStatistics.Empty;
    public VocabularyStatistics Statistics { get => _statistics; private set => SetProperty(ref _statistics, value); }

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
    public ICommand StartReviewCommand { get; }
    public ICommand ExitReviewCommand { get; }
    public ICommand GradeReviewCommand { get; }

    public void Load()
    {
        if (_speakStartedHandler == null)
        {
            _speakStartedHandler = (_, _) => IsSpeaking = true;
            _speakCompletedHandler = (_, _) => IsSpeaking = false;
            _ttsService.SpeakStarted += _speakStartedHandler;
            _ttsService.SpeakCompleted += _speakCompletedHandler;
        }

        Dates.Clear();
        foreach (var date in _databaseService.GetDistinctDates())
            Dates.Add(date);
        LoadToday();
        RefreshStatistics();
    }

    private void RefreshStatistics()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        Statistics = _databaseService.GetStatistics(today);
    }

    /// <summary>
    /// 窗口关闭时调用，退订 TTS 事件，避免 app 级服务长期持有本实例。
    /// </summary>
    public void Unload()
    {
        if (_speakStartedHandler != null)
        {
            _ttsService.SpeakStarted -= _speakStartedHandler;
            _speakStartedHandler = null;
        }
        if (_speakCompletedHandler != null)
        {
            _ttsService.SpeakCompleted -= _speakCompletedHandler;
            _speakCompletedHandler = null;
        }
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
            var (dateFrom, dateTo) = SearchAllDates
                ? (null, (string?)null)
                : (SelectedDate, SelectedDate);
            _allEntries = _databaseService.Search(
                SearchText, dateFrom, dateTo, typeFilter,
                ReviewOptions.ToFamiliarityLevel(SelectedFamiliarity));
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
        if (string.IsNullOrWhiteSpace(SearchText) && !SearchAllDates)
            ApplyFilters();
        else
            Search();
    }

    private void ApplyFilters()
    {
        Entries = new ObservableCollection<VocabularyItem>(
            ReviewOptions.Apply(_allEntries, SelectedType, SortMode, SelectedFamiliarity));
        StatusText = $"共 {Entries.Count} 条记录";
    }

    private void Refresh()
    {
        // 有搜索词或勾选“全部日期”时走 SQL 查询（可跨日期）；否则按当前日期浏览。
        if (string.IsNullOrWhiteSpace(SearchText) && !SearchAllDates)
            LoadSelectedDate();
        else
            Search();
    }

    private void StartReview()
    {
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        _allEntries = _databaseService.GetDueForReview(today);
        Entries = new ObservableCollection<VocabularyItem>(_allEntries);
        IsReviewMode = true;

        if (Entries.Count == 0)
        {
            ClearSelection();
            StatusText = "太棒了，今天没有需要复习的内容 🎉";
            return;
        }

        SelectEntry(Entries[0]);
        StatusText = $"复习模式 · 待复习 {Entries.Count} 个";
    }

    private void ExitReview()
    {
        IsReviewMode = false;
        ClearSelection();
        Refresh();
        RefreshStatistics();
    }

    private void GradeReview(string? gradeText)
    {
        var item = SelectedEntry;
        if (item == null) return;

        var grade = gradeText switch
        {
            "forgot" => ReviewGrade.Forgot,
            "hard" => ReviewGrade.Hard,
            _ => ReviewGrade.Good
        };

        try
        {
            var next = SpacedRepetitionScheduler.Advance(
                item.ReviewRepetitions, item.ReviewIntervalDays, item.ReviewEaseFactor,
                grade, DateTime.Now);
            item.ReviewRepetitions = next.Repetitions;
            item.ReviewIntervalDays = next.IntervalDays;
            item.ReviewEaseFactor = next.EaseFactor;
            item.NextReviewDate = next.NextReviewDate;
            item.LastReviewedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            item.Familiarity = grade switch
            {
                ReviewGrade.Forgot => 0,
                ReviewGrade.Hard => 1,
                _ => 2
            };
            _databaseService.UpdateReviewState(item);
        }
        catch (Exception ex)
        {
            StatusText = $"评分失败：{ex.Message}";
            return;
        }

        var index = Entries.IndexOf(item);
        Entries.Remove(item);
        _allEntries.Remove(item);
        RefreshStatistics();

        if (Entries.Count == 0)
        {
            ClearSelection();
            StatusText = "复习完成，全部过了一遍 🎉";
            return;
        }

        SelectEntry(Entries[Math.Min(index, Entries.Count - 1)]);
        StatusText = $"已评分 · 剩余 {Entries.Count} 个";
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
            RefreshStatistics();
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
            RefreshStatistics();
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
            // CSV 带 BOM，Excel 才能正确识别 UTF-8 中文。
            var encoding = format == "csv"
                ? new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: true)
                : new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            File.WriteAllText(dialog.FileName, content, encoding);
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
