using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace NotifyLite.Services;

/// <summary>
/// Suppresses native Windows toast banners by setting the EnableBalloonTips
/// registry value to 0. Notifications still arrive in Action Center and can
/// be read by UserNotificationListener — only the popup banner is hidden.
/// Restores the original value on dispose/exit.
/// </summary>
public class NotificationSuppressor : IDisposable
{
    private const string RegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced";
    private const string ValueName = "EnableBalloonTips";

    private int? _originalValue;
    private bool _isSuppressed;

    /// <summary>Whether native banners are currently suppressed.</summary>
    public bool IsSuppressed => _isSuppressed;

    /// <summary>
    /// Suppress native toast banners by setting EnableBalloonTips = 0.
    /// </summary>
    public void Suppress()
    {
        if (_isSuppressed) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
            if (key != null)
            {
                // Save original value for restoration
                var existing = key.GetValue(ValueName);
                _originalValue = existing as int? ?? (existing != null ? (int?)Convert.ToInt32(existing) : null);

                // Disable balloon tips (suppresses toast banners)
                key.SetValue(ValueName, 0, RegistryValueKind.DWord);
                _isSuppressed = true;

                // Notify Explorer of the change so it takes effect immediately
                RefreshExplorer();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationSuppressor] Failed to suppress: {ex.Message}");
        }
    }

    /// <summary>
    /// Restore the original EnableBalloonTips value.
    /// </summary>
    public void Restore()
    {
        if (!_isSuppressed) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegistryPath, writable: true);
            if (key != null)
            {
                if (_originalValue.HasValue)
                {
                    key.SetValue(ValueName, _originalValue.Value, RegistryValueKind.DWord);
                }
                else
                {
                    // Value didn't exist before — remove it
                    key.DeleteValue(ValueName, throwOnMissingValue: false);
                }

                _isSuppressed = false;
                RefreshExplorer();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[NotificationSuppressor] Failed to restore: {ex.Message}");
        }
    }

    /// <summary>
    /// Broadcast a settings change notification so Explorer picks up the
    /// registry change without requiring a restart.
    /// </summary>
    private static void RefreshExplorer()
    {
        // SHCNE_ASSOCCHANGED | SHCNF_IDLIST — broad notification that refreshes shell
        SHChangeNotify(0x08000000, 0x0000, IntPtr.Zero, IntPtr.Zero);

        // Also broadcast WM_SETTINGCHANGE so the notification subsystem refreshes
        SendMessageTimeout(
            HWND_BROADCAST,
            WM_SETTINGCHANGE,
            IntPtr.Zero,
            "Policy",
            SMTO_ABORTIFHUNG,
            1000,
            out _);
    }

    public void Dispose()
    {
        Restore();
        GC.SuppressFinalize(this);
    }

    ~NotificationSuppressor() => Restore();

    // P/Invoke declarations
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
