using WordCollector.Models;

namespace WordCollector.Services;

/// <summary>
/// 把多个 <see cref="IDictionarySource"/> 组合成一个词典提供方：按注册顺序尝试，
/// 返回第一个命中的结果；全部未命中时返回最后一个来源的错误信息。
/// </summary>
public sealed class CompositeDictionaryProvider : IDictionaryLookupProvider, IDisposable
{
    private readonly IReadOnlyList<IDictionarySource> _sources;

    public CompositeDictionaryProvider(IEnumerable<IDictionarySource> sources)
    {
        _sources = sources.ToArray();
    }

    public bool CanLookup(string text) => _sources.Any(source => source.CanLookup(text));

    public async Task<LookupResponse> QueryAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        LookupResponse? lastMiss = null;

        foreach (var source in _sources)
        {
            if (!source.CanLookup(text))
                continue;

            var response = await source.QueryAsync(text, cancellationToken);
            if (response.Result != null)
                return response;

            lastMiss = response;
        }

        return lastMiss ?? new LookupResponse(null, null, "没有可用的词典来源", LookupSource.Dictionary);
    }

    public void Dispose()
    {
        foreach (var source in _sources)
        {
            if (source is IDisposable disposable)
                disposable.Dispose();
        }
    }
}
