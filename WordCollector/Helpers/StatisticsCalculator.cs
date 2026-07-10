using System.Globalization;

namespace WordCollector.Helpers;

/// <summary>词库统计快照。</summary>
public sealed record VocabularyStatistics(
    int TotalItems,
    int WordCount,
    int PhraseCount,
    int SentenceCount,
    int NewCount,
    int LearningCount,
    int MasteredCount,
    int TotalLookups,
    int TotalSpoken,
    int ActiveDays,
    int CurrentStreak,
    int DueToday)
{
    public static VocabularyStatistics Empty { get; } =
        new(0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
}

public static class StatisticsCalculator
{
    /// <summary>
    /// 根据"有记录的日期集合"计算截至 today 的连续打卡天数。
    /// 允许今天还没有记录（此时从昨天往前数），一旦断档即停止。
    /// </summary>
    public static int CalculateStreak(IEnumerable<string> activeDates, DateTime today)
    {
        var days = new HashSet<DateTime>();
        foreach (var date in activeDates)
        {
            if (DateTime.TryParseExact(
                    date?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed))
            {
                days.Add(parsed.Date);
            }
        }

        if (days.Count == 0) return 0;

        var cursor = today.Date;
        // 今天没记录不算断，从昨天继续数；但如果昨天也没有则连续数为 0。
        if (!days.Contains(cursor))
        {
            cursor = cursor.AddDays(-1);
            if (!days.Contains(cursor)) return 0;
        }

        var streak = 0;
        while (days.Contains(cursor))
        {
            streak++;
            cursor = cursor.AddDays(-1);
        }

        return streak;
    }
}
