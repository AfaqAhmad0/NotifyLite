using Hardcodet.Wpf.TaskbarNotification;
using NotifyLite.Helpers;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;

namespace NotifyLite.Services;

/// <summary>
/// Manages the system tray icon and its context menu.
/// Provides Enable/Disable, Theme, Auto-start, and Exit controls.
/// </summary>
public class TrayManager : IDisposable
{
    private TaskbarIcon? _trayIcon;
    private readonly ConfigManager _configManager;

    /// <summary>Fired when the user toggles notification interception on/off.</summary>
    public event EventHandler<bool>? EnabledChanged;

    /// <summary>Fired when the user toggles the theme.</summary>
    public event EventHandler<string>? ThemeChanged;

    /// <summary>Fired when the user clicks Exit.</summary>
    public event EventHandler? ExitRequested;

    public TrayManager(ConfigManager configManager)
    {
        _configManager = configManager;
    }

    /// <summary>Initialize the tray icon with context menu.</summary>
    public void Initialize()
    {
        var contextMenu = CreateContextMenu();

        _trayIcon = new TaskbarIcon
        {
            Icon = CreateDefaultIcon(),
            ToolTipText = "NotifyLite — Custom Notifications",
            ContextMenu = contextMenu
        };
    }

    /// <summary>Build the right-click context menu.</summary>
    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        // Enable / Disable toggle
        var enableItem = new MenuItem
        {
            Header = _configManager.Config.Enabled ? "✅ Enabled" : "⬜ Disabled",
            Tag = "enable"
        };
        enableItem.Click += (s, _) =>
        {
            _configManager.Config.Enabled = !_configManager.Config.Enabled;
            ((MenuItem)s!).Header = _configManager.Config.Enabled ? "✅ Enabled" : "⬜ Disabled";
            _configManager.Save();
            EnabledChanged?.Invoke(this, _configManager.Config.Enabled);
        };
        menu.Items.Add(enableItem);

        menu.Items.Add(new Separator());

        // Theme toggle
        var themeItem = new MenuItem
        {
            Header = _configManager.Config.Theme == "Dark" ? "🌙 Dark Theme" : "☀️ Light Theme",
            Tag = "theme"
        };
        themeItem.Click += (s, _) =>
        {
            _configManager.Config.Theme = _configManager.Config.Theme == "Dark" ? "Light" : "Dark";
            ((MenuItem)s!).Header = _configManager.Config.Theme == "Dark" ? "🌙 Dark Theme" : "☀️ Light Theme";
            _configManager.Save();
            ThemeChanged?.Invoke(this, _configManager.Config.Theme);
        };
        menu.Items.Add(themeItem);

        // Sound toggle
        var soundItem = new MenuItem
        {
            Header = _configManager.Config.SoundEnabled ? "🔔 Sound: ON" : "🔕 Sound: OFF",
            Tag = "sound"
        };
        soundItem.Click += (s, _) =>
        {
            _configManager.Config.SoundEnabled = !_configManager.Config.SoundEnabled;
            ((MenuItem)s!).Header = _configManager.Config.SoundEnabled ? "🔔 Sound: ON" : "🔕 Sound: OFF";
            _configManager.Save();
        };
        menu.Items.Add(soundItem);

        // Auto-start toggle
        var startupItem = new MenuItem
        {
            Header = StartupManager.IsStartupEnabled() ? "🔄 Auto-start: ON" : "🔄 Auto-start: OFF",
            Tag = "startup"
        };
        startupItem.Click += (s, _) =>
        {
            var currentlyEnabled = StartupManager.IsStartupEnabled();
            StartupManager.SetStartup(!currentlyEnabled);
            _configManager.Config.AutoStart = !currentlyEnabled;
            ((MenuItem)s!).Header = !currentlyEnabled ? "🔄 Auto-start: ON" : "🔄 Auto-start: OFF";
            _configManager.Save();
        };
        menu.Items.Add(startupItem);

        menu.Items.Add(new Separator());

        // Settings
        var settingsItem = new MenuItem { Header = "⚙️ Settings" };
        settingsItem.Click += (_, _) =>
        {
            // Only open one settings window at a time
            var existing = Application.Current.Windows.OfType<NotifyLite.Windows.SettingsWindow>().FirstOrDefault();
            if (existing != null)
            {
                existing.Activate();
                return;
            }
            var settingsWindow = new NotifyLite.Windows.SettingsWindow(_configManager);
            settingsWindow.Show();
        };
        menu.Items.Add(settingsItem);

        menu.Items.Add(new Separator());

        // Exit
        var exitItem = new MenuItem { Header = "❌ Exit" };
        exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(exitItem);

        return menu;
    }

    /// <summary>
    /// Create a simple programmatic icon (avoids dependency on an .ico file).
    /// Draws an "N" on a purple gradient background.
    /// </summary>
    private static Icon CreateDefaultIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bitmap);

        // Purple gradient background
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            new Rectangle(0, 0, 32, 32),
            ColorTranslator.FromHtml("#6C63FF"),
            ColorTranslator.FromHtml("#8B5CF6"),
            System.Drawing.Drawing2D.LinearGradientMode.ForwardDiagonal);

        // Rounded rectangle background
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.FillEllipse(brush, 1, 1, 30, 30);

        // Draw "N" letter
        using var font = new Font("Segoe UI", 16, System.Drawing.FontStyle.Bold);
        using var textBrush = new SolidBrush(System.Drawing.Color.White);
        var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };
        g.DrawString("N", font, textBrush, new RectangleF(0, 0, 32, 32), format);

        return Icon.FromHandle(bitmap.GetHicon());
    }

    /// <summary>Update the tooltip to show current status.</summary>
    public void UpdateTooltip(string status)
    {
        if (_trayIcon != null)
        {
            _trayIcon.ToolTipText = $"NotifyLite — {status}";
        }
    }

    public void Dispose()
    {
        _trayIcon?.Dispose();
        GC.SuppressFinalize(this);
    }
}
