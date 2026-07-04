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
    }
}
