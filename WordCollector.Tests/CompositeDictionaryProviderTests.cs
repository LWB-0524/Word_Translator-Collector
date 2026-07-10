using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class CompositeDictionaryProviderTests
{
    [Fact]
    public async Task QueryAsync_ReturnsFirstHitAndSkipsLaterSources()
    {
        var first = new FakeSource("first") { Response = Hit("第一个") };
        var second = new FakeSource("second") { Response = Hit("第二个") };
        var provider = new CompositeDictionaryProvider(new[] { first, second });

        var response = await provider.QueryAsync("hello");

        Assert.Equal("第一个", response.Result?.MeaningZh);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(0, second.CallCount);
    }

    [Fact]
    public async Task QueryAsync_FallsThroughMissesToNextSource()
    {
        var first = new FakeSource("first") { Response = Miss("第一个没找到") };
        var second = new FakeSource("second") { Response = Hit("第二个命中") };
        var provider = new CompositeDictionaryProvider(new[] { first, second });

        var response = await provider.QueryAsync("hello");

        Assert.Equal("第二个命中", response.Result?.MeaningZh);
        Assert.Equal(1, first.CallCount);
        Assert.Equal(1, second.CallCount);
    }

    [Fact]
    public async Task QueryAsync_SkipsSourcesThatCannotHandleText()
    {
        var skipped = new FakeSource("skipped") { CanHandle = false, Response = Hit("不该被调用") };
        var used = new FakeSource("used") { Response = Hit("命中") };
        var provider = new CompositeDictionaryProvider(new[] { skipped, used });

        var response = await provider.QueryAsync("hello");

        Assert.Equal("命中", response.Result?.MeaningZh);
        Assert.Equal(0, skipped.CallCount);
        Assert.Equal(1, used.CallCount);
    }

    [Fact]
    public async Task QueryAsync_ReturnsLastMissWhenAllSourcesMiss()
    {
        var first = new FakeSource("first") { Response = Miss("第一个失败") };
        var second = new FakeSource("second") { Response = Miss("第二个失败") };
        var provider = new CompositeDictionaryProvider(new[] { first, second });

        var response = await provider.QueryAsync("hello");

        Assert.Null(response.Result);
        Assert.Equal("第二个失败", response.Error);
    }

    [Fact]
    public void CanLookup_TrueWhenAnySourceCanHandle()
    {
        var provider = new CompositeDictionaryProvider(new[]
        {
            new FakeSource("a") { CanHandle = false },
            new FakeSource("b") { CanHandle = true }
        });

        Assert.True(provider.CanLookup("hello"));
    }

    private static LookupResponse Hit(string meaning) =>
        new(new AiExplanationResult { ItemType = "word", MeaningZh = meaning }, null, null, LookupSource.Dictionary);

    private static LookupResponse Miss(string error) =>
        new(null, null, error, LookupSource.Dictionary);

    private sealed class FakeSource : IDictionarySource
    {
        public FakeSource(string name) => Name = name;

        public string Name { get; }
        public bool CanHandle { get; set; } = true;
        public int CallCount { get; private set; }
        public LookupResponse Response { get; set; } =
            new(null, null, "miss", LookupSource.Dictionary);

        public bool CanLookup(string text) => CanHandle;

        public Task<LookupResponse> QueryAsync(string text, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Response);
        }
    }
}
