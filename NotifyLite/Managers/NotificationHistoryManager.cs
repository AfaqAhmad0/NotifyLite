using NotifyLite.Models;
using System.Collections.ObjectModel;

namespace NotifyLite.Managers;

/// <summary>
/// In-memory store for dismissed notifications, displayed in the history widget.
/// Thread-safe. FIFO with configurable max size.
/// </summary>
public class NotificationHistoryManager
{
    private readonly object _lock = new();
    private const int MaxItems = 50;

    public ObservableCollection<InterceptedNotification> Notifications { get; } = new();

    public int Count
    {
        get { lock (_lock) return Notifications.Count; }
    }

    public event EventHandler? CountChanged;

    /// <summary>Add a notification to history (newest first).</summary>
    public void Add(InterceptedNotification notification)
    {
        lock (_lock)
        {
            Notifications.Insert(0, notification);

            // Trim oldest if over limit
            while (Notifications.Count > MaxItems)
            {
                Notifications.RemoveAt(Notifications.Count - 1);
            }
        }
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Remove a single notification.</summary>
    public void Remove(InterceptedNotification notification)
    {
        lock (_lock)
        {
            Notifications.Remove(notification);
        }
        CountChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Clear all notifications.</summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            Notifications.Clear();
        }
        CountChanged?.Invoke(this, EventArgs.Empty);
    }
}
