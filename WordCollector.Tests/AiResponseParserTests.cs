using WordCollector.Services;

namespace WordCollector.Tests;

public class AiResponseParserTests
{
    [Fact]
    public void TryExtractAssistantContent_ReadsOpenAiCompatibleResponse()
    {
        const string response = """
            {"choices":[{"message":{"content":"{\"item_type\":\"word\",\"meaning_zh\":\"测试\"}"}}]}
            """;

        var ok = AiResponseParser.TryExtractAssistantContent(response, out var content, out var error);

        Assert.True(ok, error);
        Assert.Contains("meaning_zh", content);
    }

    [Fact]
    public void TryParseExplanation_AcceptsMarkdownJsonFence()
    {
        const string content = """
            ```json
            {
              "item_type": "word",
              "phonetic": "/test/",
              "meaning_zh": "测试",
              "brief_explanation": "简要说明",
              "detailed_explanation": "详细说明",
              "example_en": "This is a test.",
              "example_zh": "这是一个测试。",
              "key_expressions": []
            }
            ```
            """;

        var ok = AiResponseParser.TryParseExplanation(content, out var result, out var rawContent);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.Equal("测试", result.MeaningZh);
        Assert.StartsWith("{", rawContent);
    }

    [Fact]
    public void TryParseExplanation_NormalizesNullKeyExpressions()
    {
        const string content = """
            {"item_type":"sentence","meaning_zh":"测试句子","key_expressions":null}
            """;

        var ok = AiResponseParser.TryParseExplanation(content, out var result, out _);

        Assert.True(ok);
        Assert.NotNull(result);
        Assert.NotNull(result.KeyExpressions);
        Assert.Empty(result.KeyExpressions);
    }

    [Fact]
    public void TryParseExplanation_RejectsMissingMeaningButPreservesRawContent()
    {
        const string content = "{" + "\"item_type\":\"word\"" + "}";

        var ok = AiResponseParser.TryParseExplanation(content, out var result, out var rawContent);

        Assert.False(ok);
        Assert.Null(result);
        Assert.Equal(content, rawContent);
    }
}
