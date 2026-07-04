using WordCollector.Models;

namespace WordCollector.ViewModels;

public static class ReviewOptions
{
    public static IReadOnlyList<string> ItemTypes { get; } =
        new[] { "全部", "单词", "词组", "句子" };

    public static IReadOnlyList<string> SortModes { get; } =
        new[] { "时间最新", "查询次数", "朗读次数" };

    public static IEnumerable<VocabularyItem> Apply(
        IEnumerable<VocabularyItem> items, string? selectedType, string? sortMode)
    {
        var filtered = selectedType switch
        {
            "单词" => items.Where(item => item.ItemType == "word"),
            "词组" => items.Where(item => item.ItemType == "phrase"),
            "句子" => items.Where(item => item.ItemType == "sentence"),
            _ => items
        };

        return sortMode switch
        {
            "查询次数" => filtered.OrderByDescending(item => item.LookupCount),
            "朗读次数" => filtered.OrderByDescending(item => item.SpokenCount),
            _ => filtered.OrderByDescending(item => item.CreatedAt)
        };
    }
}
