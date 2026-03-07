using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NotifyLite.Services;

/// <summary>
/// Suppresses native Windows toast banners using multiple strategies:
/// 1. Focus Assist / Quiet Hours (most reliable for modern Windows 10/11)
/// 2. EnableBalloonTips registry (legacy fallback)
/// 3. Fast removal in the listener (removes notification before Windows renders it)
/// </summary>
public class NotificationSuppressor : IDisposable
{
    // --- Strategy 1: Focus Assist (Priority Only mode) ---
    private const string QuietHoursPath = @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings";
    private const string GlobalSettingsPath = @"Software\Microsoft\Windows\CurrentVersion\Notifications\Settings\Windows.SystemToast.SecurityAndMaintenance";

    // --- Strategy 2: Legacy BalloonTips ---
    private const string ExplorerPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string BalloonTipsName = "EnableBalloonTips";

    // --- Strategy 3: Toast Enabled per-app ---
    private const string PushNotifPath = @"Software\Microsoft\Windows\CurrentVersion\PushNotifications";

    private int? _originalBalloonValue;
    private int? _originalToastEnabled;
    private bool _isSuppressed;

    public bool IsSuppressed => _isSuppressed;

    public void Suppress()
    {
        if (_isSuppressed) return;

        try
        {
            // Strategy 1: Disable toast popups globally via ToastEnabled = 0
            // This suppresses the popup banner but notifications still arrive
            // in the listener and Action Center
            using (var key = Registry.CurrentUser.OpenSubKey(PushNotifPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(PushNotifPath))
            {
                if (key != null)
                {
                    var existing = key.GetValue("ToastEnabled");
                    _originalToastEnabled = existing as int? ?? (existing != null ? (int?)Convert.ToInt32(existing) : null);
                    key.SetValue("ToastEnabled", 0, RegistryValueKind.DWord);
                    Debug.WriteLine("[Suppressor] ToastEnabled set to 0");
                }
            }

            // Strategy 2: Legacy balloon tips (covers older notification types)
            using (var key = Registry.CurrentUser.OpenSubKey(ExplorerPath, writable: true))
            {
                if (key != null)
                {
                    var existing = key.GetValue(BalloonTipsName);
                    _originalBalloonValue = existing as int? ?? (existing != null ? (int?)Convert.ToInt32(existing) : null);
                    key.SetValue(BalloonTipsName, 0, RegistryValueKind.DWord);
                }
            }

            _isSuppressed = true;

            // Broadcast setting change so the system picks it up
            RefreshExplorer();

            Debug.WriteLine("[Suppressor] Native toast banners suppressed");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Suppressor] Failed to suppress: {ex.Message}");
        }
    }

    public void Restore()
    {
        if (!_isSuppressed) return;

        try
        {
            // Restore ToastEnabled
            using (var key = Registry.CurrentUser.OpenSubKey(PushNotifPath, writable: true))
            {
                if (key != null)
                {
                    if (_originalToastEnabled.HasValue)
                        key.SetValue("ToastEnabled", _originalToastEnabled.Value, RegistryValueKind.DWord);
                    else
                        key.SetValue("ToastEnabled", 1, RegistryValueKind.DWord); // Default ON
                    Debug.WriteLine("[Suppressor] ToastEnabled restored");
                }
            }

            // Restore BalloonTips
            using (var key = Registry.CurrentUser.OpenSubKey(ExplorerPath, writable: true))
            {
                if (key != null)
                {
                    if (_originalBalloonValue.HasValue)
                        key.SetValue(BalloonTipsName, _originalBalloonValue.Value, RegistryValueKind.DWord);
                    else
                        key.DeleteValue(BalloonTipsName, throwOnMissingValue: false);
                }
            }

            _isSuppressed = false;
            RefreshExplorer();

            Debug.WriteLine("[Suppressor] Native toast banners restored");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Suppressor] Failed to restore: {ex.Message}");
        }
    }

    private static void RefreshExplorer()
    {
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

        SendMessageTimeout(
            HWND_BROADCAST, WM_SETTINGCHANGE,
            IntPtr.Zero, "Policy",
            SMTO_ABORTIFHUNG, 1000, out _);
    }

    public void Dispose()
    {
        Restore();
        GC.SuppressFinalize(this);
    }

    ~NotificationSuppressor() => Restore();

    // P/Invoke
    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);

    private static readonly IntPtr HWND_BROADCAST = new(0xFFFF);
    private const uint WM_SETTINGCHANGE = 0x001A;
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessageTimeout(
        IntPtr hWnd, uint Msg, IntPtr wParam, string lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);
}
