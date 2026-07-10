using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace WordCollector.Models;

[Table("vocabulary_items")]
public class VocabularyItem : INotifyPropertyChanged
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    private string _text = string.Empty;

    [Required]
    public string Text { get => _text; set => SetField(ref _text, value); }

    public string? NormalizedText { get; set; }

    public string? ItemType { get; set; }

    public string? Phonetic { get; set; }

    private string _meaningZh = string.Empty;

    [Required]
    public string MeaningZh { get => _meaningZh; set => SetField(ref _meaningZh, value); }

    private string? _briefExplanation;
    public string? BriefExplanation { get => _briefExplanation; set => SetField(ref _briefExplanation, value); }

    public string? DetailedExplanation { get; set; }

    private string? _exampleEn;
    public string? ExampleEn { get => _exampleEn; set => SetField(ref _exampleEn, value); }

    private string? _exampleZh;
    public string? ExampleZh { get => _exampleZh; set => SetField(ref _exampleZh, value); }

    public string? KeyExpressionsJson { get; set; }

    public string? RawAiResponse { get; set; }

    [Required]
    public string DateAdded { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    [Required]
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public string? UpdatedAt { get; set; }

    private int _lookupCount = 1;
    public int LookupCount { get => _lookupCount; set => SetField(ref _lookupCount, value); }

    private int _familiarity;

    /// <summary>
    /// 0 = 陌生, 1 = 一般, 2 = 掌握
    /// </summary>
    public int Familiarity { get => _familiarity; set => SetField(ref _familiarity, value); }

    private int _spokenCount;
    public int SpokenCount { get => _spokenCount; set => SetField(ref _spokenCount, value); }

    public string? LastSpokenAt { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
