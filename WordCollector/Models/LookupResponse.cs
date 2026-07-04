namespace WordCollector.Models;

public enum LookupSource
{
    Local,
    Dictionary,
    Ai
}

public sealed record LookupResponse(
    AiExplanationResult? Result,
    string? RawResponse,
    string? Error,
    LookupSource Source);
