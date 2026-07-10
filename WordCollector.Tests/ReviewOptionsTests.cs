using WordCollector.Models;
using WordCollector.ViewModels;

namespace WordCollector.Tests;

public class ReviewOptionsTests
{
    [Fact]
    public void Apply_FiltersWordsAndSortsByLookupCount()
    {
        var items = new[]
        {
            new VocabularyItem { Text = "phrase", ItemType = "phrase", LookupCount = 99 },
            new VocabularyItem { Text = "low", ItemType = "word", LookupCount = 1 },
            new VocabularyItem { Text = "high", ItemType = "word", LookupCount = 4 }
        };

        var result = ReviewOptions.Apply(items, "单词", "查询次数").ToList();
        Assert.Equal(new[] { "high", "low" }, result.Select(item => item.Text));
    }

    [Fact]
    public void OptionCollectionsContainTheDefaultSelections()
    {
        Assert.Contains("全部", ReviewOptions.ItemTypes);
        Assert.Contains("时间最新", ReviewOptions.SortModes);
        Assert.Contains("全部", ReviewOptions.Familiarities);
        Assert.Contains("已掌握", ReviewOptions.Familiarities);
    }

    [Theory]
    [InlineData("陌生", 0)]
    [InlineData("学习中", 1)]
    [InlineData("已掌握", 2)]
    public void ToFamiliarityLevel_MapsDisplayNamesToStoredValues(string display, int expected)
    {
        Assert.Equal(expected, ReviewOptions.ToFamiliarityLevel(display));
    }

    [Theory]
    [InlineData("全部")]
    [InlineData(null)]
    [InlineData("不认识")]
    public void ToFamiliarityLevel_ReturnsNullForAllOrUnknown(string? display)
    {
        Assert.Null(ReviewOptions.ToFamiliarityLevel(display));
    }

    [Fact]
    public void Apply_FiltersByFamiliarity()
    {
        var items = new[]
        {
            new VocabularyItem { Text = "new", ItemType = "word", Familiarity = 0 },
            new VocabularyItem { Text = "learning", ItemType = "word", Familiarity = 1 },
            new VocabularyItem { Text = "mastered", ItemType = "word", Familiarity = 2 }
        };

        var result = ReviewOptions.Apply(items, "全部", "时间最新", "已掌握").ToList();

        Assert.Single(result);
        Assert.Equal("mastered", result[0].Text);
    }

    [Fact]
    public void Apply_IgnoresFamiliarityWhenAllSelected()
    {
        var items = new[]
        {
            new VocabularyItem { Text = "a", ItemType = "word", Familiarity = 0 },
            new VocabularyItem { Text = "b", ItemType = "word", Familiarity = 2 }
        };

        var result = ReviewOptions.Apply(items, "全部", "时间最新", "全部").ToList();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_CombinesTypeAndFamiliarityFilters()
    {
        var items = new[]
        {
            new VocabularyItem { Text = "word-mastered", ItemType = "word", Familiarity = 2 },
            new VocabularyItem { Text = "phrase-mastered", ItemType = "phrase", Familiarity = 2 },
            new VocabularyItem { Text = "word-new", ItemType = "word", Familiarity = 0 }
        };

        var result = ReviewOptions.Apply(items, "单词", "时间最新", "已掌握").ToList();

        Assert.Single(result);
        Assert.Equal("word-mastered", result[0].Text);
    }
}
