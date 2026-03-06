using NotifyLite.Models;
using System.Diagnostics;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace NotifyLite.Services;

/// <summary>
/// Sends notifications to the Windows Action Center so they persist
/// in the notification tray (Win+N side panel) alongside the custom toast.
/// </summary>
public static class ActionCenterManager
{
    private static ToastNotifier? _notifier;

    /// <summary>Initialize the toast notifier for this app's package identity.</summary>
    public static void Initialize()
    {
        try
        {
            _notifier = ToastNotificationManager.CreateToastNotifier();
            Debug.WriteLine("[ActionCenter] Initialized");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ActionCenter] Init failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Send an intercepted notification to the Action Center.
    /// This makes it appear in the Windows notification tray (Win+N).
    /// </summary>
    public static void SendToActionCenter(InterceptedNotification data)
    {
        if (_notifier == null) return;

        try
        {
            // Build toast XML content
            var xml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText04);
            var textNodes = xml.GetElementsByTagName("text");

            // Line 1: App name
            if (textNodes.Length > 0)
                textNodes[0].AppendChild(xml.CreateTextNode(data.AppName));

            // Line 2: Title
            if (textNodes.Length > 1)
                textNodes[1].AppendChild(xml.CreateTextNode(data.Title));

            // Line 3: Body
            if (textNodes.Length > 2)
                textNodes[2].AppendChild(xml.CreateTextNode(data.Body));

            // Suppress the popup since we show our own custom toast
            // The notification will still appear in Action Center
            var toast = new ToastNotification(xml)
            {
                SuppressPopup = true,  // Don't show Windows popup (we have our own)
                Group = "NotifyLite",
                Tag = $"n{DateTime.Now.Ticks % 100000}" // Unique tag (max 16 chars)
            };

            _notifier.Show(toast);
            Debug.WriteLine($"[ActionCenter] Sent: {data.AppName} - {data.Title}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ActionCenter] Error: {ex.Message}");
        }
    }

    /// <summary>Clear all NotifyLite notifications from the Action Center.</summary>
    public static void ClearAll()
    {
        try
        {
            ToastNotificationManager.History.Clear();
        }
        catch { }
    }
}
