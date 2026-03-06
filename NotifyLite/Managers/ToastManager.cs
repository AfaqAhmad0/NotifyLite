using NotifyLite.Helpers;
using NotifyLite.Models;
using NotifyLite.Windows;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;

namespace NotifyLite.Managers;

/// <summary>
/// Manages the lifecycle, positioning, and sound playback for toast windows.
/// Supports configurable positions, custom sounds, and per-app sound overrides.
/// </summary>
public class ToastManager
{
    private readonly List<ToastWindow> _activeToasts = new();
    private readonly object _lock = new();
    private readonly ConfigManager _configManager;

    private const double ToastSpacing = 4;
    private const double ScreenMargin = 8;

    public ToastManager(ConfigManager configManager)
    {
        _configManager = configManager;
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
        toast.ToastDismissed += OnToastDismissed;

        lock (_lock)
        {
            _activeToasts.Add(toast);
        }

        try
        {
            PositionToast(toast, config.Position);
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
    private void PositionToast(ToastWindow toast, string position)
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
        var position = _configManager.Config.Position;
        double offset = ScreenMargin;

        lock (_lock)
        {
            foreach (var toast in _activeToasts)
            {
                if (!toast.IsLoaded) continue;

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

                offset += toast.ActualHeight + ToastSpacing;
            }
        }
    }

    private void OnToastDismissed(object? sender, EventArgs e)
    {
        if (sender is ToastWindow toast)
        {
            toast.ToastDismissed -= OnToastDismissed;
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
