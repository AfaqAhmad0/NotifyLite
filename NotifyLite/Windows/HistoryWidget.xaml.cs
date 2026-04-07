using NotifyLite.Helpers;
using NotifyLite.Managers;
using NotifyLite.Models;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace NotifyLite.Windows;

/// <summary>
/// Popup notification history widget. Shows past notifications with clear/dismiss.
/// Closes on click-away (Deactivated event).
/// Hidden from Alt+Tab and Win+Tab task switcher.
/// </summary>
public partial class HistoryWidget : Window
{
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        WindowHelper.HideFromTaskSwitcher(this);
    }

    private readonly NotificationHistoryManager _historyManager;
    private readonly ConfigManager _configManager;
    private readonly Window _ownerIcon;
    private bool _isClosing;

    public HistoryWidget(NotificationHistoryManager historyManager, ConfigManager configManager, Window ownerIcon)
    {
        InitializeComponent();
        _historyManager = historyManager;
        _configManager = configManager;
        _ownerIcon = ownerIcon;

        _historyManager.CountChanged += (_, _) => Dispatcher.BeginInvoke(RefreshList);
        SizeChanged += Window_SizeChanged;

        _historyManager.CountChanged += (_, _) => Dispatcher.BeginInvoke(RefreshList);
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        RefreshList();
    }

    private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.HeightChanged && _ownerIcon != null)
        {
            var workArea = SystemParameters.WorkArea;
            var newTop = _ownerIcon.Top;
            
            if (newTop < workArea.Top + 10)
                newTop = workArea.Top + 10;
            if (newTop + ActualHeight > workArea.Bottom - 10)
                newTop = workArea.Bottom - ActualHeight - 10;
                
            Top = newTop;
        }
    }

    private void RefreshList()
    {
        NotificationList.Children.Clear();

        var notifications = _historyManager.Notifications.ToList();

        if (notifications.Count == 0)
        {
            EmptyText.Visibility = System.Windows.Visibility.Visible;
            ClearAllBtn.Visibility = System.Windows.Visibility.Collapsed;
            return;
        }

        EmptyText.Visibility = System.Windows.Visibility.Collapsed;
        ClearAllBtn.Visibility = System.Windows.Visibility.Visible;

        foreach (var notif in notifications)
        {
            var card = CreateNotificationCard(notif);
            NotificationList.Children.Add(card);
        }
    }

    private Border CreateNotificationCard(InterceptedNotification notif)
    {
        var card = new Border
        {
            Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#252538")),
            CornerRadius = new CornerRadius(8),
            Margin = new Thickness(8, 3, 8, 3),
            Padding = new Thickness(10, 8, 8, 8),
            Cursor = Cursors.Hand,
            Tag = notif
        };

        card.MouseEnter += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#2E2E44"));
        card.MouseLeave += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#252538"));
        card.MouseLeftButtonUp += Card_Click;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        // Content
        var content = new StackPanel();

        // App name + time
        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 2) };

        var appName = new TextBlock
        {
            Text = notif.AppName ?? "Unknown",
            FontSize = 10,
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#6C63FF")),
            FontWeight = FontWeights.SemiBold
        };
        DockPanel.SetDock(appName, Dock.Left);
        header.Children.Add(appName);

        var time = new TextBlock
        {
            Text = FormatTime(notif.Timestamp),
            FontSize = 9,
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#555")),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Right
        };
        DockPanel.SetDock(time, Dock.Right);
        header.Children.Add(time);

        content.Children.Add(header);

        // Title
        if (!string.IsNullOrEmpty(notif.Title))
        {
            content.Children.Add(new TextBlock
            {
                Text = notif.Title,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#E0E0F0")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 230
            });
        }

        // Body
        if (!string.IsNullOrEmpty(notif.Body))
        {
            content.Children.Add(new TextBlock
            {
                Text = notif.Body,
                FontSize = 10,
                Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#9999AA")),
                TextTrimming = TextTrimming.CharacterEllipsis,
                TextWrapping = TextWrapping.NoWrap,
                MaxWidth = 230
            });
        }

        Grid.SetColumn(content, 0);
        grid.Children.Add(content);

        // Close button
        var closeBtn = new Border
        {
            Width = 20,
            Height = 20,
            CornerRadius = new CornerRadius(10),
            Background = Brushes.Transparent,
            Cursor = Cursors.Hand,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4, 0, 0, 0),
            Tag = notif
        };
        closeBtn.Child = new TextBlock
        {
            Text = "\u2715",
            FontSize = 10,
            Foreground = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#666")),
            HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        closeBtn.MouseEnter += (s, _) =>
            ((Border)s!).Background = new SolidColorBrush((Color)System.Windows.Media.ColorConverter.ConvertFromString("#3A3A4E"));
        closeBtn.MouseLeave += (s, _) =>
            ((Border)s!).Background = Brushes.Transparent;
        closeBtn.MouseLeftButtonUp += CloseItem_Click;

        Grid.SetColumn(closeBtn, 1);
        grid.Children.Add(closeBtn);

        card.Child = grid;
        return card;
    }

    private void Card_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.Tag is InterceptedNotification notif)
        {
            // Try to launch the app
            if (!string.IsNullOrEmpty(notif.AppUserModelId))
            {
                try
                {
                    var aumid = notif.AppUserModelId;
                    var process = new Process();
                    process.StartInfo.FileName = "explorer.exe";
                    process.StartInfo.Arguments = $"shell:AppsFolder\\{aumid}";
                    process.StartInfo.UseShellExecute = false;
                    process.Start();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[HistoryWidget] Launch failed: {ex.Message}");
                }
            }
        }
    }

    private void CloseItem_Click(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true; // Prevent card click
        if (sender is Border border && border.Tag is InterceptedNotification notif)
        {
            _historyManager.Remove(notif);
        }
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        _historyManager.ClearAll();
    }

    private void UIElement_OnPreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer scv)
        {
            scv.ScrollToVerticalOffset(scv.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!_isClosing)
        {
            _isClosing = true;
            Close();
        }
    }

    private static string FormatTime(DateTime timestamp)
    {
        var diff = DateTime.Now - timestamp;
        if (diff.TotalSeconds < 60) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        return timestamp.ToString("MMM d");
    }
}
