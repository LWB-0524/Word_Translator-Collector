using Microsoft.Data.Sqlite;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class DatabaseServiceIntegrationTests
{
    [Fact]
    public void InsertAndFind_RoundTripsThroughNativeSqliteProvider()
    {
        WithService(service =>
        {
            var item = new VocabularyItem
            {
                Text = "A test",
                NormalizedText = "a test",
                ItemType = "phrase",
                MeaningZh = "一次测试",
                DateAdded = "2026-06-30",
                CreatedAt = "2026-06-30 12:00:00"
            };

            var id = service.Insert(item);
            var loaded = service.FindByNormalizedTextAndDate("a test", "2026-06-30");

            Assert.True(id > 0);
            Assert.NotNull(loaded);
            Assert.Equal("一次测试", loaded.MeaningZh);
        });
    }

    [Fact]
    public void Search_FiltersByFamiliarity()
    {
        WithService(service =>
        {
            service.Insert(NewItem("mastered", "已掌握", familiarity: 2));
            service.Insert(NewItem("new", "陌生", familiarity: 0));

            var mastered = service.Search(null, null, null, null, familiarity: 2);

            Assert.Single(mastered);
            Assert.Equal("mastered", mastered[0].Text);
        });
    }

    [Fact]
    public void Search_EscapesLikeWildcardsInSearchText()
    {
        WithService(service =>
        {
            service.Insert(NewItem("50% off", "五折"));
            service.Insert(NewItem("5000 dollars", "五千美元"));

            // 未转义时 "50%" 会被当成通配符匹配到 "5000..."；转义后只应命中字面包含 "50%" 的记录。
            var results = service.Search("50%", null, null, null, null);

            Assert.Single(results);
            Assert.Equal("50% off", results[0].Text);
        });
    }

    private static VocabularyItem NewItem(string text, string meaning, int familiarity = 0) =>
        new()
        {
            Text = text,
            NormalizedText = text.ToLowerInvariant(),
            ItemType = "phrase",
            MeaningZh = meaning,
            Familiarity = familiarity,
            DateAdded = "2026-06-30",
            CreatedAt = "2026-06-30 12:00:00"
        };

    private static void WithService(Action<DatabaseService> test)
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
