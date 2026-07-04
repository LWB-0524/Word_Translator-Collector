using WordCollector.NativeMethods;
using WordCollector.Services;

namespace WordCollector.Tests;

public class HotkeyServiceTests
{
    [Fact]
    public void GlobalHotkeysAvoidUnmodifiedEscapeAndCommonControlShortcuts()
    {
        var definitions = HotkeyService.GlobalHotkeys;

        Assert.Equal(2, definitions.Count);
        Assert.DoesNotContain(definitions, definition => definition.VirtualKey == Win32.VK_ESCAPE);
        Assert.DoesNotContain(definitions, definition => definition.VirtualKey == Win32.VK_L);
        Assert.All(definitions, definition =>
            Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, definition.Modifiers));
    }

    [Fact]
    public void QuickQueryUsesCtrlShiftQ()
    {
        var quickQuery = Assert.Single(
            HotkeyService.GlobalHotkeys,
            definition => definition.Action == GlobalHotkeyAction.QuickQuery);

        Assert.Equal(Win32.VK_Q, quickQuery.VirtualKey);
        Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, quickQuery.Modifiers);
    }
}
