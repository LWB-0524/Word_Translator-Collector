using System.Text.Json;
using WordCollector.Helpers;
using WordCollector.Models;

namespace WordCollector.Services;

public sealed class LookupService
{
    private readonly DatabaseService _databaseService;
    private readonly IDictionaryLookupProvider _dictionaryProvider;
    private readonly IAiLookupProvider _aiProvider;

    public LookupService(
        DatabaseService databaseService,
        IDictionaryLookupProvider dictionaryProvider,
        IAiLookupProvider aiProvider)
    {
        _databaseService = databaseService;
        _dictionaryProvider = dictionaryProvider;
        _aiProvider = aiProvider;
    }

    public async Task<LookupResponse> QueryAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        var normalized = TextNormalizer.Normalize(text);
        var localItem = _databaseService.FindByNormalizedText(normalized);
        if (localItem != null)
            return FromLocal(localItem);

        if (_dictionaryProvider.CanLookup(text))
        {
            var dictionaryResponse = await _dictionaryProvider.QueryAsync(text, cancellationToken);
            if (dictionaryResponse.Result != null)
                return dictionaryResponse;
        }

        var (result, rawResponse, error) = await _aiProvider.QueryAsync(text, cancellationToken);
        return new LookupResponse(result, rawResponse, error, LookupSource.Ai);
    }

    private static LookupResponse FromLocal(VocabularyItem item)
    {
        var keyExpressions = string.IsNullOrWhiteSpace(item.KeyExpressionsJson)
            ? new List<KeyExpression>()
            : JsonHelper.SafeDeserialize<List<KeyExpression>>(item.KeyExpressionsJson)
              ?? new List<KeyExpression>();

        var result = new AiExplanationResult
        {
            ItemType = item.ItemType ?? "word",
            Phonetic = item.Phonetic ?? string.Empty,
            MeaningZh = item.MeaningZh,
            BriefExplanation = item.BriefExplanation ?? string.Empty,
            DetailedExplanation = item.DetailedExplanation ?? string.Empty,
            ExampleEn = item.ExampleEn ?? string.Empty,
            ExampleZh = item.ExampleZh ?? string.Empty,
            KeyExpressions = keyExpressions
        };

        return new LookupResponse(result, item.RawAiResponse, null, LookupSource.Local);
    }
}
