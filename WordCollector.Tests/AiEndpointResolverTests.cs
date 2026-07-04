using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class AiEndpointResolverTests
{
    [Fact]
    public void TryResolve_UsesDeepSeekDefaultsWhenFieldsAreBlank()
    {
        var settings = new AppSettings
        {
            AiProvider = "deepseek",
            AiBaseUrl = string.Empty,
            AiModel = string.Empty
        };

        var ok = AiEndpointResolver.TryResolve(settings, out var endpoint, out var model, out var error);

        Assert.True(ok, error);
        Assert.Equal("https://api.deepseek.com/v1/chat/completions", endpoint.AbsoluteUri);
        Assert.Equal("deepseek-chat", model);
    }

    [Fact]
    public void TryResolve_DoesNotDuplicateExactChatCompletionsPath()
    {
        var settings = new AppSettings
        {
            AiProvider = "custom",
            AiBaseUrl = "https://example.com/v1/chat/completions",
            AiModel = "custom-model"
        };

        var ok = AiEndpointResolver.TryResolve(settings, out var endpoint, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal("https://example.com/v1/chat/completions", endpoint.AbsoluteUri);
    }

    [Fact]
    public void TryResolve_RejectsInsecureRemoteHttpEndpoint()
    {
        var settings = new AppSettings
        {
            AiProvider = "custom",
            AiBaseUrl = "http://example.com",
            AiModel = "custom-model"
        };

        var ok = AiEndpointResolver.TryResolve(settings, out _, out _, out var error);

        Assert.False(ok);
        Assert.Contains("HTTPS", error);
    }

    [Fact]
    public void TryResolve_AllowsHttpForLoopbackDevelopmentEndpoint()
    {
        var settings = new AppSettings
        {
            AiProvider = "custom",
            AiBaseUrl = "http://127.0.0.1:11434",
            AiModel = "local-model"
        };

        var ok = AiEndpointResolver.TryResolve(settings, out var endpoint, out _, out var error);

        Assert.True(ok, error);
        Assert.Equal("http://127.0.0.1:11434/v1/chat/completions", endpoint.AbsoluteUri);
    }
}
