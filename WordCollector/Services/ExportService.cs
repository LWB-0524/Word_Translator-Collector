using System.Text;
using WordCollector.Models;

namespace WordCollector.Services;

public class ExportService
{
    public string GenerateMarkdown(List<VocabularyItem> items, string date)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"# WordCollector Daily Review - {date}");
        sb.AppendLine();

        if (items.Count == 0)
        {
            sb.AppendLine("_No entries for this date._");
            return sb.ToString();
        }

        var words = items.Where(i => i.ItemType == "word").ToList();
        var phrases = items.Where(i => i.ItemType == "phrase").ToList();
        var sentences = items.Where(i => i.ItemType == "sentence" || i.ItemType == null).ToList();

        ExportCategory(sb, "Words", words);
        ExportCategory(sb, "Phrases", phrases);
        ExportCategory(sb, "Sentences", sentences);

        return sb.ToString();
    }

    private void ExportCategory(StringBuilder sb, string categoryName, List<VocabularyItem> items)
    {
        if (items.Count == 0) return;

        sb.AppendLine($"## {categoryName}");
        sb.AppendLine();

        foreach (var item in items)
        {
            sb.AppendLine($"### {item.Text}");
            sb.AppendLine($"中文释义：{item.MeaningZh}");
            if (!string.IsNullOrWhiteSpace(item.BriefExplanation))
                sb.AppendLine($"用法说明：{item.BriefExplanation}");
            sb.AppendLine($"朗读次数：{item.SpokenCount}");

            if (!string.IsNullOrWhiteSpace(item.ExampleEn))
                sb.AppendLine($"例句：{item.ExampleEn}");

            if (!string.IsNullOrWhiteSpace(item.ExampleZh))
                sb.AppendLine($"例句翻译：{item.ExampleZh}");

            if (!string.IsNullOrWhiteSpace(item.KeyExpressionsJson))
            {
                try
                {
                    var expressions = System.Text.Json.JsonSerializer.Deserialize<List<KeyExpression>>(item.KeyExpressionsJson);
                    if (expressions != null && expressions.Count > 0)
                    {
                        sb.AppendLine("关键表达：");
                        foreach (var ke in expressions)
                        {
                            sb.AppendLine($"- {ke.Expression}：{ke.MeaningZh}");
                        }
                    }
                }
                catch
                {
                    // Ignore parse errors in export
                }
            }

            sb.AppendLine();
        }
    }

    public string GenerateCsv(List<VocabularyItem> items)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("date_added,text,item_type,meaning_zh,brief_explanation,example_en,example_zh,lookup_count,spoken_count,familiarity");

        foreach (var item in items)
        {
            var fields = new[]
            {
                CsvEscape(item.DateAdded),
                CsvEscape(item.Text),
                CsvEscape(item.ItemType ?? ""),
                CsvEscape(item.MeaningZh),
                CsvEscape(item.BriefExplanation ?? ""),
                CsvEscape(item.ExampleEn ?? ""),
                CsvEscape(item.ExampleZh ?? ""),
                item.LookupCount.ToString(),
                item.SpokenCount.ToString(),
                item.Familiarity.ToString()
            };

            sb.AppendLine(string.Join(",", fields));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }
}
