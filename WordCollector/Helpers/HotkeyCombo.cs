using WordCollector.NativeMethods;

namespace WordCollector.Helpers;

/// <summary>
/// 一个全局快捷键组合（修饰键 + 主键），可与存储字符串（如 "Ctrl+Shift+Space"）互相转换。
/// 保持纯逻辑、不依赖 WPF，方便单元测试；数值与 Win32 RegisterHotKey 直接兼容。
/// </summary>
public sealed class HotkeyCombo
{
    public uint Modifiers { get; }
    public uint VirtualKey { get; }
    public string KeyName { get; }

    private HotkeyCombo(uint modifiers, uint virtualKey, string keyName)
    {
        Modifiers = modifiers;
        VirtualKey = virtualKey;
        KeyName = keyName;
    }

    public bool HasModifier => Modifiers != 0;

    // 存储/展示时的规范修饰键顺序。
    private static readonly (string Name, uint Flag)[] ModifierOrder =
    {
        ("Ctrl", Win32.MOD_CONTROL),
        ("Alt", Win32.MOD_ALT),
        ("Shift", Win32.MOD_SHIFT),
        ("Win", Win32.MOD_WIN)
    };

    private static readonly Dictionary<string, uint> ModifierAliases =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = Win32.MOD_CONTROL,
            ["control"] = Win32.MOD_CONTROL,
            ["alt"] = Win32.MOD_ALT,
            ["shift"] = Win32.MOD_SHIFT,
            ["win"] = Win32.MOD_WIN,
            ["windows"] = Win32.MOD_WIN,
            ["super"] = Win32.MOD_WIN
        };

    private static readonly Dictionary<uint, string> CanonicalKeyNames = BuildCanonicalKeyNames();
    private static readonly Dictionary<string, uint> KeyNameToVk = BuildKeyNameLookup();

    public string ToStorageString()
    {
        var parts = ModifierOrder
            .Where(entry => (Modifiers & entry.Flag) != 0)
            .Select(entry => entry.Name)
            .ToList();
        parts.Add(KeyName);
        return string.Join("+", parts);
    }

    /// <summary>用于界面展示，例如 "Ctrl + Shift + Space"。</summary>
    public string DisplayText => ToStorageString().Replace("+", " + ");

    /// <summary>从捕获到的修饰键标志 + 虚拟键码构造组合（供快捷键录制 UI 使用）。</summary>
    public static bool TryCreate(uint modifiers, uint virtualKey, out HotkeyCombo combo)
    {
        combo = null!;
        if (!CanonicalKeyNames.TryGetValue(virtualKey, out var keyName))
            return false;

        combo = new HotkeyCombo(modifiers, virtualKey, keyName);
        return true;
    }

    /// <summary>解析存储字符串，如 "Ctrl+Shift+Space"。要求至少一个修饰键 + 一个已知主键。</summary>
    public static bool TryParse(string? text, out HotkeyCombo combo)
    {
        combo = null!;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        uint modifiers = 0;
        string? keyName = null;

        foreach (var rawToken in text.Split('+', StringSplitOptions.RemoveEmptyEntries))
        {
            var token = rawToken.Trim();
            if (token.Length == 0)
                continue;

            if (ModifierAliases.TryGetValue(token, out var flag))
            {
                modifiers |= flag;
                continue;
            }

            if (keyName != null)
                return false; // 出现了第二个非修饰键 —— 非法组合。

            keyName = token;
        }

        if (keyName == null || modifiers == 0)
            return false;

        if (!KeyNameToVk.TryGetValue(keyName, out var vk))
            return false;

        // 用规范名重建，保证往返稳定（例如 "esc" -> "Esc"）。
        return TryCreate(modifiers, vk, out combo);
    }

    private static Dictionary<uint, string> BuildCanonicalKeyNames()
    {
        var map = new Dictionary<uint, string>();

        for (uint c = 'A'; c <= 'Z'; c++)
            map[c] = ((char)c).ToString();
        for (uint d = '0'; d <= '9'; d++)
            map[d] = ((char)d).ToString();
        for (uint f = 0; f < 24; f++)
            map[0x70 + f] = $"F{f + 1}";

        map[0x20] = "Space";
        map[0x0D] = "Enter";
        map[0x09] = "Tab";
        map[0x1B] = "Esc";
        map[0x08] = "Backspace";
        map[0x2D] = "Insert";
        map[0x2E] = "Delete";
        map[0x24] = "Home";
        map[0x23] = "End";
        map[0x21] = "PageUp";
        map[0x22] = "PageDown";
        map[0x25] = "Left";
        map[0x26] = "Up";
        map[0x27] = "Right";
        map[0x28] = "Down";
        return map;
    }

    private static Dictionary<string, uint> BuildKeyNameLookup()
    {
        var map = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        foreach (var (vk, name) in CanonicalKeyNames)
            map[name] = vk;

        // 一些常见别名 —— 便于解析手工编辑或历史遗留的配置。
        map["Escape"] = 0x1B;
        map["Return"] = 0x0D;
        map["Del"] = 0x2E;
        map["Ins"] = 0x2D;
        map["Back"] = 0x08;
        map["PgUp"] = 0x21;
        map["PgDn"] = 0x22;
        map["PageDn"] = 0x22;
        map["Spacebar"] = 0x20;
        return map;
    }
}
