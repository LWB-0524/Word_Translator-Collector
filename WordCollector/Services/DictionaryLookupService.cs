using System.Net;
using System.Net.Http;
using System.Text.Json;
using WordCollector.Models;

namespace WordCollector.Services;

public sealed class DictionaryLookupService : IDictionaryLookupProvider, IDictionarySource, IDisposable
{
    private readonly HttpClient _httpClient;

    public DictionaryLookupService()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(5) })
    {
    }

    internal DictionaryLookupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "开放词典 (dictionaryapi.dev + MyMemory)";

    bool IDictionaryLookupProvider.CanLookup(string text) => CanLookup(text);
    bool IDictionarySource.CanLookup(string text) => CanLookup(text);

    public static bool CanLookup(string text) => CanLookupWord(text);

    private static bool CanLookupWord(string text)
    {
        var word = text.Trim();
        if (word.Length is 0 or > 64 || !IsAsciiLetter(word[0]) || !IsAsciiLetter(word[^1]))
            return false;

        return word.All(character =>
            IsAsciiLetter(character) || character is '-' or '\'');
    }

    public async Task<LookupResponse> QueryAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var word = text.Trim();
        if (!CanLookupWord(word))
            return Miss("词典仅处理单个英文单词");

        var escapedWord = Uri.EscapeDataString(word);
        var dictionaryUri = $"https://api.dictionaryapi.dev/api/v2/entries/en/{escapedWord}";
        var translationUri =
            $"https://api.mymemory.translated.net/get?q={escapedWord}&langpair=en%7Czh-CN";

        try
        {
            var dictionaryTask = _httpClient.GetAsync(dictionaryUri, cancellationToken);
            var translationTask = _httpClient.GetAsync(translationUri, cancellationToken);
            await Task.WhenAll(dictionaryTask, translationTask);

            using var dictionaryResponse = await dictionaryTask;
            using var translationResponse = await translationTask;

            if (dictionaryResponse.StatusCode == HttpStatusCode.NotFound)
                return Miss("词典中未找到该单词");
            if (!dictionaryResponse.IsSuccessStatusCode || !translationResponse.IsSuccessStatusCode)
                return Miss("词典服务暂时不可用");

            var dictionaryJson = await dictionaryResponse.Content.ReadAsStringAsync(cancellationToken);
            var translationJson = await translationResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!DictionaryResponseParser.TryParseDictionary(dictionaryJson, out var entry) ||
                !DictionaryResponseParser.TryParseTranslation(translationJson, out var translation) ||
                string.Equals(translation, word, StringComparison.OrdinalIgnoreCase))
            {
                return Miss("词典结果不完整");
            }

            var explanation = string.IsNullOrWhiteSpace(entry.PartOfSpeech)
                ? entry.Definition
                : $"{entry.PartOfSpeech} · {entry.Definition}";
            var result = new AiExplanationResult
            {
                ItemType = "word",
                Phonetic = entry.Phonetic,
                MeaningZh = translation,
                BriefExplanation = explanation,
                DetailedExplanation = entry.Definition,
                ExampleEn = entry.Example,
                ExampleZh = string.Empty,
                KeyExpressions = new List<KeyExpression>()
            };

            return new LookupResponse(result, dictionaryJson, null, LookupSource.Dictionary);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Miss("词典查询超时");
        }
        catch (HttpRequestException)
        {
            return Miss("词典网络连接失败");
        }
        catch (JsonException)
        {
            return Miss("词典返回格式无效");
        }
    }

    private static LookupResponse Miss(string error) =>
        new(null, null, error, LookupSource.Dictionary);

    private static bool IsAsciiLetter(char character) =>
        character is >= 'A' and <= 'Z' or >= 'a' and <= 'z';

    public void Dispose() => _httpClient.Dispose();
}
