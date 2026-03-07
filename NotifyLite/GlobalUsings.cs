// Resolve WinRT/WinForms vs WPF namespace conflicts.
// net8.0-windows10.0.* + WinForms (for NotifyIcon) bring in conflicting types.
// These global aliases force the WPF versions project-wide.

global using Application = System.Windows.Application;
global using MessageBox = System.Windows.MessageBox;
global using MessageBoxButton = System.Windows.MessageBoxButton;
global using MessageBoxImage = System.Windows.MessageBoxImage;
global using MessageBoxResult = System.Windows.MessageBoxResult;
global using Color = System.Windows.Media.Color;
global using SolidColorBrush = System.Windows.Media.SolidColorBrush;
global using ColorConverter = System.Windows.Media.ColorConverter;
global using FontFamily = System.Windows.Media.FontFamily;
global using Visibility = System.Windows.Visibility;
global using Window = System.Windows.Window;
global using Point = System.Windows.Point;
global using TextBox = System.Windows.Controls.TextBox;
global using ComboBox = System.Windows.Controls.ComboBox;
global using Button = System.Windows.Controls.Button;
global using MouseEventArgs = System.Windows.Input.MouseEventArgs;
global using Brushes = System.Windows.Media.Brushes;
global using Cursors = System.Windows.Input.Cursors;
