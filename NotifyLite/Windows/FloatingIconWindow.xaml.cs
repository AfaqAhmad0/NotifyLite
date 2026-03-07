using NotifyLite.Helpers;
using NotifyLite.Managers;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace NotifyLite.Windows;

/// <summary>
/// Small draggable floating icon with notification badge.
/// Click to open the history widget.
/// </summary>
public partial class FloatingIconWindow : Window
{
    private readonly ConfigManager _configManager;
    private readonly NotificationHistoryManager _historyManager;
    private HistoryWidget? _historyWidget;
    private bool _isDragging;
    private System.Windows.Point _dragStart;

    public FloatingIconWindow(ConfigManager configManager, NotificationHistoryManager historyManager)
    {
        InitializeComponent();
        _configManager = configManager;
        _historyManager = historyManager;

        _historyManager.CountChanged += (_, _) => Dispatcher.BeginInvoke(UpdateBadge);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var config = _configManager.Config;

        // Position: use saved or default to center-right
        if (config.FloatingIconX >= 0 && config.FloatingIconY >= 0)
        {
            Left = config.FloatingIconX;
            Top = config.FloatingIconY;
        }
        else
        {
            var workArea = SystemParameters.WorkArea;
            Left = workArea.Right - 60;
            Top = workArea.Height / 2;
        }

        UpdateBadge();
    }

    /// <summary>Update the badge count display.</summary>
    private void UpdateBadge()
    {
        var count = _historyManager.Count;
        if (count > 0)
        {
            BadgeBorder.Visibility = System.Windows.Visibility.Visible;
            BadgeText.Text = count > 99 ? "99+" : count.ToString();
        }
        else
        {
            BadgeBorder.Visibility = System.Windows.Visibility.Collapsed;
        }
    }

    /// <summary>Play a small pulse animation when a notification arrives.</summary>
    public void AnimateNotificationIn()
    {
        try
        {
            Dispatcher.BeginInvoke(() =>
            {
                // Pulse the icon slightly
                var scaleX = new DoubleAnimation(1.0, 1.3, TimeSpan.FromMilliseconds(150));
                var scaleY = new DoubleAnimation(1.0, 1.3, TimeSpan.FromMilliseconds(150));
                scaleX.AutoReverse = true;
                scaleY.AutoReverse = true;

                var transform = new System.Windows.Media.ScaleTransform(1, 1);
                IconBorder.RenderTransformOrigin = new System.Windows.Point(0.5, 0.5);
                IconBorder.RenderTransform = transform;

                transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleXProperty, scaleX);
                transform.BeginAnimation(System.Windows.Media.ScaleTransform.ScaleYProperty, scaleY);

                UpdateBadge();
            });
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[FloatingIcon] Animation error: {ex.Message}");
        }
    }

    /// <summary>Get the screen position of this icon (for toast fly-in).</summary>
    public System.Windows.Point GetScreenCenter()
    {
        return new System.Windows.Point(Left + Width / 2, Top + Height / 2);
    }

    private void Icon_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        _dragStart = e.GetPosition(this);
        CaptureMouse();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (e.LeftButton == MouseButtonState.Pressed && IsMouseCaptured)
        {
            var current = e.GetPosition(this);
            var diff = current - _dragStart;

            if (!_isDragging && (Math.Abs(diff.X) > 3 || Math.Abs(diff.Y) > 3))
            {
                _isDragging = true;
            }

            if (_isDragging)
            {
                Left += diff.X;
                Top += diff.Y;
            }
        }
    }

    private void Icon_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        ReleaseMouseCapture();

        if (_isDragging)
        {
            // Save position after drag
            _configManager.Config.FloatingIconX = Left;
            _configManager.Config.FloatingIconY = Top;
            _configManager.Save();
            _isDragging = false;
        }
        else
        {
            // Click - toggle history widget
            ToggleHistoryWidget();
        }
    }

    private void ToggleHistoryWidget()
    {
        if (_historyWidget != null && _historyWidget.IsVisible)
        {
            _historyWidget.Close();
            _historyWidget = null;
            return;
        }

        _historyWidget = new HistoryWidget(_historyManager, _configManager);
        _historyWidget.Closed += (_, _) => _historyWidget = null;

        var workArea = SystemParameters.WorkArea;

        // Place to the left if icon is on right half of screen, else to the right
        if (Left > workArea.Width / 2)
            _historyWidget.Left = Left - _historyWidget.Width - 10;
        else
            _historyWidget.Left = Left + Width + 10;

        // Align tops
        _historyWidget.Top = Top;

        // Keep vertically within screen bounds (widget MaxHeight is 500)
        if (_historyWidget.Top < workArea.Top + 10)
            _historyWidget.Top = workArea.Top + 10;
        if (_historyWidget.Top + _historyWidget.MaxHeight > workArea.Bottom - 10)
            _historyWidget.Top = workArea.Bottom - _historyWidget.MaxHeight - 10;

        _historyWidget.Show();
    }
}
