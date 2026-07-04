using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WordCollector.Helpers;

public static class WindowThemeHelper
{
    private const int DwmUseImmersiveDarkMode = 20;

    public static void ApplyNativeTitleBar(Window window, string? themeMode)
    {
        void Apply()
        {
            var handle = new WindowInteropHelper(window).Handle;
            if (handle == IntPtr.Zero) return;
            var enabled = string.Equals(themeMode, "dark", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
            _ = DwmSetWindowAttribute(handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
        }

        if (new WindowInteropHelper(window).Handle == IntPtr.Zero)
            window.SourceInitialized += (_, _) => Apply();
        else
            Apply();
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(
        IntPtr windowHandle,
        int attribute,
        ref int attributeValue,
        int attributeSize);
}
