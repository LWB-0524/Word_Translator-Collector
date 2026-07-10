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

    [Fact]
    public void GetDueForReview_ReturnsNullAndPastDatesButNotFuture()
    {
        WithService(service =>
        {
            service.Insert(NewItem("due-null", "空日期即到期")); // next_review_date 默认 null
            service.Insert(NewItem("due-past", "过去日期", nextReviewDate: "2026-07-01"));
            service.Insert(NewItem("not-due", "未来日期", nextReviewDate: "2026-07-20"));

            var due = service.GetDueForReview("2026-07-10");
            var texts = due.Select(i => i.Text).ToList();

            Assert.Contains("due-null", texts);
            Assert.Contains("due-past", texts);
            Assert.DoesNotContain("not-due", texts);
            Assert.Equal(2, service.GetDueCount("2026-07-10"));
        });
    }

    [Fact]
    public void UpdateReviewState_PersistsSrsFieldsAndFamiliarity()
    {
        WithService(service =>
        {
            var id = service.Insert(NewItem("card", "词条"));
            var item = service.GetById(id)!;
            item.ReviewRepetitions = 3;
            item.ReviewIntervalDays = 17;
            item.ReviewEaseFactor = 2.62;
            item.NextReviewDate = "2026-07-27";
            item.LastReviewedAt = "2026-07-10 09:00:00";
            item.Familiarity = 2;

            service.UpdateReviewState(item);
            var reloaded = service.GetById(id)!;

            Assert.Equal(3, reloaded.ReviewRepetitions);
            Assert.Equal(17, reloaded.ReviewIntervalDays);
            Assert.Equal(2.62, reloaded.ReviewEaseFactor, 3);
            Assert.Equal("2026-07-27", reloaded.NextReviewDate);
            Assert.Equal(2, reloaded.Familiarity);
        });
    }

    [Fact]
    public void GetStatistics_AggregatesCountsAndDue()
    {
        WithService(service =>
        {
            service.Insert(NewItem("a", "甲", familiarity: 2));
            service.Insert(NewItem("b", "乙", familiarity: 0));
            service.Insert(NewItem("c", "丙", familiarity: 2, nextReviewDate: "2026-07-20"));

            var stats = service.GetStatistics("2026-07-10");

            Assert.Equal(3, stats.TotalItems);
            Assert.Equal(2, stats.MasteredCount);
            Assert.Equal(2, stats.DueToday); // a、b 到期（null），c 未来
        });
    }

    private static VocabularyItem NewItem(
        string text, string meaning, int familiarity = 0, string? nextReviewDate = null) =>
        new()
        {
            Text = text,
            NormalizedText = text.ToLowerInvariant(),
            ItemType = "phrase",
            MeaningZh = meaning,
            Familiarity = familiarity,
            NextReviewDate = nextReviewDate,
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
