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
    private ContextMenu? _contextMenu;
    private string _currentStatus = "Disabled";

    // Menu items for dynamic text refresh
    private MenuItem? _enableItem;
    private MenuItem? _themeItem;
    private MenuItem? _soundItem;
    private MenuItem? _startupItem;
    private MenuItem? _settingsItem;
    private MenuItem? _exitItem;

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
        _contextMenu = CreateContextMenu();

        _trayIcon = new TaskbarIcon
        {
            Icon = CreateDefaultIcon(),
            ToolTipText = LocalizationManager.Instance.GetString("Tray.Title"),
            ContextMenu = _contextMenu
        };

        LocalizationManager.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged(object? sender, string lang)
    {
        RefreshMenuHeaders();
        UpdateTooltip(_currentStatus);
    }

    private void RefreshMenuHeaders()
    {
        if (_enableItem != null)
            _enableItem.Header = LocalizationManager.Instance.GetString(_configManager.Config.Enabled ? "Tray.Menu.Enabled" : "Tray.Menu.Disabled");

        if (_themeItem != null)
            _themeItem.Header = LocalizationManager.Instance.GetString(_configManager.Config.Theme == "Dark" ? "Tray.Menu.DarkTheme" : "Tray.Menu.LightTheme");

        if (_soundItem != null)
            _soundItem.Header = LocalizationManager.Instance.GetString(_configManager.Config.SoundEnabled ? "Tray.Menu.SoundOn" : "Tray.Menu.SoundOff");

        if (_startupItem != null)
            _startupItem.Header = LocalizationManager.Instance.GetString(StartupManager.IsStartupEnabled() ? "Tray.Menu.AutoStartOn" : "Tray.Menu.AutoStartOff");

        if (_settingsItem != null)
            _settingsItem.Header = LocalizationManager.Instance.GetString("Tray.Menu.Settings");

        if (_exitItem != null)
            _exitItem.Header = LocalizationManager.Instance.GetString("Tray.Menu.Exit");
    }

    /// <summary>Build the right-click context menu.</summary>
    private ContextMenu CreateContextMenu()
    {
        var menu = new ContextMenu();

        // Enable / Disable toggle
        _enableItem = new MenuItem
        {
            Header = LocalizationManager.Instance.GetString(_configManager.Config.Enabled ? "Tray.Menu.Enabled" : "Tray.Menu.Disabled"),
            Tag = "enable"
        };
        _enableItem.Click += (s, _) =>
        {
            _configManager.Config.Enabled = !_configManager.Config.Enabled;
            ((MenuItem)s!).Header = LocalizationManager.Instance.GetString(_configManager.Config.Enabled ? "Tray.Menu.Enabled" : "Tray.Menu.Disabled");
            _configManager.Save();
            EnabledChanged?.Invoke(this, _configManager.Config.Enabled);
        };
        menu.Items.Add(_enableItem);

        menu.Items.Add(new Separator());

        // Theme toggle
        _themeItem = new MenuItem
        {
            Header = LocalizationManager.Instance.GetString(_configManager.Config.Theme == "Dark" ? "Tray.Menu.DarkTheme" : "Tray.Menu.LightTheme"),
            Tag = "theme"
        };
        _themeItem.Click += (s, _) =>
        {
            _configManager.Config.Theme = _configManager.Config.Theme == "Dark" ? "Light" : "Dark";
            ((MenuItem)s!).Header = LocalizationManager.Instance.GetString(_configManager.Config.Theme == "Dark" ? "Tray.Menu.DarkTheme" : "Tray.Menu.LightTheme");
            _configManager.Save();
            ThemeChanged?.Invoke(this, _configManager.Config.Theme);
        };
        menu.Items.Add(_themeItem);

        // Sound toggle
        _soundItem = new MenuItem
        {
            Header = LocalizationManager.Instance.GetString(_configManager.Config.SoundEnabled ? "Tray.Menu.SoundOn" : "Tray.Menu.SoundOff"),
            Tag = "sound"
        };
        _soundItem.Click += (s, _) =>
        {
            _configManager.Config.SoundEnabled = !_configManager.Config.SoundEnabled;
            ((MenuItem)s!).Header = LocalizationManager.Instance.GetString(_configManager.Config.SoundEnabled ? "Tray.Menu.SoundOn" : "Tray.Menu.SoundOff");
            _configManager.Save();
        };
        menu.Items.Add(_soundItem);

        // Auto-start toggle
        _startupItem = new MenuItem
        {
            Header = LocalizationManager.Instance.GetString(StartupManager.IsStartupEnabled() ? "Tray.Menu.AutoStartOn" : "Tray.Menu.AutoStartOff"),
            Tag = "startup"
        };
        _startupItem.Click += (s, _) =>
        {
            var currentlyEnabled = StartupManager.IsStartupEnabled();
            StartupManager.SetStartup(!currentlyEnabled);
            _configManager.Config.AutoStart = !currentlyEnabled;
            ((MenuItem)s!).Header = LocalizationManager.Instance.GetString(!currentlyEnabled ? "Tray.Menu.AutoStartOn" : "Tray.Menu.AutoStartOff");
            _configManager.Save();
        };
        menu.Items.Add(_startupItem);

        menu.Items.Add(new Separator());

        // Settings
        _settingsItem = new MenuItem { Header = LocalizationManager.Instance.GetString("Tray.Menu.Settings") };
        _settingsItem.Click += (_, _) =>
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
        menu.Items.Add(_settingsItem);

        menu.Items.Add(new Separator());

        // Exit
        _exitItem = new MenuItem { Header = LocalizationManager.Instance.GetString("Tray.Menu.Exit") };
        _exitItem.Click += (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty);
        menu.Items.Add(_exitItem);

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
        _currentStatus = status;
        if (_trayIcon != null)
        {
            var localizedStatus = status switch
            {
                "Active - Listening" or "Active" => LocalizationManager.Instance.GetString("Tray.Active"),
                "Disabled" => LocalizationManager.Instance.GetString("Tray.Disabled"),
                "Permission Required" or "PermissionRequired" => LocalizationManager.Instance.GetString("Tray.PermissionRequired"),
                _ => LocalizationManager.Instance.GetString(status)
            };
            _trayIcon.ToolTipText = LocalizationManager.Instance.GetString("Tray.TooltipFormat", localizedStatus);
        }
    }

    public void Dispose()
    {
        LocalizationManager.Instance.LanguageChanged -= OnLanguageChanged;
        _trayIcon?.Dispose();
        GC.SuppressFinalize(this);
    }
}
