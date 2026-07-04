namespace WordCollector.Helpers;

public readonly record struct WindowSize(double Width, double Height);

public static class WindowSizePolicy
{
    public const double DefaultWidth = 520;
    public const double DefaultHeight = 400;
    public const double MinimumWidth = 480;
    public const double MinimumHeight = 340;

    public static WindowSize Resolve(double width, double height)
    {
        var isPreviousDefault = Math.Abs(width - 720) < 0.5 && Math.Abs(height - 650) < 0.5;
        if (isPreviousDefault)
            return new WindowSize(DefaultWidth, DefaultHeight);

        return new WindowSize(
            Math.Max(width, MinimumWidth),
            Math.Max(height, MinimumHeight));
    }
}
