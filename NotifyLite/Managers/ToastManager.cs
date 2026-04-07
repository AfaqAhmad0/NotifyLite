using NotifyLite.Helpers;
using NotifyLite.Managers;
using NotifyLite.Models;
using NotifyLite.Windows;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;

namespace NotifyLite.Managers;

/// <summary>
/// Manages the lifecycle, positioning, and sound playback for toast windows.
/// Supports configurable positions (presets + custom X/Y), custom sounds, 
/// per-app sound overrides, and notification history integration.
/// </summary>
public class ToastManager
{
    private readonly List<ToastWindow> _activeToasts = new();
    private readonly object _lock = new();
    private readonly ConfigManager _configManager;

    private NotificationHistoryManager? _historyManager;
    private FloatingIconWindow? _floatingIcon;

    private const double ToastSpacing = 4;
    private const double ScreenMargin = 8;

    public ToastManager(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    /// <summary>Set the history manager and floating icon for animation/history features.</summary>
    public void SetHistoryManager(NotificationHistoryManager historyManager, FloatingIconWindow? floatingIcon)
    {
        _historyManager = historyManager;
        SetFloatingIcon(floatingIcon);
    }

    /// <summary>Update the floating icon reference (e.g. when shown/hidden).</summary>
    public void SetFloatingIcon(FloatingIconWindow? icon)
    {
        // Unsubscribe from old icon
        if (_floatingIcon != null)
            _floatingIcon.PositionChanged -= OnFloatingIconMoved;

        _floatingIcon = icon;

        // Subscribe to new icon's position changes for sticky mode
        if (_floatingIcon != null)
            _floatingIcon.PositionChanged += OnFloatingIconMoved;
    }

    /// <summary>When the floating icon is dragged in "FloatingIcon" position mode, reposition all toasts.</summary>
    private void OnFloatingIconMoved(object? sender, EventArgs e)
    {
        if (_configManager.Config.Position == "FloatingIcon")
        {
            Application.Current?.Dispatcher.BeginInvoke(RepositionAllToasts);
        }
    }

    /// <summary>Show a new toast notification. Must be called on the UI thread.</summary>
    public void ShowToast(InterceptedNotification data)
    {
        var config = _configManager.Config;

        // Enforce max visible toasts
        lock (_lock)
        {
            var toRemove = new List<ToastWindow>();
            while (_activeToasts.Count - toRemove.Count >= config.MaxVisibleToasts && _activeToasts.Count > 0)
            {
                var oldest = _activeToasts.FirstOrDefault(t => !toRemove.Contains(t));
                if (oldest == null) break;
                toRemove.Add(oldest);
            }
            foreach (var old in toRemove)
            {
                _activeToasts.Remove(old);
                try { old.DismissWithAnimation(); } catch { }
            }
        }

        // Create toast with full config
        var toast = new ToastWindow(data, config);
        toast.GetTargetIconPoint = () => _floatingIcon?.IsVisible == true ? _floatingIcon.GetScreenCenter() : null;
        toast.ToastDismissed += OnToastDismissed;

        lock (_lock)
        {
            _activeToasts.Add(toast);
        }

        try
        {
            PositionToast(toast, config);
            toast.Show();

            // Play notification sound
            if (config.SoundEnabled)
            {
                PlayNotificationSound(data.AppName, config);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ToastManager] Error showing toast: {ex.Message}");
            lock (_lock) { _activeToasts.Remove(toast); }
        }
    }

    /// <summary>Play the appropriate sound for this notification.</summary>
    private static void PlayNotificationSound(string appName, AppConfig config)
    {
        try
        {
            // Check per-app sound override
            if (config.AppSounds.TryGetValue(appName, out var appSound))
            {
                if (appSound.Equals("none", StringComparison.OrdinalIgnoreCase))
                    return; // Muted for this app

                if (File.Exists(appSound))
                {
                    using var player = new SoundPlayer(appSound);
                    player.Play();
                    return;
                }
            }

            // Default sound
            if (config.SoundFile == "default" || string.IsNullOrEmpty(config.SoundFile))
            {
                SystemSounds.Asterisk.Play();
            }
            else if (File.Exists(config.SoundFile))
            {
                using var player = new SoundPlayer(config.SoundFile);
                player.Play();
            }
            else
            {
                SystemSounds.Asterisk.Play();
            }
        }
        catch
        {
            // Sound playback is non-critical
        }
    }

    /// <summary>Position a toast based on the configured screen position.</summary>
    private void PositionToast(ToastWindow toast, AppConfig config)
    {
        var workArea = SystemParameters.WorkArea;
        double offset = ScreenMargin;

        lock (_lock)
        {
            foreach (var existing in _activeToasts)
            {
                if (existing == toast || !existing.IsLoaded) continue;
                offset += existing.ActualHeight + ToastSpacing;
            }
        }

        double estimatedHeight = 90;
        string position = config.Position;

        // Floating Icon sticky mode
        if (position == "FloatingIcon" && _floatingIcon?.IsVisible == true)
        {
            PositionRelativeToIcon(toast, offset, estimatedHeight, workArea);
            toast.ContentRendered += (_, _) => RepositionAllToasts();
            return;
        }

        // Custom position mode
        if (position == "Custom" && config.PositionX >= 0 && config.PositionY >= 0)
        {
            toast.Left = config.PositionX;
            // Stack upward from the anchor point
            toast.Top = config.PositionY - offset - estimatedHeight + ScreenMargin;
            toast.ContentRendered += (_, _) => RepositionAllToasts();
            return;
        }

        // Preset positions
        switch (position)
        {
            case "TopRight":
                toast.Left = workArea.Right - toast.Width - ScreenMargin;
                toast.Top = workArea.Top + offset;
                break;
            case "TopLeft":
                toast.Left = workArea.Left + ScreenMargin;
                toast.Top = workArea.Top + offset;
                break;
            case "BottomLeft":
                toast.Left = workArea.Left + ScreenMargin;
                toast.Top = workArea.Bottom - offset - estimatedHeight;
                break;
            default: // BottomRight
                toast.Left = workArea.Right - toast.Width - ScreenMargin;
                toast.Top = workArea.Bottom - offset - estimatedHeight;
                break;
        }

        toast.ContentRendered += (_, _) => RepositionAllToasts();
    }

    /// <summary>Recalculate positions for all active toasts.</summary>
    private void RepositionAllToasts()
    {
        var workArea = SystemParameters.WorkArea;
        var config = _configManager.Config;
        string position = config.Position;
        double offset = ScreenMargin;

        lock (_lock)
        {
            foreach (var toast in _activeToasts)
            {
                if (!toast.IsLoaded) continue;

                if (position == "FloatingIcon" && _floatingIcon?.IsVisible == true)
                {
                    PositionRelativeToIcon(toast, offset, toast.ActualHeight, workArea);
                }
                else if (position == "Custom" && config.PositionX >= 0 && config.PositionY >= 0)
                {
                    toast.Left = config.PositionX;
                    toast.Top = config.PositionY - offset - toast.ActualHeight + ScreenMargin;
                }
                else
                {
                    switch (position)
                    {
                        case "TopRight":
                            toast.Left = workArea.Right - toast.Width - ScreenMargin;
                            toast.Top = workArea.Top + offset;
                            break;
                        case "TopLeft":
                            toast.Left = workArea.Left + ScreenMargin;
                            toast.Top = workArea.Top + offset;
                            break;
                        case "BottomLeft":
                            toast.Left = workArea.Left + ScreenMargin;
                            toast.Top = workArea.Bottom - offset - toast.ActualHeight;
                            break;
                        default: // BottomRight
                            toast.Left = workArea.Right - toast.Width - ScreenMargin;
                            toast.Top = workArea.Bottom - offset - toast.ActualHeight;
                            break;
                    }
                }

                offset += toast.ActualHeight + ToastSpacing;
            }
        }
    }

    /// <summary>
    /// Position a toast relative to the floating icon.
    /// Toasts stack upward from the icon and appear to the left or right
    /// depending on the icon's horizontal position on screen.
    /// </summary>
    private void PositionRelativeToIcon(ToastWindow toast, double offset, double toastHeight, System.Windows.Rect workArea)
    {
        var iconCenter = _floatingIcon!.GetScreenCenter();
        var iconRight = _floatingIcon.Left + _floatingIcon.Width;
        var iconLeft = _floatingIcon.Left;
        const double gap = 8;

        // Determine horizontal placement: prefer the side with more space
        if (iconCenter.X > workArea.Width / 2)
        {
            // Icon is on right half → place toasts to the left
            toast.Left = iconLeft - toast.Width - gap;
        }
        else
        {
            // Icon is on left half → place toasts to the right
            toast.Left = iconRight + gap;
        }

        // Clamp horizontal to screen bounds
        if (toast.Left < workArea.Left + ScreenMargin)
            toast.Left = workArea.Left + ScreenMargin;
        if (toast.Left + toast.Width > workArea.Right - ScreenMargin)
            toast.Left = workArea.Right - toast.Width - ScreenMargin;

        // Stack upward from the icon's vertical center
        toast.Top = iconCenter.Y - offset - toastHeight;

        // If stacking upward goes off-screen, stack downward instead
        if (toast.Top < workArea.Top + ScreenMargin)
        {
            toast.Top = iconCenter.Y + offset + gap;
        }
    }

    private void OnToastDismissed(object? sender, EventArgs e)
    {
        if (sender is ToastWindow toast)
        {
            toast.ToastDismissed -= OnToastDismissed;

            // Add to notification history
            if (toast.NotificationData != null && _historyManager != null)
            {
                _historyManager.Add(toast.NotificationData);
                _floatingIcon?.AnimateNotificationIn();
            }

            lock (_lock) { _activeToasts.Remove(toast); }
            Application.Current?.Dispatcher.BeginInvoke(RepositionAllToasts);
        }
    }

    public void DismissAll()
    {
        List<ToastWindow> toasts;
        lock (_lock) { toasts = new List<ToastWindow>(_activeToasts); }
        foreach (var toast in toasts)
        {
            try { toast.DismissWithAnimation(); } catch { }
        }
    }
}
