using WordCollector.Models;

namespace WordCollector.Services;

public interface IDictionaryLookupProvider
{
    bool CanLookup(string text);

    Task<LookupResponse> QueryAsync(
        string text,
        CancellationToken cancellationToken = default);
}

public interface IAiLookupProvider
{
    Task<(AiExplanationResult? Result, string? RawResponse, string? Error)> QueryAsync(
        string text,
        CancellationToken cancellationToken = default);
}
