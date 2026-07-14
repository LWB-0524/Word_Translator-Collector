using Microsoft.Data.Sqlite;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class VocabularyImportServiceTests
{
    [Fact]
    public void ParseSeed_ParsesEntries()
    {
        const string json = """
            [{"text":"beam","item_type":"word","phonetic":"/biːm/","meaning_zh":"梁","example_en":"A steel beam.","example_zh":"一根钢梁。"}]
            """;

        var entries = VocabularyImportService.ParseSeed(json);

        Assert.Single(entries);
        Assert.Equal("beam", entries[0].Text);
        Assert.Equal("梁", entries[0].MeaningZh);
        Assert.Equal("/biːm/", entries[0].Phonetic);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not json")]
    public void ParseSeed_ReturnsEmptyOnInvalidInput(string json)
    {
        Assert.Empty(VocabularyImportService.ParseSeed(json));
    }

    [Fact]
    public void Import_InsertsNewEntriesAndDeduplicatesByNormalizedText()
    {
        WithDatabase(database =>
        {
            database.Insert(new VocabularyItem
            {
                Text = "Beam",
                NormalizedText = "beam",
                MeaningZh = "梁",
                DateAdded = "2026-07-01",
                CreatedAt = "2026-07-01 00:00:00"
            });

            var entries = new[]
            {
                new VocabularySeedEntry { Text = "beam", MeaningZh = "梁" },      // 已存在（大小写不敏感）
                new VocabularySeedEntry { Text = "column", MeaningZh = "柱" }     // 新增
            };

            var result = VocabularyImportService.Import(database, entries);

            Assert.Equal(1, result.Added);
            Assert.Equal(1, result.Skipped);
            Assert.NotNull(database.FindByNormalizedText("column"));
        });
    }

    [Fact]
    public void Import_SkipsEntriesMissingTextOrMeaning()
    {
        WithDatabase(database =>
        {
            var entries = new[]
            {
                new VocabularySeedEntry { Text = "", MeaningZh = "有释义" },
                new VocabularySeedEntry { Text = "slab", MeaningZh = "" }
            };

            var result = VocabularyImportService.Import(database, entries);

            Assert.Equal(0, result.Added);
            Assert.Equal(2, result.Skipped);
        });
    }

    [Fact]
    public void ImportBuiltIn_LoadsBundledCivilEngineeringTermsAndIsIdempotent()
    {
        WithDatabase(database =>
        {
            var first = VocabularyImportService.ImportBuiltIn(database);

            Assert.True(first.Added >= 100, $"expected the bundled list to add many terms, got {first.Added}");
            Assert.NotNull(database.FindByNormalizedText("concrete"));
            Assert.NotNull(database.FindByNormalizedText("reinforced concrete"));
            Assert.NotNull(database.FindByNormalizedText("bending moment"));

            // 再次导入应全部去重、不新增。
            var second = VocabularyImportService.ImportBuiltIn(database);
            Assert.Equal(0, second.Added);
            Assert.Equal(first.Added, second.Skipped);
        });
    }

    private static void WithDatabase(Action<DatabaseService> test)
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "WordCollector.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "words.db");

        try
        {
            test(new DatabaseService(databasePath));
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
