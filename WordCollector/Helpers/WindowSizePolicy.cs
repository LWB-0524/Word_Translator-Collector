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

    /// <summary>
    /// 把保存的窗口位置限制在虚拟屏幕范围内，保证至少有一角可见、标题栏不会跑到屏幕上方之外
    /// （例如拔掉扩展显示器后恢复位置时）。
    /// </summary>
    public static (double Left, double Top) ClampToScreen(
        double left, double top, double width, double height,
        double screenLeft, double screenTop, double screenWidth, double screenHeight)
    {
        const double minVisible = 48;

        if (double.IsNaN(left) || double.IsInfinity(left)) left = screenLeft;
        if (double.IsNaN(top) || double.IsInfinity(top)) top = screenTop;

        var minLeft = screenLeft + minVisible - width;
        var maxLeft = screenLeft + screenWidth - minVisible;
        var maxTop = screenTop + screenHeight - minVisible;

        // 先套下界再套上界，屏幕比窗口还小时以“留在屏幕内”优先。
        left = Math.Min(Math.Max(left, minLeft), maxLeft);
        top = Math.Min(Math.Max(top, screenTop), maxTop);
        return (left, top);
    }
}
