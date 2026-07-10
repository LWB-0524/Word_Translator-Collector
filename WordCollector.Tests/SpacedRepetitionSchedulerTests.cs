using WordCollector.Helpers;

namespace WordCollector.Tests;

public class SpacedRepetitionSchedulerTests
{
    private static readonly DateTime Today = new(2026, 7, 10);

    [Fact]
    public void Advance_NewCardGood_SchedulesOneDayLater()
    {
        var state = SpacedRepetitionScheduler.Advance(0, 0, 2.5, ReviewGrade.Good, Today);

        Assert.Equal(1, state.Repetitions);
        Assert.Equal(1, state.IntervalDays);
        Assert.Equal(2.6, state.EaseFactor, 3);
        Assert.Equal("2026-07-11", state.NextReviewDate);
    }

    [Fact]
    public void Advance_SecondGood_SchedulesSixDays()
    {
        var state = SpacedRepetitionScheduler.Advance(1, 1, 2.6, ReviewGrade.Good, Today);

        Assert.Equal(2, state.Repetitions);
        Assert.Equal(6, state.IntervalDays);
        Assert.Equal("2026-07-16", state.NextReviewDate);
    }

    [Fact]
    public void Advance_ThirdGood_MultipliesByEaseFactor()
    {
        // ease 2.7 -> 2.8 after a good grade; 6 * 2.8 = 16.8 -> 17.
        var state = SpacedRepetitionScheduler.Advance(2, 6, 2.7, ReviewGrade.Good, Today);

        Assert.Equal(3, state.Repetitions);
        Assert.Equal(17, state.IntervalDays);
    }

    [Fact]
    public void Advance_Forgot_ResetsProgressAndLowersEase()
    {
        var state = SpacedRepetitionScheduler.Advance(5, 40, 2.5, ReviewGrade.Forgot, Today);

        Assert.Equal(0, state.Repetitions);
        Assert.Equal(1, state.IntervalDays);
        Assert.True(state.EaseFactor < 2.5);
        Assert.Equal("2026-07-11", state.NextReviewDate);
    }

    [Fact]
    public void Advance_EaseFactorNeverDropsBelowMinimum()
    {
        var state = SpacedRepetitionScheduler.Advance(0, 1, SpacedRepetitionScheduler.MinEase, ReviewGrade.Forgot, Today);

        Assert.Equal(SpacedRepetitionScheduler.MinEase, state.EaseFactor);
    }

    [Fact]
    public void Advance_UninitializedEaseFallsBackToDefault()
    {
        var state = SpacedRepetitionScheduler.Advance(0, 0, 0, ReviewGrade.Good, Today);

        // 0 -> default 2.5, then +0.1 for a good grade.
        Assert.Equal(2.6, state.EaseFactor, 3);
    }

    [Theory]
    [InlineData(ReviewGrade.Forgot, 2)]
    [InlineData(ReviewGrade.Hard, 3)]
    [InlineData(ReviewGrade.Good, 5)]
    public void GradeToQuality_MapsGrades(ReviewGrade grade, double expected)
    {
        Assert.Equal(expected, SpacedRepetitionScheduler.GradeToQuality(grade));
    }
}
