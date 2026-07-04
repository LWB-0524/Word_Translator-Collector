using Microsoft.Data.Sqlite;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class LookupServiceTests
{
    [Fact]
    public async Task QueryAsync_ReturnsLocalEntryWithoutNetworkCalls()
    {
        await WithService(async (database, dictionary, ai, service) =>
        {
            database.Insert(new VocabularyItem
            {
                Text = "hello",
                NormalizedText = "hello",
                ItemType = "word",
                MeaningZh = "你好",
                DateAdded = "2026-07-01",
                CreatedAt = "2026-07-01 10:00:00"
            });

            var response = await service.QueryAsync("hello");

            Assert.Equal(LookupSource.Local, response.Source);
            Assert.Equal("你好", response.Result?.MeaningZh);
            Assert.Equal(0, dictionary.CallCount);
            Assert.Equal(0, ai.CallCount);
        });
    }

    [Fact]
    public async Task QueryAsync_ReturnsDictionaryResultBeforeAi()
    {
        await WithService(async (_, dictionary, ai, service) =>
        {
            dictionary.Response = Success("词典释义", LookupSource.Dictionary);

            var response = await service.QueryAsync("hello");

            Assert.Equal(LookupSource.Dictionary, response.Source);
            Assert.Equal("词典释义", response.Result?.MeaningZh);
            Assert.Equal(1, dictionary.CallCount);
            Assert.Equal(0, ai.CallCount);
        });
    }

    [Fact]
    public async Task QueryAsync_FallsBackToAiWhenDictionaryMisses()
    {
        await WithService(async (_, dictionary, ai, service) =>
        {
            dictionary.Response = new LookupResponse(null, null, "not found", LookupSource.Dictionary);
            ai.Response = (new AiExplanationResult { ItemType = "word", MeaningZh = "AI 释义" }, null, null);

            var response = await service.QueryAsync("hello");

            Assert.Equal(LookupSource.Ai, response.Source);
            Assert.Equal("AI 释义", response.Result?.MeaningZh);
            Assert.Equal(1, dictionary.CallCount);
            Assert.Equal(1, ai.CallCount);
        });
    }

    [Fact]
    public async Task QueryAsync_SkipsDictionaryForPhrases()
    {
        await WithService(async (_, dictionary, ai, service) =>
        {
            ai.Response = (new AiExplanationResult { ItemType = "phrase", MeaningZh = "打破僵局" }, null, null);

            var response = await service.QueryAsync("break the ice");

            Assert.Equal(LookupSource.Ai, response.Source);
            Assert.Equal(0, dictionary.CallCount);
            Assert.Equal(1, ai.CallCount);
        });
    }

    private static LookupResponse Success(string meaning, LookupSource source) =>
        new(new AiExplanationResult { ItemType = "word", MeaningZh = meaning }, null, null, source);

    private static async Task WithService(
        Func<DatabaseService, FakeDictionaryProvider, FakeAiProvider, LookupService, Task> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "WordCollector.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "words.db");

        try
        {
            var database = new DatabaseService(databasePath);
            var dictionary = new FakeDictionaryProvider();
            var ai = new FakeAiProvider();
            var service = new LookupService(database, dictionary, ai);
            await test(database, dictionary, ai, service);
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class FakeDictionaryProvider : IDictionaryLookupProvider
    {
        public int CallCount { get; private set; }
        public LookupResponse Response { get; set; } = Success("词典释义", LookupSource.Dictionary);
        public bool CanLookup(string text) => DictionaryLookupService.CanLookup(text);

        public Task<LookupResponse> QueryAsync(string text, CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Response);
        }
    }

    private sealed class FakeAiProvider : IAiLookupProvider
    {
        public int CallCount { get; private set; }
        public (AiExplanationResult? Result, string? RawResponse, string? Error) Response { get; set; } =
            (new AiExplanationResult { ItemType = "word", MeaningZh = "AI 释义" }, null, null);

        public Task<(AiExplanationResult? Result, string? RawResponse, string? Error)> QueryAsync(
            string text,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult(Response);
        }
    }
}
