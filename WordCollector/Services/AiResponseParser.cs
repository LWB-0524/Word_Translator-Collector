using System.Text.Json;
using WordCollector.Helpers;
using WordCollector.Models;

namespace WordCollector.Services;

public static class AiResponseParser
{
    private static readonly HashSet<string> AllowedItemTypes =
        new(StringComparer.OrdinalIgnoreCase) { "word", "phrase", "sentence" };

    public static bool TryExtractAssistantContent(
        string responseBody,
        out string content,
        out string error)
    {
        content = string.Empty;
        error = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("choices", out var choices) ||
                choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0 ||
                !choices[0].TryGetProperty("message", out var message) ||
                !message.TryGetProperty("content", out var contentElement) ||
                contentElement.ValueKind != JsonValueKind.String)
            {
                error = "AI 返回格式无效";
                return false;
            }

            content = contentElement.GetString()?.Trim() ?? string.Empty;
            if (content.Length == 0)
            {
                error = "AI 返回内容为空";
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = "AI 返回格式无效";
            return false;
        }
    }

    public static bool TryParseExplanation(
        string? content,
        out AiExplanationResult? result,
        out string rawContent)
    {
        rawContent = StripMarkdownFence(content);
        result = JsonHelper.SafeDeserialize<AiExplanationResult>(rawContent);
        var itemType = result?.ItemType?.Trim();

        if (result == null ||
            !AllowedItemTypes.Contains(itemType ?? string.Empty) ||
            string.IsNullOrWhiteSpace(result.MeaningZh))
        {
            result = null;
            return false;
        }

        result.ItemType = itemType!.ToLowerInvariant();
        result.MeaningZh = result.MeaningZh.Trim();
        result.Phonetic ??= string.Empty;
        result.BriefExplanation ??= string.Empty;
        result.DetailedExplanation ??= string.Empty;
        result.ExampleEn ??= string.Empty;
        result.ExampleZh ??= string.Empty;
        result.KeyExpressions ??= new List<KeyExpression>();
        return true;
    }

    private static string StripMarkdownFence(string? content)
    {
        var trimmed = content?.Trim() ?? string.Empty;
        if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            return trimmed;

        var firstLineEnd = trimmed.IndexOf('\n');
        var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
        if (firstLineEnd < 0 || closingFence <= firstLineEnd)
            return trimmed;

        return trimmed[(firstLineEnd + 1)..closingFence].Trim();
    }
}
