namespace WordCollector.Helpers;

public static class TextNormalizer
{
    public static string Normalize(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Trim().ToLowerInvariant();
    }
}
