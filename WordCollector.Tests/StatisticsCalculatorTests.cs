using WordCollector.Helpers;

namespace WordCollector.Tests;

public class StatisticsCalculatorTests
{
    private static readonly DateTime Today = new(2026, 7, 10);

    [Fact]
    public void CalculateStreak_CountsConsecutiveDaysEndingToday()
    {
        var dates = new[] { "2026-07-10", "2026-07-09", "2026-07-08" };
        Assert.Equal(3, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_AllowsTodayToBeMissing()
    {
        // 今天还没记录，但昨天及之前连续 → 从昨天数起。
        var dates = new[] { "2026-07-09", "2026-07-08" };
        Assert.Equal(2, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_StopsAtFirstGap()
    {
        var dates = new[] { "2026-07-10", "2026-07-09", "2026-07-07" };
        Assert.Equal(2, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_ReturnsZeroWhenYesterdayAndTodayMissing()
    {
        var dates = new[] { "2026-07-08", "2026-07-07" };
        Assert.Equal(0, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_DeduplicatesDates()
    {
        var dates = new[] { "2026-07-10", "2026-07-10", "2026-07-09" };
        Assert.Equal(2, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_IgnoresUnparseableDates()
    {
        var dates = new[] { "2026-07-10", "not-a-date", "2026-07-09" };
        Assert.Equal(2, StatisticsCalculator.CalculateStreak(dates, Today));
    }

    [Fact]
    public void CalculateStreak_EmptyReturnsZero()
    {
        Assert.Equal(0, StatisticsCalculator.CalculateStreak(Array.Empty<string>(), Today));
    }
}
