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
}
