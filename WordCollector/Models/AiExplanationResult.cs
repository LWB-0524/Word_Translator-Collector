using System.Text.Json.Serialization;

namespace WordCollector.Models;

public class AiExplanationResult
{
    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = string.Empty;

    [JsonPropertyName("phonetic")]
    public string Phonetic { get; set; } = string.Empty;

    [JsonPropertyName("meaning_zh")]
    public string MeaningZh { get; set; } = string.Empty;

    [JsonPropertyName("brief_explanation")]
    public string BriefExplanation { get; set; } = string.Empty;

    [JsonPropertyName("detailed_explanation")]
    public string DetailedExplanation { get; set; } = string.Empty;

    [JsonPropertyName("example_en")]
    public string ExampleEn { get; set; } = string.Empty;

    [JsonPropertyName("example_zh")]
    public string ExampleZh { get; set; } = string.Empty;

    [JsonPropertyName("key_expressions")]
    public List<KeyExpression> KeyExpressions { get; set; } = new();
}
