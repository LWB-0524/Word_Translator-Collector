using WordCollector.Models;

namespace WordCollector.ViewModels;

public static class ReviewOptions
{
    public static IReadOnlyList<string> ItemTypes { get; } =
        new[] { "全部", "单词", "词组", "句子" };

    public static IReadOnlyList<string> SortModes { get; } =
        new[] { "时间最新", "查询次数", "朗读次数" };

    public static IReadOnlyList<string> Familiarities { get; } =
        new[] { "全部", "陌生", "学习中", "已掌握" };

    /// <summary>
    /// 熟悉度显示名 → 存储值（0 陌生 / 1 学习中 / 2 已掌握）；“全部”或未知返回 null 表示不过滤。
    /// </summary>
    public static int? ToFamiliarityLevel(string? display) =>
        display switch
        {
            "陌生" => 0,
            "学习中" => 1,
            "已掌握" => 2,
            _ => null
        };

    public static IEnumerable<VocabularyItem> Apply(
        IEnumerable<VocabularyItem> items,
        string? selectedType,
        string? sortMode,
        string? selectedFamiliarity = null)
    {
        var filtered = selectedType switch
        {
            "单词" => items.Where(item => item.ItemType == "word"),
            "词组" => items.Where(item => item.ItemType == "phrase"),
            "句子" => items.Where(item => item.ItemType == "sentence"),
            _ => items
        };

        var familiarityLevel = ToFamiliarityLevel(selectedFamiliarity);
        if (familiarityLevel.HasValue)
            filtered = filtered.Where(item => item.Familiarity == familiarityLevel.Value);

        return sortMode switch
        {
            "查询次数" => filtered.OrderByDescending(item => item.LookupCount),
            "朗读次数" => filtered.OrderByDescending(item => item.SpokenCount),
            _ => filtered.OrderByDescending(item => item.CreatedAt)
        };
    }
}
