namespace WordCollector.Helpers;

public readonly record struct WindowSize(double Width, double Height);

public static class WindowSizePolicy
{
    public const double DefaultWidth = 440;
    public const double DefaultHeight = 320;
    public const double MinimumWidth = 400;
    public const double MinimumHeight = 280;

    public static WindowSize Resolve(double width, double height)
    {
        var isPreviousCanonicalSize =
            (Math.Abs(width - 720) < 0.5 && Math.Abs(height - 650) < 0.5) ||
            (Math.Abs(width - 520) < 0.5 && Math.Abs(height - 400) < 0.5) ||
            (Math.Abs(width - 480) < 0.5 && Math.Abs(height - 340) < 0.5);
        if (isPreviousCanonicalSize)
            return new WindowSize(DefaultWidth, DefaultHeight);

        return new WindowSize(
            Math.Max(width, MinimumWidth),
            Math.Max(height, MinimumHeight));
    }
}
