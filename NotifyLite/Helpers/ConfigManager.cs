using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NotifyLite.Helpers;

/// <summary>
/// Manages persistent JSON configuration stored in %APPDATA%/NotifyLite/config.json.
/// </summary>
public class ConfigManager
{
    private static readonly string ConfigDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NotifyLite");

    private static readonly string ConfigPath = Path.Combine(ConfigDir, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>Current application settings.</summary>
    public AppConfig Config { get; private set; } = new();

    /// <summary>Fired when config is saved so the UI can refresh.</summary>
    public event EventHandler? ConfigChanged;

    /// <summary>Load config from disk, creating defaults if missing.</summary>
    public void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions) ?? new AppConfig();
            }
            else
            {
                Config = new AppConfig();
                Save();
            }
        }
        catch
        {
            Config = new AppConfig();
        }
    }

    /// <summary>Persist current config to disk and notify listeners.</summary>
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(ConfigDir);
            var json = JsonSerializer.Serialize(Config, JsonOptions);
            File.WriteAllText(ConfigPath, json);
            ConfigChanged?.Invoke(this, EventArgs.Empty);
        }
        catch { }
    }

    /// <summary>Get the config directory path.</summary>
    public static string GetConfigDir() => ConfigDir;
}

/// <summary>
/// Application settings with sensible defaults.
/// </summary>
public class AppConfig
{
    // --- Appearance ---
    public string Theme { get; set; } = "Dark";
    public string FontFamily { get; set; } = "Segoe UI";
    public double TitleFontSize { get; set; } = 12;
    public double BodyFontSize { get; set; } = 11;

    // --- Colors ---
    public string AccentColor { get; set; } = "#6C63FF";
    public string TitleColor { get; set; } = "#E0E0F0";
    public string BodyColor { get; set; } = "#AAAACC";
    public string CardColor { get; set; } = "#1E1E2E";
    public string CardBorderColor { get; set; } = "#2A2A3E";

    // --- Card ---
    public double ToastWidth { get; set; } = 300;
    public double CornerRadius { get; set; } = 8;
    public double CardOpacity { get; set; } = 1.0;
    public double TextOpacity { get; set; } = 1.0;

    // --- Behavior ---
    public double DismissSeconds { get; set; } = 4;
    public int MaxVisibleToasts { get; set; } = 5;
    /// <summary>"BottomRight", "BottomLeft", "TopRight", "TopLeft", or "Custom"</summary>
    public string Position { get; set; } = "BottomRight";
    /// <summary>Custom X/Y for toast position. -1 = use preset position.</summary>
    public double PositionX { get; set; } = -1;
    public double PositionY { get; set; } = -1;

    // --- Floating Icon ---
    public bool ShowFloatingIcon { get; set; } = true;
    /// <summary>Floating icon X/Y screen position. -1 = auto (center-right).</summary>
    public double FloatingIconX { get; set; } = -1;
    public double FloatingIconY { get; set; } = -1;

    // --- Sound ---
    public bool SoundEnabled { get; set; } = true;
    /// <summary>"default" = system sound, or absolute path to .wav file.</summary>
    public string SoundFile { get; set; } = "default";
    /// <summary>Per-app overrides. Key = app display name, Value = "default"/"none"/path-to-.wav</summary>
    public Dictionary<string, string> AppSounds { get; set; } = new();

    // --- System ---
    public bool AutoStart { get; set; } = false;
    public bool Enabled { get; set; } = true;

    // --- Tracked apps (auto-populated as notifications arrive) ---
    public List<string> KnownApps { get; set; } = new();

    // Legacy compat
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double FontSize { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public double Opacity { get; set; }
}
