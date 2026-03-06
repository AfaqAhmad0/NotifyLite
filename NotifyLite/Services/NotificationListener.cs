using NotifyLite.Models;
using System.Collections.Concurrent;
using System.Diagnostics;
using Windows.UI.Notifications;
using Windows.UI.Notifications.Management;

namespace NotifyLite.Services;

/// <summary>
/// Listens for all Windows notifications via the WinRT UserNotificationListener API.
/// Extracts notification data and raises events for the UI layer.
/// </summary>
public class NotificationListener : IDisposable
{
    private UserNotificationListener? _listener;
    private bool _isListening;
    // ConcurrentDictionary for thread safety (event fires on background thread)
    private readonly ConcurrentDictionary<uint, bool> _knownNotificationIds = new();

    /// <summary>Fired when a new notification is intercepted.</summary>
    public event EventHandler<InterceptedNotification>? NotificationReceived;

    /// <summary>Whether the listener is currently active.</summary>
    public bool IsListening => _isListening;

    /// <summary>
    /// Request notification access from the user and start listening.
    /// Returns true if access was granted.
    /// </summary>
    public async Task<bool> StartAsync()
    {
        try
        {
            _listener = UserNotificationListener.Current;

            // Request user permission — shows a system dialog on first call
            var accessStatus = await _listener.RequestAccessAsync();

            if (accessStatus != UserNotificationListenerAccessStatus.Allowed)
            {
                Debug.WriteLine($"[NotificationListener] Access denied: {accessStatus}");
                return false;
            }

            _isListening = true;

            // Subscribe to notification changes
            _listener.NotificationChanged += OnNotificationChanged;

            Debug.WriteLine("[NotificationListener] Started listening for notifications.");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationListener] Failed to start: {ex.Message}");
            return false;
        }
    }

    /// <summary>Stop listening for notifications.</summary>
    public void Stop()
    {
        _isListening = false;
        if (_listener != null)
        {
            _listener.NotificationChanged -= OnNotificationChanged;
        }
        _knownNotificationIds.Clear();
        Debug.WriteLine("[NotificationListener] Stopped listening.");
    }

    /// <summary>
    /// Called whenever a notification is added, removed, or changed.
    /// This fires on a BACKGROUND THREAD — do not touch UI here.
    /// </summary>
    private async void OnNotificationChanged(UserNotificationListener sender,
        UserNotificationChangedEventArgs args)
    {
        if (!_isListening) return;

        // Only care about new notifications
        if (args.ChangeKind != UserNotificationChangedKind.Added) return;

        try
        {
            // Get all current notifications
            var notifications = await sender.GetNotificationsAsync(
                NotificationKinds.Toast);

            if (notifications == null) return;

            foreach (var notification in notifications)
            {
                try
                {
                    // Skip if we've already processed this one (thread-safe check)
                    if (!_knownNotificationIds.TryAdd(notification.Id, true)) continue;

                    // Skip our OWN notifications to prevent feedback loop
                    // (NotifyLite → Action Center → listener picks it up → infinite loop)
                    var appId = notification.AppInfo?.AppUserModelId ?? "";
                    var appName = notification.AppInfo?.DisplayInfo?.DisplayName ?? "";
                    if (appId.Contains("NotifyLite", StringComparison.OrdinalIgnoreCase) ||
                        appName.Equals("NotifyLite", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var data = ExtractNotificationData(notification);
                    if (data != null)
                    {
                        // Try to remove from Action Center
                        try
                        {
                            sender.RemoveNotification(notification.Id);
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine($"[NotificationListener] Failed to remove: {ex.Message}");
                        }

                        // Raise event (handled by App.xaml.cs on UI thread via Dispatcher)
                        NotificationReceived?.Invoke(this, data);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[NotificationListener] Error processing notification {notification.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationListener] Error in OnNotificationChanged: {ex.Message}");
        }
    }

    /// <summary>
    /// Extract useful data from a UserNotification object.
    /// </summary>
    private static InterceptedNotification? ExtractNotificationData(UserNotification notification)
    {
        try
        {
            var toast = notification.Notification;
            var binding = toast?.Visual?.GetBinding(KnownNotificationBindings.ToastGeneric);

            if (binding == null) return null;

            var textElements = binding.GetTextElements();
            var texts = textElements?.Select(t => t.Text).ToList() ?? new List<string>();

            var data = new InterceptedNotification
            {
                AppName = notification.AppInfo?.DisplayInfo?.DisplayName ?? "Unknown App",
                Title = texts.Count > 0 ? texts[0] : string.Empty,
                Body = texts.Count > 1 ? string.Join("\n", texts.Skip(1)) : string.Empty,
                Timestamp = notification.CreationTime.LocalDateTime,
                AppUserModelId = notification.AppInfo?.AppUserModelId
            };

            // Skip empty notifications
            if (string.IsNullOrWhiteSpace(data.Title) && string.IsNullOrWhiteSpace(data.Body))
                return null;

            return data;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[NotificationListener] Failed to extract data: {ex.Message}");
            return null;
        }
    }

    public void Dispose()
    {
        Stop();
        GC.SuppressFinalize(this);
    }
}
