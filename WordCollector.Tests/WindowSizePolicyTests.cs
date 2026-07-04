using WordCollector.Helpers;

namespace WordCollector.Tests;

public class WindowSizePolicyTests
{
    [Fact]
    public void Resolve_MigratesPreviousLargeDefaultToCompactDefault()
    {
        var size = WindowSizePolicy.Resolve(720, 650);

        Assert.Equal(520, size.Width);
        Assert.Equal(400, size.Height);
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
        var size = WindowSizePolicy.Resolve(320, 260);

        Assert.Equal(480, size.Width);
        Assert.Equal(340, size.Height);
    }
}
