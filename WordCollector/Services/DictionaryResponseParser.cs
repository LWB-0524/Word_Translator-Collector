using System.Net;
using System.Text.Json;

namespace WordCollector.Services;

public sealed record DictionaryEntry(
    string Phonetic,
    string PartOfSpeech,
    string Definition,
    string Example);

public static class DictionaryResponseParser
{
    public static bool TryParseDictionary(string json, out DictionaryEntry entry)
    {
        entry = new DictionaryEntry(string.Empty, string.Empty, string.Empty, string.Empty);

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array ||
                document.RootElement.GetArrayLength() == 0)
            {
                return false;
            }

            var root = document.RootElement[0];
            var phonetic = ReadString(root, "phonetic");
            if (string.IsNullOrWhiteSpace(phonetic) &&
                root.TryGetProperty("phonetics", out var phonetics) &&
                phonetics.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in phonetics.EnumerateArray())
                {
                    phonetic = ReadString(item, "text");
                    if (!string.IsNullOrWhiteSpace(phonetic)) break;
                }
            }

            if (!root.TryGetProperty("meanings", out var meanings) ||
                meanings.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var meaning in meanings.EnumerateArray())
            {
                if (!meaning.TryGetProperty("definitions", out var definitions) ||
                    definitions.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var definitionItem in definitions.EnumerateArray())
                {
                    var definition = ReadString(definitionItem, "definition");
                    if (string.IsNullOrWhiteSpace(definition)) continue;

                    var partOfSpeech = ReadString(meaning, "partOfSpeech");
                    var example = ReadString(definitionItem, "example");
                    entry = new DictionaryEntry(
                        NormalizePhonetic(phonetic),
                        partOfSpeech,
                        definition,
                        example);
                    return true;
                }
            }

            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public static bool TryParseTranslation(string json, out string translation)
    {
        translation = string.Empty;

        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (!root.TryGetProperty("responseData", out var responseData) ||
                responseData.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            translation = WebUtility.HtmlDecode(ReadString(responseData, "translatedText")).Trim();
            return !string.IsNullOrWhiteSpace(translation);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) &&
               value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim() ?? string.Empty
            : string.Empty;
    }

    private static string NormalizePhonetic(string phonetic)
    {
        var trimmed = phonetic.Trim();
        if (string.IsNullOrEmpty(trimmed)) return string.Empty;
        return trimmed.StartsWith('/') && trimmed.EndsWith('/')
            ? trimmed
            : $"/{trimmed.Trim('/')}/";
    }
}
