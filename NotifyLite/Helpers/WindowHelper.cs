using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace NotifyLite.Helpers;

/// <summary>
/// Win32 helper to hide WPF windows from Alt+Tab and Win+Tab task switcher.
/// Uses the WS_EX_TOOLWINDOW extended style which excludes the window
/// from the taskbar and task switcher without affecting functionality.
/// </summary>
public static class WindowHelper
{
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hwnd, int index);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

    /// <summary>
    /// Hide a window from Alt+Tab and Win+Tab task switcher.
    /// Must be called after the window's handle is created (OnSourceInitialized or later).
    /// </summary>
    public static void HideFromTaskSwitcher(Window window)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_TOOLWINDOW);
    }
}
