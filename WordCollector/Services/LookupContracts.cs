using WordCollector.Models;

namespace WordCollector.Services;

public interface IDictionaryLookupProvider
{
    bool CanLookup(string text);

    Task<LookupResponse> QueryAsync(
        string text,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 单个词典来源。多个来源由 <see cref="CompositeDictionaryProvider"/> 按注册顺序尝试，
/// 新增一个来源只需实现本接口并加入组合列表。
/// </summary>
public interface IDictionarySource
{
    /// <summary>用于日志/展示的来源名称。</summary>
    string Name { get; }

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
