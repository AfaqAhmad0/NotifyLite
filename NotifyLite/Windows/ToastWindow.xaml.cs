using NotifyLite.Helpers;
using NotifyLite.Models;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace NotifyLite.Windows;

/// <summary>
/// Animated toast notification card with fully configurable colors, fonts, and opacity.
/// Hidden from Alt+Tab and Win+Tab task switcher.
/// </summary>
public partial class ToastWindow : Window
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.HideFromTaskSwitcher(this);
    }

    private readonly DispatcherTimer _dismissTimer;
    private readonly DispatcherTimer _timerBarUpdater;
    private readonly double _dismissSeconds;
    private readonly string? _appUserModelId;
    private DateTime _showTime;
    private bool _isClosing;

    public event EventHandler? ToastDismissed;
    public InterceptedNotification? NotificationData { get; private set; }

    public ToastWindow(InterceptedNotification data, Helpers.AppConfig config)
    {
        InitializeComponent();

        _dismissSeconds = config.DismissSeconds;
        _appUserModelId = data.AppUserModelId;
        NotificationData = data;

        // Content
        AppNameText.Text = data.AppName;
        TitleText.Text = data.Title;
        BodyText.Text = data.Body;
        TimestampText.Text = FormatTimestamp(data.Timestamp);
        if (string.IsNullOrWhiteSpace(data.Body)) BodyText.Visibility = Visibility.Collapsed;
        if (string.IsNullOrWhiteSpace(data.Title)) TitleText.Visibility = Visibility.Collapsed;

        ApplyConfig(config);

        _dismissTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(_dismissSeconds) };
        _dismissTimer.Tick += (_, _) => { _dismissTimer.Stop(); _timerBarUpdater.Stop(); DismissWithAnimation(); };
        _timerBarUpdater = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(30) };
        _timerBarUpdater.Tick += UpdateTimerBar;
    }

    private void ApplyConfig(Helpers.AppConfig config)
    {
        // Size
        this.Width = config.ToastWidth;

        // Corner radius
        var radius = new CornerRadius(config.CornerRadius);
        CardBorder.CornerRadius = radius;
        BackgroundBorder.CornerRadius = radius;

        // Card opacity (background only)
        BackgroundBorder.Opacity = config.CardOpacity;

        // Font
        try
        {
            var fontFamily = new FontFamily(config.FontFamily);
            TitleText.FontFamily = fontFamily;
            BodyText.FontFamily = fontFamily;
            AppNameText.FontFamily = fontFamily;
            TimestampText.FontFamily = fontFamily;
        }
        catch { }

        TitleText.FontSize = config.TitleFontSize;
        BodyText.FontSize = config.BodyFontSize;

        // Text opacity
        TitleText.Opacity = config.TextOpacity;
        BodyText.Opacity = config.TextOpacity;
        AppNameText.Opacity = config.TextOpacity;
        TimestampText.Opacity = config.TextOpacity;

        // Colors from config (hex)
        TrySetBrush(BackgroundBorder, "Background", config.CardColor);
        TrySetBrush(BackgroundBorder, "BorderBrush", config.CardBorderColor);
        TrySetForeground(TitleText, config.TitleColor);
        TrySetForeground(BodyText, config.BodyColor);

        // App name and timestamp derive from body color with more transparency
        try
        {
            var bodyColor = (Color)ColorConverter.ConvertFromString(config.BodyColor);
            AppNameText.Foreground = new SolidColorBrush(Color.FromArgb(180, bodyColor.R, bodyColor.G, bodyColor.B));
            TimestampText.Foreground = new SolidColorBrush(Color.FromArgb(140, bodyColor.R, bodyColor.G, bodyColor.B));
        }
        catch { }

        // Accent color (timer bar)
        try
        {
            var accentColor = (Color)ColorConverter.ConvertFromString(config.AccentColor);
            TimerBar.Background = new SolidColorBrush(accentColor);
        }
        catch { }

        // Start hidden for slide-in animation
        CardBorder.Opacity = 0;
    }

    private static void TrySetBrush(System.Windows.Controls.Border border, string prop, string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            var brush = new SolidColorBrush(color);
            if (prop == "Background") border.Background = brush;
            else if (prop == "BorderBrush") border.BorderBrush = brush;
        }
        catch { }
    }

    private static void TrySetForeground(System.Windows.Controls.TextBlock tb, string hex)
    {
        try
        {
            var color = (Color)ColorConverter.ConvertFromString(hex);
            tb.Foreground = new SolidColorBrush(color);
        }
        catch { }
    }

    private static string FormatTimestamp(DateTime timestamp)
    {
        var diff = DateTime.Now - timestamp;
        if (diff.TotalSeconds < 5) return "just now";
        if (diff.TotalSeconds < 60) return $"{(int)diff.TotalSeconds}s ago";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        return timestamp.ToString("HH:mm");
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try { ((Storyboard)FindResource("SlideIn")).Begin(this); }
        catch (Exception ex) { Debug.WriteLine($"[Toast] Animation: {ex.Message}"); }
        _showTime = DateTime.Now;
        _dismissTimer.Start();
        _timerBarUpdater.Start();
    }

    private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dismissTimer.Stop();
        _timerBarUpdater.Stop();
        if (!string.IsNullOrEmpty(_appUserModelId))
        {
            try { LaunchAppByUserModelId(_appUserModelId); }
            catch (Exception ex) { Debug.WriteLine($"[Toast] Launch: {ex.Message}"); }
        }
        DismissWithAnimation();
    }

    /// <summary>Close button: dismiss without opening the source app.</summary>
    private void CloseBtn_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        e.Handled = true; // Prevent Window_MouseLeftButtonDown from firing
        _dismissTimer.Stop();
        _timerBarUpdater.Stop();
        DismissWithAnimation();
    }

    private void CloseBtn_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        CloseBtn.Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
    }

    private void CloseBtn_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        CloseBtn.Background = System.Windows.Media.Brushes.Transparent;
    }

    private static void LaunchAppByUserModelId(string aumid)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = $"shell:AppsFolder\\{aumid}",
                UseShellExecute = true
            });
        }
        catch
        {
            try
            {
                var aam = (IApplicationActivationManager)new ApplicationActivationManager();
                aam.ActivateApplication(aumid, null, ActivateOptions.None, out _);
            }
            catch { }
        }
    }

    private void UpdateTimerBar(object? sender, EventArgs e)
    {
        try
        {
            var elapsed = (DateTime.Now - _showTime).TotalSeconds;
            var fraction = Math.Max(0, 1 - elapsed / _dismissSeconds);
            var w = CardBorder.ActualWidth - 24;
            if (w > 0) TimerBar.Width = fraction * w;
        }
        catch { }
    }

    public Func<Point?>? GetTargetIconPoint { get; set; }

    public void DismissWithAnimation()
    {
        if (_isClosing) return;
        _isClosing = true;
        try
        {
            var targetPoint = GetTargetIconPoint?.Invoke();
            if (targetPoint.HasValue)
            {
                // Fly to icon animation
                var sb = new Storyboard();
                sb.Completed += SlideOut_Completed;

                var duration = TimeSpan.FromMilliseconds(350);
                var ease = new CubicEase { EasingMode = EasingMode.EaseIn };

                var xAnim = new DoubleAnimation
                {
                    From = Left,
                    To = targetPoint.Value.X - Width / 2,
                    Duration = duration,
                    EasingFunction = ease
                };

                var yAnim = new DoubleAnimation
                {
                    From = Top,
                    To = targetPoint.Value.Y - ActualHeight / 2,
                    Duration = duration,
                    EasingFunction = ease
                };

                Storyboard.SetTarget(xAnim, this);
                Storyboard.SetTargetProperty(xAnim, new PropertyPath("Left"));
                Storyboard.SetTarget(yAnim, this);
                Storyboard.SetTargetProperty(yAnim, new PropertyPath("Top"));

                var scaleGroup = new TransformGroup();
                scaleGroup.Children.Add(new TranslateTransform { X = 0, Y = 0 });
                var scale = new ScaleTransform(1, 1);
                scaleGroup.Children.Add(scale);
                CardBorder.RenderTransformOrigin = new Point(0.5, 0.5);
                CardBorder.RenderTransform = scaleGroup;

                var scaleAnimX = new DoubleAnimation { To = 0.05, Duration = duration, EasingFunction = ease };
                var scaleAnimY = new DoubleAnimation { To = 0.05, Duration = duration, EasingFunction = ease };

                Storyboard.SetTarget(scaleAnimX, CardBorder);
                Storyboard.SetTargetProperty(scaleAnimX, new PropertyPath("RenderTransform.Children[1].ScaleX"));
                Storyboard.SetTarget(scaleAnimY, CardBorder);
                Storyboard.SetTargetProperty(scaleAnimY, new PropertyPath("RenderTransform.Children[1].ScaleY"));

                var opacityAnim = new DoubleAnimation { To = 0, Duration = duration, EasingFunction = ease };
                Storyboard.SetTarget(opacityAnim, CardBorder);
                Storyboard.SetTargetProperty(opacityAnim, new PropertyPath("Opacity"));

                sb.Children.Add(xAnim);
                sb.Children.Add(yAnim);
                sb.Children.Add(scaleAnimX);
                sb.Children.Add(scaleAnimY);
                sb.Children.Add(opacityAnim);

                sb.Begin();
            }
            else
            {
                ((Storyboard)FindResource("SlideOut")).Begin(this);
            }
        }
        catch { ToastDismissed?.Invoke(this, EventArgs.Empty); Close(); }
    }

    private void SlideOut_Completed(object? sender, EventArgs e)
    {
        ToastDismissed?.Invoke(this, EventArgs.Empty);
        Close();
    }

    #region COM Interop
    [ComImport, Guid("2e941141-7f97-4756-ba1d-9decde894a3d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IApplicationActivationManager
    {
        int ActivateApplication(
            [MarshalAs(UnmanagedType.LPWStr)] string appUserModelId,
            [MarshalAs(UnmanagedType.LPWStr)] string? arguments,
            ActivateOptions options, out uint processId);
    }
    [ComImport, Guid("45BA127D-10A8-46EA-8AB7-56EA9078943C")]
    private class ApplicationActivationManager { }
    [Flags] private enum ActivateOptions { None = 0 }
    #endregion
}
