using WordCollector.Helpers;

namespace WordCollector.Tests;

public class WindowSizePolicyTests
{
    [Fact]
    public void Resolve_MigratesPreviousCanonicalSizesToPocketDefault()
    {
        var originalDefault = WindowSizePolicy.Resolve(720, 650);
        var compactDefault = WindowSizePolicy.Resolve(520, 400);
        var compactMinimum = WindowSizePolicy.Resolve(480, 340);

        Assert.Equal(440, originalDefault.Width);
        Assert.Equal(320, originalDefault.Height);
        Assert.Equal(440, compactDefault.Width);
        Assert.Equal(320, compactDefault.Height);
        Assert.Equal(440, compactMinimum.Width);
        Assert.Equal(320, compactMinimum.Height);
    }

    [Fact]
    public void Resolve_PreservesAUserChosenValidSize()
    {
        var size = WindowSizePolicy.Resolve(640, 520);

        Assert.Equal(640, size.Width);
        Assert.Equal(520, size.Height);
    }

    [Fact]
    public void Resolve_ClampsInvalidSmallDimensions()
    {
        var size = WindowSizePolicy.Resolve(320, 240);

        Assert.Equal(400, size.Width);
        Assert.Equal(280, size.Height);
    }

    [Fact]
    public void ClampToScreen_KeepsPositionInsideScreenUnchanged()
    {
        var (left, top) = WindowSizePolicy.ClampToScreen(
            600, 200, 440, 320,
            0, 0, 1920, 1080);

        Assert.Equal(600, left);
        Assert.Equal(200, top);
    }

    [Fact]
    public void ClampToScreen_PullsBackWindowLostBeyondRightEdge()
    {
        // 例如原本在第二台显示器上，拔掉后位置超出了当前虚拟屏幕。
        var (left, top) = WindowSizePolicy.ClampToScreen(
            2500, 3000, 440, 320,
            0, 0, 1920, 1080);

        Assert.True(left <= 1920 - 48);
        Assert.True(top <= 1080 - 48);
    }

    [Fact]
    public void ClampToScreen_PullsBackWindowLostBeyondLeftAndTopEdge()
    {
        var (left, top) = WindowSizePolicy.ClampToScreen(
            -5000, -400, 440, 320,
            0, 0, 1920, 1080);

        Assert.True(left >= 48 - 440);
        Assert.Equal(0, top);
    }

    [Fact]
    public void ClampToScreen_ReplacesNaNWithScreenOrigin()
    {
        var (left, top) = WindowSizePolicy.ClampToScreen(
            double.NaN, double.NaN, 440, 320,
            0, 0, 1920, 1080);

        Assert.Equal(0, left);
        Assert.Equal(0, top);
    }
}
