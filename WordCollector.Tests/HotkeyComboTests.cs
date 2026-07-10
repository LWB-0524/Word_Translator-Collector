using WordCollector.Helpers;
using WordCollector.NativeMethods;

namespace WordCollector.Tests;

public class HotkeyComboTests
{
    [Fact]
    public void TryParse_ParsesModifiersAndKey()
    {
        Assert.True(HotkeyCombo.TryParse("Ctrl+Shift+Space", out var combo));

        Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_SHIFT, combo.Modifiers);
        Assert.Equal((uint)0x20, combo.VirtualKey);
        Assert.Equal("Space", combo.KeyName);
    }

    [Fact]
    public void TryParse_IsCaseInsensitiveAndTrimsWhitespace()
    {
        Assert.True(HotkeyCombo.TryParse("  ctrl + ALT + q ", out var combo));

        Assert.Equal(Win32.MOD_CONTROL | Win32.MOD_ALT, combo.Modifiers);
        Assert.Equal((uint)'Q', combo.VirtualKey);
    }

    [Theory]
    [InlineData("control+alt+del", Win32.MOD_CONTROL | Win32.MOD_ALT, (uint)0x2E)]
    [InlineData("win+esc", Win32.MOD_WIN, (uint)0x1B)]
    [InlineData("alt+return", Win32.MOD_ALT, (uint)0x0D)]
    public void TryParse_ResolvesAliases(string text, uint expectedModifiers, uint expectedVk)
    {
        Assert.True(HotkeyCombo.TryParse(text, out var combo));
        Assert.Equal(expectedModifiers, combo.Modifiers);
        Assert.Equal(expectedVk, combo.VirtualKey);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Space")]          // 缺少修饰键
    [InlineData("Ctrl+Shift")]     // 缺少主键
    [InlineData("Ctrl+Foo")]       // 未知主键
    [InlineData("Ctrl+A+B")]       // 两个主键
    public void TryParse_RejectsInvalidCombos(string? text)
    {
        Assert.False(HotkeyCombo.TryParse(text, out _));
    }

    [Fact]
    public void ToStorageString_UsesCanonicalOrderAndNames()
    {
        // 输入顺序打乱 + 使用别名，输出应规范化。
        Assert.True(HotkeyCombo.TryParse("shift+control+escape", out var combo));
        Assert.Equal("Ctrl+Shift+Esc", combo.ToStorageString());
    }

    [Fact]
    public void DisplayText_SeparatesTokensWithSpaces()
    {
        Assert.True(HotkeyCombo.TryParse("Ctrl+Shift+Q", out var combo));
        Assert.Equal("Ctrl + Shift + Q", combo.DisplayText);
    }

    [Fact]
    public void TryCreate_RejectsUnknownVirtualKey()
    {
        Assert.False(HotkeyCombo.TryCreate(Win32.MOD_CONTROL, 0xFF, out _));
    }

    [Fact]
    public void TryCreate_CanonicalizesKeyName()
    {
        Assert.True(HotkeyCombo.TryCreate(Win32.MOD_CONTROL | Win32.MOD_SHIFT, 0x70, out var combo));
        Assert.Equal("F1", combo.KeyName);
        Assert.Equal("Ctrl+Shift+F1", combo.ToStorageString());
    }
}
