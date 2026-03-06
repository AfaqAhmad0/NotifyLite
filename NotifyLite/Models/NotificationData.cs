namespace NotifyLite.Models;

/// <summary>
/// Represents a captured notification with extracted metadata.
/// Named 'InterceptedNotification' to avoid conflict with Windows.UI.Notifications.NotificationData.
/// </summary>
public class InterceptedNotification
{
    /// <summary>Display name of the app that sent the notification.</summary>
    public string AppName { get; set; } = string.Empty;

    /// <summary>Notification title / headline.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Notification body text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>When the notification was created.</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>Optional path or URI to the app's icon.</summary>
    public string? AppIconPath { get; set; }

    /// <summary>The Application User Model ID — used to launch the source app on click.</summary>
    public string? AppUserModelId { get; set; }
}
