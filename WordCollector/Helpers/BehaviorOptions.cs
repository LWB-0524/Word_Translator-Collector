namespace WordCollector.Helpers;

public enum PostQueryAction
{
    None,
    Hide,
    Clear
}

public static class BehaviorOptions
{
    public const string HideDisplayText = "查询后隐藏窗口";
    public const string ClearDisplayText = "查询后清空输入框";
    public const string KeepDisplayText = "查询后保留结果";

    public static IReadOnlyList<string> CtrlEnterBehaviors { get; } =
        new[] { HideDisplayText, ClearDisplayText, KeepDisplayText };

    public static string ToDisplayCtrlEnterBehavior(string? storedBehavior) =>
        storedBehavior?.Trim().ToLowerInvariant() switch
        {
            "hide" => HideDisplayText,
            "keep" => KeepDisplayText,
            _ => ClearDisplayText
        };

    public static string ToStoredCtrlEnterBehavior(string? behavior) =>
        behavior?.Trim() switch
        {
            HideDisplayText or "hide" => "hide",
            KeepDisplayText or "keep" => "keep",
            _ => "clear"
        };

    public static string ToDisplayVoiceName(string? storedVoiceName) =>
        string.IsNullOrWhiteSpace(storedVoiceName) || storedVoiceName == "auto" ? "自动" : storedVoiceName;

    public static string ToStoredVoiceName(string? displayVoiceName) =>
        string.IsNullOrWhiteSpace(displayVoiceName) || displayVoiceName == "自动" ? "auto" : displayVoiceName;

    public static PostQueryAction ResolvePostQueryAction(
        bool isCtrlEnter, bool autoHideAfterQuery, string? ctrlEnterBehavior)
    {
        if (!isCtrlEnter)
            return autoHideAfterQuery ? PostQueryAction.Hide : PostQueryAction.None;

        return ToStoredCtrlEnterBehavior(ctrlEnterBehavior) switch
        {
            "hide" => PostQueryAction.Hide,
            "clear" => PostQueryAction.Clear,
            _ => PostQueryAction.None
        };
    }
}
