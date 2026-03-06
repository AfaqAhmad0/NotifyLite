using NotifyLite.Helpers;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfComboBoxItem = System.Windows.Controls.ComboBoxItem;

namespace NotifyLite.Windows;

public partial class SettingsWindow : Window
{
    private readonly ConfigManager _configManager;
    private bool _isLoading = true;

    // Per-app sound data: appName -> (comboBox for mode, textBox for custom path)
    private readonly Dictionary<string, (System.Windows.Controls.ComboBox Combo, TextBox PathBox)> _appSoundControls = new();

    public SettingsWindow(ConfigManager configManager)
    {
        InitializeComponent();
        _configManager = configManager;
        LoadSettings();
        _isLoading = false;
    }

    private void LoadSettings()
    {
        var c = _configManager.Config;

        // Appearance
        ThemeCombo.SelectedIndex = c.Theme == "Dark" ? 0 : 1;
        SelectComboByContent(FontCombo, c.FontFamily);
        TitleSizeSlider.Value = c.TitleFontSize;
        BodySizeSlider.Value = c.BodyFontSize;

        // Colors
        TitleColorBox.Text = c.TitleColor;
        BodyColorBox.Text = c.BodyColor;
        CardColorBox.Text = c.CardColor;
        AccentColorBox.Text = c.AccentColor;
        TryUpdatePreview(TitleColorPreview, c.TitleColor);
        TryUpdatePreview(BodyColorPreview, c.BodyColor);
        TryUpdatePreview(CardColorPreview, c.CardColor);
        TryUpdatePreview(AccentPreview, c.AccentColor);

        // Card
        WidthSlider.Value = c.ToastWidth;
        RadiusSlider.Value = c.CornerRadius;
        CardOpacitySlider.Value = c.CardOpacity;
        TextOpacitySlider.Value = c.TextOpacity;

        // Behavior
        DurationSlider.Value = c.DismissSeconds;
        MaxToastsSlider.Value = c.MaxVisibleToasts;
        SelectComboByTag(PositionCombo, c.Position);

        // Sound
        SoundEnabledCheck.IsChecked = c.SoundEnabled;
        if (c.SoundFile == "default" || string.IsNullOrEmpty(c.SoundFile))
        {
            DefaultSoundRadio.IsChecked = true;
            CustomSoundPanel.Visibility = Visibility.Collapsed;
        }
        else
        {
            CustomSoundRadio.IsChecked = true;
            SoundFileBox.Text = c.SoundFile;
            CustomSoundPanel.Visibility = Visibility.Visible;
        }

        // Per-app sounds
        BuildAppSoundsList();
    }

    /// <summary>Build the per-app sound configuration UI from known apps.</summary>
    private void BuildAppSoundsList()
    {
        AppSoundsPanel.Children.Clear();
        _appSoundControls.Clear();

        var apps = _configManager.Config.KnownApps.Distinct().OrderBy(a => a).ToList();
        if (apps.Count == 0)
        {
            AppSoundsPanel.Children.Add(new TextBlock
            {
                Text = "No apps detected yet. Apps will appear here once notifications arrive.",
                Foreground = new SolidColorBrush(Color.FromRgb(102, 102, 136)),
                FontSize = 11,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 4)
            });
            return;
        }

        foreach (var appName in apps)
        {
            var row = new DockPanel { Margin = new Thickness(0, 3, 0, 3) };

            // App name label
            var label = new TextBlock
            {
                Text = appName,
                Foreground = new SolidColorBrush(Color.FromRgb(204, 204, 221)),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center,
                Width = 130,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            row.Children.Add(label);
            DockPanel.SetDock(label, Dock.Left);

            // Mode combo: Default / Custom / Muted
            var combo = new System.Windows.Controls.ComboBox
            {
                Width = 90,
                Height = 24,
                FontSize = 10,
                Margin = new Thickness(6, 0, 6, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Tag = appName
            };
            combo.Items.Add(new WpfComboBoxItem { Content = "Default", Tag = "default" });
            combo.Items.Add(new WpfComboBoxItem { Content = "Custom", Tag = "custom" });
            combo.Items.Add(new WpfComboBoxItem { Content = "Muted", Tag = "none" });
            DockPanel.SetDock(combo, Dock.Left);
            row.Children.Add(combo);

            // Custom path textbox
            var pathBox = new TextBox
            {
                FontSize = 10,
                Background = new SolidColorBrush(Color.FromRgb(42, 42, 62)),
                Foreground = new SolidColorBrush(Color.FromRgb(224, 224, 240)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(58, 58, 78)),
                Padding = new Thickness(4, 2, 4, 2),
                IsReadOnly = true,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center
            };
            row.Children.Add(pathBox);

            // Browse button (hidden until Custom selected)
            var browseBtn = new Button
            {
                Content = "...",
                Width = 28,
                Height = 24,
                FontSize = 10,
                Visibility = Visibility.Collapsed,
                VerticalAlignment = VerticalAlignment.Center,
                Tag = pathBox
            };
            browseBtn.Click += BrowseAppSound_Click;
            DockPanel.SetDock(browseBtn, Dock.Right);
            row.Children.Insert(row.Children.Count - 1, browseBtn);

            // Wire combo change to show/hide path box
            combo.SelectionChanged += (s, _) =>
            {
                var selected = (combo.SelectedItem as WpfComboBoxItem)?.Tag?.ToString();
                var isCustom = selected == "custom";
                pathBox.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
                browseBtn.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            };

            // Set initial state from config
            if (_configManager.Config.AppSounds.TryGetValue(appName, out var appSound))
            {
                if (appSound.Equals("none", StringComparison.OrdinalIgnoreCase))
                    combo.SelectedIndex = 2; // Muted
                else if (appSound.Equals("default", StringComparison.OrdinalIgnoreCase))
                    combo.SelectedIndex = 0;
                else
                {
                    combo.SelectedIndex = 1; // Custom
                    pathBox.Text = appSound;
                }
            }
            else
            {
                combo.SelectedIndex = 0; // Default
            }

            _appSoundControls[appName] = (combo, pathBox);
            AppSoundsPanel.Children.Add(row);
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var c = _configManager.Config;

        // Appearance
        c.Theme = (ThemeCombo.SelectedItem as WpfComboBoxItem)?.Content?.ToString() ?? "Dark";
        c.FontFamily = (FontCombo.SelectedItem as WpfComboBoxItem)?.Content?.ToString() ?? "Segoe UI";
        c.TitleFontSize = TitleSizeSlider.Value;
        c.BodyFontSize = BodySizeSlider.Value;

        // Colors
        c.TitleColor = TitleColorBox.Text;
        c.BodyColor = BodyColorBox.Text;
        c.CardColor = CardColorBox.Text;
        c.AccentColor = AccentColorBox.Text;

        // Card
        c.ToastWidth = WidthSlider.Value;
        c.CornerRadius = RadiusSlider.Value;
        c.CardOpacity = CardOpacitySlider.Value;
        c.TextOpacity = TextOpacitySlider.Value;

        // Behavior
        c.DismissSeconds = DurationSlider.Value;
        c.MaxVisibleToasts = (int)MaxToastsSlider.Value;
        c.Position = (PositionCombo.SelectedItem as WpfComboBoxItem)?.Tag?.ToString() ?? "BottomRight";

        // Sound
        c.SoundEnabled = SoundEnabledCheck.IsChecked == true;
        c.SoundFile = DefaultSoundRadio.IsChecked == true ? "default" : SoundFileBox.Text;

        // Per-app sounds
        c.AppSounds.Clear();
        foreach (var (appName, (combo, pathBox)) in _appSoundControls)
        {
            var mode = (combo.SelectedItem as WpfComboBoxItem)?.Tag?.ToString() ?? "default";
            if (mode == "none")
                c.AppSounds[appName] = "none";
            else if (mode == "custom" && !string.IsNullOrEmpty(pathBox.Text))
                c.AppSounds[appName] = pathBox.Text;
            // don't store "default" — absence = default
        }

        // Update theme-dependent colors when theme is Dark or Light and colors match defaults
        UpdateThemeColors(c);

        _configManager.Save();
        Close();
    }

    /// <summary>If the user switched themes, update colors to theme defaults unless they customized them.</summary>
    private void UpdateThemeColors(AppConfig c)
    {
        // Only auto-update if colors still match the OTHER theme's defaults
        var darkDefaults = new { Title = "#E0E0F0", Body = "#AAAACC", Card = "#1E1E2E", Border = "#2A2A3E" };
        var lightDefaults = new { Title = "#1E1E28", Body = "#505064", Card = "#FFFFFF", Border = "#E6E6EB" };

        if (c.Theme == "Light")
        {
            if (c.TitleColor == darkDefaults.Title) c.TitleColor = lightDefaults.Title;
            if (c.BodyColor == darkDefaults.Body) c.BodyColor = lightDefaults.Body;
            if (c.CardColor == darkDefaults.Card) c.CardColor = lightDefaults.Card;
            if (c.CardBorderColor == darkDefaults.Border) c.CardBorderColor = lightDefaults.Border;
        }
        else
        {
            if (c.TitleColor == lightDefaults.Title) c.TitleColor = darkDefaults.Title;
            if (c.BodyColor == lightDefaults.Body) c.BodyColor = darkDefaults.Body;
            if (c.CardColor == lightDefaults.Card) c.CardColor = darkDefaults.Card;
            if (c.CardBorderColor == lightDefaults.Border) c.CardBorderColor = darkDefaults.Border;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

    private void Reset_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Reset all settings to defaults?", "Reset",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            var c = _configManager.Config;
            c.Theme = "Dark"; c.FontFamily = "Segoe UI";
            c.TitleFontSize = 12; c.BodyFontSize = 11;
            c.TitleColor = "#E0E0F0"; c.BodyColor = "#AAAACC";
            c.CardColor = "#1E1E2E"; c.CardBorderColor = "#2A2A3E";
            c.AccentColor = "#6C63FF";
            c.ToastWidth = 300; c.CornerRadius = 8;
            c.CardOpacity = 1.0; c.TextOpacity = 1.0;
            c.DismissSeconds = 4; c.MaxVisibleToasts = 5;
            c.Position = "BottomRight";
            c.SoundEnabled = true; c.SoundFile = "default";
            c.AppSounds.Clear();

            _isLoading = true;
            LoadSettings();
            _isLoading = false;
        }
    }

    // --- Theme change auto-updates color defaults ---
    private void ThemeCombo_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;
        var isDark = ThemeCombo.SelectedIndex == 0;
        TitleColorBox.Text = isDark ? "#E0E0F0" : "#1E1E28";
        BodyColorBox.Text = isDark ? "#AAAACC" : "#505064";
        CardColorBox.Text = isDark ? "#1E1E2E" : "#FFFFFF";
    }

    // --- Slider display handlers ---
    private void TitleSizeSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (TitleSizeLabel != null) TitleSizeLabel.Text = $"{(int)e.NewValue}px"; }

    private void BodySizeSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (BodySizeLabel != null) BodySizeLabel.Text = $"{(int)e.NewValue}px"; }

    private void WidthSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (WidthLabel != null) WidthLabel.Text = $"{(int)e.NewValue}px"; }

    private void RadiusSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (RadiusLabel != null) RadiusLabel.Text = $"{(int)e.NewValue}px"; }

    private void CardOpacitySlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (CardOpacityLabel != null) CardOpacityLabel.Text = $"{e.NewValue:P0}"; }

    private void TextOpacitySlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (TextOpacityLabel != null) TextOpacityLabel.Text = $"{e.NewValue:P0}"; }

    private void DurationSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (DurationLabel != null) DurationLabel.Text = $"{e.NewValue:F1}s"; }

    private void MaxToastsSlider_Changed(object s, RoutedPropertyChangedEventArgs<double> e)
    { if (MaxToastsLabel != null) MaxToastsLabel.Text = $"{(int)e.NewValue}"; }

    // --- Color preview handlers ---
    private void TitleColor_Changed(object s, TextChangedEventArgs e)
    { if (!_isLoading) TryUpdatePreview(TitleColorPreview, TitleColorBox.Text); }

    private void BodyColor_Changed(object s, TextChangedEventArgs e)
    { if (!_isLoading) TryUpdatePreview(BodyColorPreview, BodyColorBox.Text); }

    private void CardColor_Changed(object s, TextChangedEventArgs e)
    { if (!_isLoading) TryUpdatePreview(CardColorPreview, CardColorBox.Text); }

    private void AccentColor_Changed(object s, TextChangedEventArgs e)
    { if (!_isLoading) TryUpdatePreview(AccentPreview, AccentColorBox.Text); }

    private static void TryUpdatePreview(Border preview, string hex)
    {
        try
        {
            if (hex.StartsWith("#") && (hex.Length == 7 || hex.Length == 4))
                preview.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }
        catch { }
    }

    // --- Sound handlers ---
    private void SoundType_Changed(object sender, RoutedEventArgs e)
    {
        if (_isLoading || CustomSoundPanel == null) return;
        CustomSoundPanel.Visibility = CustomSoundRadio.IsChecked == true
            ? Visibility.Visible : Visibility.Collapsed;
    }

    private void BrowseSound_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "Sound Files|*.wav|All Files|*.*",
            Title = "Select Notification Sound"
        };
        if (dialog.ShowDialog() == true) SoundFileBox.Text = dialog.FileName;
    }

    private void BrowseAppSound_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TextBox pathBox)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Sound Files|*.wav|All Files|*.*",
                Title = "Select App Sound"
            };
            if (dialog.ShowDialog() == true) pathBox.Text = dialog.FileName;
        }
    }

    // --- Helpers ---
    private static void SelectComboByContent(WpfComboBox combo, string content)
    {
        foreach (WpfComboBoxItem item in combo.Items)
            if (item.Content?.ToString() == content) { combo.SelectedItem = item; return; }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }

    private static void SelectComboByTag(WpfComboBox combo, string tag)
    {
        foreach (WpfComboBoxItem item in combo.Items)
            if (item.Tag?.ToString() == tag) { combo.SelectedItem = item; return; }
        if (combo.Items.Count > 0) combo.SelectedIndex = 0;
    }
}
