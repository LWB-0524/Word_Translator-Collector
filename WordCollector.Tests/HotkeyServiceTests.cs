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
        Assert.DoesNotContain(definitions, definition => definition.DefaultCombo.VirtualKey == Win32.VK_ESCAPE);
        Assert.DoesNotContain(definitions, definition => definition.DefaultCombo.VirtualKey == Win32.VK_L);
        Assert.All(definitions, definition =>
            Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, definition.DefaultCombo.Modifiers));
    }

    [Fact]
    public void QuickQueryUsesCtrlShiftQ()
    {
        var quickQuery = Assert.Single(
            HotkeyService.GlobalHotkeys,
            definition => definition.Action == GlobalHotkeyAction.QuickQuery);

        Assert.Equal((uint)Win32.VK_Q, quickQuery.DefaultCombo.VirtualKey);
        Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, quickQuery.DefaultCombo.Modifiers);
    }

    [Fact]
    public void EveryDefaultHotkeyRoundTripsThroughStorageString()
    {
        Assert.All(HotkeyService.GlobalHotkeys, definition =>
        {
            var storage = definition.DefaultCombo.ToStorageString();
            Assert.True(WordCollector.Helpers.HotkeyCombo.TryParse(storage, out var parsed));
            Assert.Equal(definition.DefaultCombo.Modifiers, parsed.Modifiers);
            Assert.Equal(definition.DefaultCombo.VirtualKey, parsed.VirtualKey);
        });
    }
}
