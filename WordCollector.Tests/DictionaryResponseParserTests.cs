using WordCollector.Services;
using WordCollector.Models;

namespace WordCollector.Tests;

public class DictionaryResponseParserTests
{
    [Fact]
    public async Task QueryAsync_StartsDictionaryAndTranslationRequestsInParallel()
    {
        using var handler = new ParallelProbeHandler();
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(2) };
        using var service = new DictionaryLookupService(client);

        var response = await service.QueryAsync("hello", TestContext.Current.CancellationToken);

        Assert.Equal(LookupSource.Dictionary, response.Source);
        Assert.Equal("你好", response.Result?.MeaningZh);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact]
    public void TryParseDictionary_ExtractsPhoneticDefinitionAndExample()
    {
        const string json = """
            [{
              "word": "hello",
              "phonetic": "həˈləʊ",
              "meanings": [{
                "partOfSpeech": "exclamation",
                "definitions": [{
                  "definition": "used as a greeting.",
                  "example": "Hello there!"
                }]
              }]
            }]
            """;

        var parsed = DictionaryResponseParser.TryParseDictionary(json, out var entry);

        Assert.True(parsed);
        Assert.Equal("/həˈləʊ/", entry.Phonetic);
        Assert.Equal("exclamation", entry.PartOfSpeech);
        Assert.Equal("used as a greeting.", entry.Definition);
        Assert.Equal("Hello there!", entry.Example);
    }

    [Fact]
    public void TryParseTranslation_DecodesAndValidatesChineseText()
    {
        const string json = """
            {"responseStatus":200,"responseData":{"translatedText":"你好&amp;欢迎"}}
            """;

        var parsed = DictionaryResponseParser.TryParseTranslation(json, out var translation);

        Assert.True(parsed);
        Assert.Equal("你好&欢迎", translation);
    }

    [Theory]
    [InlineData("hello", true)]
    [InlineData("well-known", true)]
    [InlineData("don't", true)]
    [InlineData("break the ice", false)]
    [InlineData("hello!", false)]
    public void CanLookup_OnlyAcceptsSingleDictionaryWords(string text, bool expected)
    {
        Assert.Equal(expected, DictionaryLookupService.CanLookup(text));
    }

    private sealed class ParallelProbeHandler : HttpMessageHandler
    {
        private readonly TaskCompletionSource _bothRequestsStarted =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _requestCount;

        public int RequestCount => _requestCount;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (Interlocked.Increment(ref _requestCount) == 2)
                _bothRequestsStarted.TrySetResult();

            await _bothRequestsStarted.Task.WaitAsync(cancellationToken);

            var json = request.RequestUri!.Host.Contains("dictionaryapi", StringComparison.Ordinal)
                ? """[{"phonetic":"həˈləʊ","meanings":[{"partOfSpeech":"noun","definitions":[{"definition":"a greeting"}]}]}]"""
                : """{"responseData":{"translatedText":"你好"}}""";
            return new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            };
        }
    }
}
