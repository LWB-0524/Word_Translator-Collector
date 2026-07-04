using WordCollector.Models;

namespace WordCollector.Services;

public static class AiEndpointResolver
{
    public static IReadOnlyList<string> Providers { get; } =
        new[] { "openai", "deepseek", "custom" };

    public static (string? BaseUrl, string? Model) GetProviderDefaults(string? provider)
    {
        return provider?.Trim().ToLowerInvariant() switch
        {
            "openai" => ("https://api.openai.com", "gpt-4o-mini"),
            "deepseek" => ("https://api.deepseek.com", "deepseek-chat"),
            _ => (null, null)
        };
    }

    public static bool TryResolve(
        AppSettings settings,
        out Uri endpoint,
        out string model,
        out string error)
    {
        endpoint = null!;
        model = string.Empty;
        error = string.Empty;

        var provider = settings.AiProvider?.Trim().ToLowerInvariant();
        if (!Providers.Contains(provider))
        {
            error = "不支持的 API Provider";
            return false;
        }

        var defaults = GetProviderDefaults(provider);
        var baseUrl = string.IsNullOrWhiteSpace(settings.AiBaseUrl)
            ? defaults.BaseUrl
            : settings.AiBaseUrl.Trim();
        model = string.IsNullOrWhiteSpace(settings.AiModel)
            ? defaults.Model ?? string.Empty
            : settings.AiModel.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(model))
        {
            error = "请配置有效的 Base URL 和模型名称";
            return false;
        }

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
            (baseUri.Scheme != Uri.UriSchemeHttps && baseUri.Scheme != Uri.UriSchemeHttp))
        {
            error = "Base URL 格式无效";
            return false;
        }

        if (baseUri.Scheme == Uri.UriSchemeHttp && !baseUri.IsLoopback)
        {
            error = "远程 API 必须使用 HTTPS；HTTP 仅允许本机回环地址";
            return false;
        }

        if (!string.IsNullOrEmpty(baseUri.UserInfo))
        {
            error = "Base URL 不应包含用户名或密码";
            return false;
        }

        var builder = new UriBuilder(baseUri);
        var path = builder.Path.TrimEnd('/');
        if (!path.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase))
        {
            path = path.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)
                ? $"{path}/chat/completions"
                : $"{path}/v1/chat/completions";
        }

        builder.Path = path;
        endpoint = builder.Uri;
        return true;
    }
}
