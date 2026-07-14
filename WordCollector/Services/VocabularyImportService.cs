using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using WordCollector.Helpers;
using WordCollector.Models;

namespace WordCollector.Services;

/// <summary>内置/导入词表中的一条词条（字段与 AI 结果保持一致的 snake_case）。</summary>
public sealed class VocabularySeedEntry
{
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("item_type")] public string ItemType { get; set; } = "word";
    [JsonPropertyName("phonetic")] public string Phonetic { get; set; } = string.Empty;
    [JsonPropertyName("meaning_zh")] public string MeaningZh { get; set; } = string.Empty;
    [JsonPropertyName("brief_explanation")] public string BriefExplanation { get; set; } = string.Empty;
    [JsonPropertyName("detailed_explanation")] public string DetailedExplanation { get; set; } = string.Empty;
    [JsonPropertyName("example_en")] public string ExampleEn { get; set; } = string.Empty;
    [JsonPropertyName("example_zh")] public string ExampleZh { get; set; } = string.Empty;
}

public readonly record struct VocabularyImportResult(int Added, int Skipped);

/// <summary>
/// 把词表批量导入词库：按 normalized_text 去重，仅插入尚不存在的词条。
/// 解析与导入均为纯逻辑，便于单测；<see cref="ImportBuiltIn"/> 读取内置的土木工程词表资源。
/// </summary>
public static class VocabularyImportService
{
    private const string ResourceSuffix = "civil-engineering-vocabulary.json";

    private static readonly JsonSerializerOptions Options =
        new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<VocabularySeedEntry> ParseSeed(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return Array.Empty<VocabularySeedEntry>();

        try
        {
            return JsonSerializer.Deserialize<List<VocabularySeedEntry>>(json, Options)
                   ?? new List<VocabularySeedEntry>();
        }
        catch (JsonException)
        {
            return Array.Empty<VocabularySeedEntry>();
        }
    }

    public static VocabularyImportResult Import(
        DatabaseService database, IEnumerable<VocabularySeedEntry> entries)
    {
        int added = 0, skipped = 0;
        var today = DateTime.Now.ToString("yyyy-MM-dd");
        var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        foreach (var entry in entries)
        {
            var text = entry.Text?.Trim();
            if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(entry.MeaningZh))
            {
                skipped++;
                continue;
            }

            var normalized = TextNormalizer.Normalize(text);
            if (database.FindByNormalizedText(normalized) != null)
            {
                skipped++;
                continue;
            }

            database.Insert(new VocabularyItem
            {
                Text = text,
                NormalizedText = normalized,
                ItemType = string.IsNullOrWhiteSpace(entry.ItemType)
                    ? "word"
                    : entry.ItemType.Trim().ToLowerInvariant(),
                Phonetic = entry.Phonetic ?? string.Empty,
                MeaningZh = entry.MeaningZh.Trim(),
                BriefExplanation = entry.BriefExplanation ?? string.Empty,
                DetailedExplanation = entry.DetailedExplanation ?? string.Empty,
                ExampleEn = entry.ExampleEn ?? string.Empty,
                ExampleZh = entry.ExampleZh ?? string.Empty,
                DateAdded = today,
                CreatedAt = now,
                UpdatedAt = now,
                LookupCount = 1,
                Familiarity = 0
                // NextReviewDate 留空 → 立即进入间隔重复的待复习队列。
            });
            added++;
        }

        return new VocabularyImportResult(added, skipped);
    }

    public static VocabularyImportResult ImportBuiltIn(DatabaseService database) =>
        Import(database, ParseSeed(LoadBuiltInJson()));

    /// <summary>读取内置的土木工程词表 JSON（嵌入资源）。找不到时返回空串。</summary>
    public static string LoadBuiltInJson()
    {
        var assembly = typeof(VocabularyImportService).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(ResourceSuffix, StringComparison.OrdinalIgnoreCase));
        if (resourceName == null)
            return string.Empty;

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            return string.Empty;

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
