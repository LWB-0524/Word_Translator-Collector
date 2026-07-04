using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WordCollector.Models;

[Table("vocabulary_items")]
public class VocabularyItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public string Text { get; set; } = string.Empty;

    public string? NormalizedText { get; set; }

    public string? ItemType { get; set; }

    public string? Phonetic { get; set; }

    [Required]
    public string MeaningZh { get; set; } = string.Empty;

    public string? BriefExplanation { get; set; }

    public string? DetailedExplanation { get; set; }

    public string? ExampleEn { get; set; }

    public string? ExampleZh { get; set; }

    public string? KeyExpressionsJson { get; set; }

    public string? RawAiResponse { get; set; }

    [Required]
    public string DateAdded { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");

    [Required]
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

    public string? UpdatedAt { get; set; }

    public int LookupCount { get; set; } = 1;

    /// <summary>
    /// 0 = 陌生, 1 = 一般, 2 = 掌握
    /// </summary>
    public int Familiarity { get; set; } = 0;

    public int SpokenCount { get; set; } = 0;

    public string? LastSpokenAt { get; set; }
}
