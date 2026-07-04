using System.Text.Json.Serialization;

namespace WordCollector.Models;

public class KeyExpression
{
    [JsonPropertyName("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonPropertyName("meaning_zh")]
    public string MeaningZh { get; set; } = string.Empty;
}
