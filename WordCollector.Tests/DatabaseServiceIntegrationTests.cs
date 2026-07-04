using Microsoft.Data.Sqlite;
using WordCollector.Models;
using WordCollector.Services;

namespace WordCollector.Tests;

public class DatabaseServiceIntegrationTests
{
    [Fact]
    public void InsertAndFind_RoundTripsThroughNativeSqliteProvider()
    {
        var directory = Path.Combine(
            Path.GetTempPath(), "WordCollector.Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(directory, "words.db");

        try
        {
            var service = new DatabaseService(databasePath);
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
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }
}
