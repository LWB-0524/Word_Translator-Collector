namespace WordCollector.Helpers;

/// <summary>用户对一次复习的自评。</summary>
public enum ReviewGrade
{
    Forgot, // 忘记
    Hard,   // 模糊
    Good    // 记得
}

/// <summary>一个词条的间隔重复状态。</summary>
public readonly record struct SrsState(
    int Repetitions,
    int IntervalDays,
    double EaseFactor,
    string NextReviewDate);

/// <summary>
/// SM-2 间隔重复算法的纯逻辑实现（无 UI / 无 IO，便于单测）。
/// 评分映射到 SM-2 质量分：忘记=2、模糊=3、记得=5。
/// </summary>
public static class SpacedRepetitionScheduler
{
    public const double DefaultEase = 2.5;
    public const double MinEase = 1.3;

    public static double GradeToQuality(ReviewGrade grade) => grade switch
    {
        ReviewGrade.Forgot => 2,
        ReviewGrade.Hard => 3,
        _ => 5
    };

    /// <summary>
    /// 根据当前状态和本次评分推进到下一个复习状态。
    /// </summary>
    public static SrsState Advance(
        int repetitions, int intervalDays, double easeFactor, ReviewGrade grade, DateTime today)
    {
        var quality = GradeToQuality(grade);

        // EaseFactor 每次都更新；无效/未初始化时回落到默认值。
        var ease = easeFactor < MinEase ? DefaultEase : easeFactor;
        ease += 0.1 - (5 - quality) * (0.08 + (5 - quality) * 0.02);
        if (ease < MinEase) ease = MinEase;

        int reps;
        int interval;
        if (grade == ReviewGrade.Forgot)
        {
            // 答错：重置进度，明天再复习。
            reps = 0;
            interval = 1;
        }
        else
        {
            reps = repetitions + 1;
            interval = reps switch
            {
                1 => 1,
                2 => 6,
                _ => Math.Max(1, (int)Math.Round(Math.Max(1, intervalDays) * ease))
            };
        }

        var next = today.Date.AddDays(interval).ToString("yyyy-MM-dd");
        return new SrsState(reps, interval, Math.Round(ease, 4), next);
    }
}
