using WordCollector.Helpers;

namespace WordCollector.Tests;

public class BehaviorOptionsTests
{
    [Theory]
    [InlineData("hide", "查询后隐藏窗口")]
    [InlineData("clear", "查询后清空输入框")]
    [InlineData("keep", "查询后保留结果")]
    public void ToDisplayCtrlEnterBehavior_MapsStoredCodes(string stored, string expected)
    {
        Assert.Equal(expected, BehaviorOptions.ToDisplayCtrlEnterBehavior(stored));
    }

    [Theory]
    [InlineData("查询后隐藏窗口", "hide")]
    [InlineData("查询后清空输入框", "clear")]
    [InlineData("查询后保留结果", "keep")]
    [InlineData("hide", "hide")]
    public void ToStoredCtrlEnterBehavior_AcceptsDisplayValuesAndStoredCodes(string value, string expected)
    {
        Assert.Equal(expected, BehaviorOptions.ToStoredCtrlEnterBehavior(value));
    }

    [Fact]
    public void ResolvePostQueryAction_AutoHidesAfterNormalQuery()
    {
        var action = BehaviorOptions.ResolvePostQueryAction(false, true, "clear");
        Assert.Equal(PostQueryAction.Hide, action);
    }

    [Fact]
    public void ResolvePostQueryAction_CtrlEnterBehaviorTakesPriority()
    {
        var action = BehaviorOptions.ResolvePostQueryAction(true, true, "clear");
        Assert.Equal(PostQueryAction.Clear, action);
    }

    [Theory]
    [InlineData("auto", "自动")]
    [InlineData("Microsoft David", "Microsoft David")]
    public void ToDisplayVoiceName_MapsAutomaticVoice(string stored, string expected)
    {
        Assert.Equal(expected, BehaviorOptions.ToDisplayVoiceName(stored));
    }

    [Theory]
    [InlineData("自动", "auto")]
    [InlineData("Microsoft David", "Microsoft David")]
    public void ToStoredVoiceName_MapsAutomaticVoice(string display, string expected)
    {
        Assert.Equal(expected, BehaviorOptions.ToStoredVoiceName(display));
    }
}
