using NotifyLite.Helpers;
using NotifyLite.Managers;
using NotifyLite.Services;
using System.Diagnostics;
using System.Windows;

namespace NotifyLite;

/// <summary>
/// Application entry point. Runs as a tray-only app with no main window.
/// Orchestrates: notification listening → custom toast rendering → tray management.
/// </summary>
public partial class App : Application
{
    private ConfigManager _configManager = null!;
    private NotificationSuppressor _suppressor = null!;
    private NotificationListener _listener = null!;
    private ToastManager _toastManager = null!;
    private TrayManager _trayManager = null!;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Global exception handlers to prevent crashes
        DispatcherUnhandledException += (_, args) =>
        {
            Debug.WriteLine($"[App] Unhandled UI exception: {args.Exception.Message}");
            Debug.WriteLine(args.Exception.StackTrace);
            args.Handled = true; // Prevent crash
        };
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            Debug.WriteLine($"[App] Unhandled domain exception: {(args.ExceptionObject as Exception)?.Message}");
        };
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            Debug.WriteLine($"[App] Unobserved task exception: {args.Exception.Message}");
            args.SetObserved(); // Prevent crash
        };
        // 1. Load configuration
        _configManager = new ConfigManager();
        _configManager.Load();

        // 2. Package identity is registered externally via Scripts\register-package.ps1
        //    If not registered, the notification listener will show a permission dialog.

        // 3. Initialize toast manager (handles custom UI rendering)
        _toastManager = new ToastManager(_configManager);

        // 3b. Initialize Action Center integration
        ActionCenterManager.Initialize();

        // 4. Initialize notification suppressor (hides native banners)
        _suppressor = new NotificationSuppressor();

        // 5. Initialize notification listener
        _listener = new NotificationListener();
        _listener.NotificationReceived += OnNotificationReceived;

        // 6. Initialize tray icon with menu
        _trayManager = new TrayManager(_configManager);
        _trayManager.Initialize();
        _trayManager.EnabledChanged += OnEnabledChanged;
        _trayManager.ExitRequested += (_, _) => Shutdown();

        // 7. Start intercepting if enabled
        if (_configManager.Config.Enabled)
        {
            await StartInterception();
        }
        else
        {
            _trayManager.UpdateTooltip("Disabled");
        }
    }

    /// <summary>Start listening for notifications and suppress native banners.</summary>
    private async Task StartInterception()
    {
        // Suppress native toast banners first
        _suppressor.Suppress();

        // Start notification listener
        var accessGranted = await _listener.StartAsync();

        if (accessGranted)
        {
            _trayManager.UpdateTooltip("Active — Listening");
            Debug.WriteLine("[App] Notification interception started.");
        }
        else
        {
            _trayManager.UpdateTooltip("Permission Required");
            Debug.WriteLine("[App] Notification access was not granted.");

            // Show a message to the user
            MessageBox.Show(
                "NotifyLite needs permission to access your notifications.\n\n" +
                "Please grant notification access in Windows Settings:\n" +
                "Settings → Privacy → Notifications",
                "NotifyLite — Permission Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    /// <summary>Stop listening and restore native banners.</summary>
    private void StopInterception()
    {
        _listener.Stop();
        _suppressor.Restore();
        _trayManager.UpdateTooltip("Disabled");
        _toastManager.DismissAll();
        Debug.WriteLine("[App] Notification interception stopped.");
    }

    /// <summary>Handle enable/disable toggle from tray menu.</summary>
    private async void OnEnabledChanged(object? sender, bool enabled)
    {
        if (enabled)
        {
            await StartInterception();
        }
        else
        {
            StopInterception();
        }
    }

    private void OnNotificationReceived(object? sender, Models.InterceptedNotification data)
    {
        // Track known apps for Settings UI
        if (!string.IsNullOrEmpty(data.AppName) &&
            !_configManager.Config.KnownApps.Contains(data.AppName))
        {
            _configManager.Config.KnownApps.Add(data.AppName);
            _configManager.Save();
        }

        // Dispatch to UI thread with error handling
        Dispatcher.BeginInvoke(() =>
        {
            try
            {
                _toastManager.ShowToast(data);
                ActionCenterManager.SendToActionCenter(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[App] Error showing toast: {ex.Message}");
            }
        });
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Clean up: restore native banners
        _suppressor?.Dispose();
        _listener?.Dispose();
        _trayManager?.Dispose();

        base.OnExit(e);
    }
}
