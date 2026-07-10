using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace WordCollector.Services;

public class TrayService : IDisposable
{
    private Window? _mainWindow;
    private Action? _showReviewAction;
    private Action? _showSettingsAction;
    private Action? _exitAction;
    private readonly SettingsService _settingsService;
    private HwndSource? _hwndSource;
    private bool _iconAdded;
    private System.Drawing.Icon? _trayIcon;

    // Win32 Shell_NotifyIcon
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll")]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, uint uIDNewItem, string lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y,
        int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT { public int X; public int Y; }

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_MODIFY = 0x00000001;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIF_INFO = 0x00000010;
    private const uint NIIF_INFO = 0x00000001;
    private const uint WM_TRAYICON = 0x8001;

    private const uint WM_RBUTTONUP = 0x0205;
    private const uint WM_LBUTTONDBLCLK = 0x0203;

    private const uint MF_STRING = 0x00000000;
    private const uint MF_SEPARATOR = 0x00000800;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_BOTTOMALIGN = 0x0020;
    // 必须带 TPM_RETURNCMD，TrackPopupMenu 才会返回菜单项 ID 而不是 BOOL。
    private const uint TPM_RETURNCMD = 0x0100;

    // Menu item IDs
    private const uint IDM_SHOW = 1001;
    private const uint IDM_REVIEW = 1002;
    private const uint IDM_SETTINGS = 1003;
    private const uint IDM_EXIT = 1004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
    }

    public TrayService(SettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    public void Initialize(Window mainWindow, Action showReview, Action showSettings, Action exitApp)
    {
        _mainWindow = mainWindow;
        _showReviewAction = showReview;
        _showSettingsAction = showSettings;
        _exitAction = exitApp;

        // Hook into the main window's message loop for tray messages
        _mainWindow.SourceInitialized += (s, e) =>
        {
            var handle = new WindowInteropHelper(_mainWindow).Handle;
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource?.AddHook(WndProc);
            AddTrayIcon(handle);
        };
    }

    private void AddTrayIcon(IntPtr hWnd)
    {
        if (_iconAdded) return;

        IntPtr iconHandle = IntPtr.Zero;
        try
        {
            var exePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(exePath))
                _trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
            iconHandle = (_trayIcon ?? System.Drawing.SystemIcons.Application).Handle;
        }
        catch
        {
            // Use zero handle as fallback
        }

        var nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = hWnd,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = iconHandle,
            szTip = "WordCollector"
        };

        Shell_NotifyIcon(NIM_ADD, ref nid);
        _iconAdded = true;
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_TRAYICON)
        {
            uint lp = (uint)lParam.ToInt64();

            if (lp == WM_LBUTTONDBLCLK)
            {
                ShowMainWindow();
                handled = true;
            }
            else if (lp == WM_RBUTTONUP)
            {
                ShowWin32ContextMenu();
                handled = true;
            }
        }
        return IntPtr.Zero;
    }

    private void ShowWin32ContextMenu()
    {
        var menu = CreatePopupMenu();
        AppendMenu(menu, MF_STRING, IDM_SHOW, "显示快速捕捉窗口");
        AppendMenu(menu, MF_STRING, IDM_REVIEW, "打开每日复盘");
        AppendMenu(menu, MF_STRING, IDM_SETTINGS, "打开设置");
        AppendMenu(menu, MF_SEPARATOR, 0, "");
        AppendMenu(menu, MF_STRING, IDM_EXIT, "退出程序");

        GetCursorPos(out var pt);
        SetForegroundWindow(_hwndSource?.Handle ?? IntPtr.Zero);

        uint selected = TrackPopupMenu(menu, TPM_RIGHTBUTTON | TPM_BOTTOMALIGN | TPM_RETURNCMD,
            pt.X, pt.Y, 0, _hwndSource?.Handle ?? IntPtr.Zero, IntPtr.Zero);

        DestroyMenu(menu);

        switch (selected)
        {
            case IDM_SHOW: ShowMainWindow(); break;
            case IDM_REVIEW: _showReviewAction?.Invoke(); break;
            case IDM_SETTINGS: _showSettingsAction?.Invoke(); break;
            case IDM_EXIT: _exitAction?.Invoke(); break;
        }
    }

    public void ShowMainWindow()
    {
        _mainWindow?.Dispatcher.Invoke(() =>
        {
            _mainWindow.Show();
            _mainWindow.WindowState = WindowState.Normal;
            _mainWindow.Activate();
            _mainWindow.Topmost = _settingsService.Load().AlwaysOnTop;
        });
    }

    public void ShowBalloonTip(string title, string message)
    {
        if (_mainWindow == null) return;

        try
        {
            var handle = new WindowInteropHelper(_mainWindow).Handle;
            if (handle == IntPtr.Zero) return;

            var nid = new NOTIFYICONDATA
            {
                cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = handle,
                uID = 1,
                uFlags = NIF_INFO,
                szInfoTitle = title,
                szInfo = message,
                dwInfoFlags = NIIF_INFO,
                uTimeoutOrVersion = 3000
            };

            Shell_NotifyIcon(NIM_MODIFY, ref nid);
        }
        catch
        {
            // Ignore balloon tip errors
        }
    }

    public void Dispose()
    {
        if (_iconAdded && _mainWindow != null)
        {
            try
            {
                var handle = new WindowInteropHelper(_mainWindow).Handle;
                if (handle != IntPtr.Zero)
                {
                    var nid = new NOTIFYICONDATA
                    {
                        cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
                        hWnd = handle,
                        uID = 1
                    };
                    Shell_NotifyIcon(NIM_DELETE, ref nid);
                }
            }
            catch { }
            _iconAdded = false;
        }

        _trayIcon?.Dispose();
        _trayIcon = null;
        _hwndSource?.RemoveHook(WndProc);
        _hwndSource = null;
    }
}
