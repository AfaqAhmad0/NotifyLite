using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Markup;

namespace NotifyLite.Services;

/// <summary>
/// Application-level localization boundary.
/// Responsible for:
/// - Three-state language preference (System, zh-CN, en-US)
/// - Windows UI culture resolution (zh* -> zh-CN, others -> en-US)
/// - Current display language state
/// - String resource lookup with English fallback
/// - WPF ResourceDictionary swapping for live UI updates
/// - Relative time formatting
/// </summary>
public class LocalizationManager
{
    public const string LanguageSystem = "System";
    public const string LanguageChinese = "zh-CN";
    public const string LanguageEnglish = "en-US";

    private static LocalizationManager? _instance;
    public static LocalizationManager Instance => _instance ??= new LocalizationManager();

    private ResourceDictionary? _enResourceDict;
    private ResourceDictionary? _zhResourceDict;

    public string Preference { get; private set; } = LanguageSystem;
    public string CurrentLanguage { get; private set; } = LanguageEnglish;

    public event EventHandler<string>? LanguageChanged;

    public LocalizationManager(ResourceDictionary? enDict = null, ResourceDictionary? zhDict = null)
    {
        if (enDict != null || zhDict != null)
        {
            _enResourceDict = enDict;
            _zhResourceDict = zhDict;
        }
        else
        {
            LoadResourceDictionaries();
        }
    }

    /// <summary>
    /// Resolve effective display language from preference and system UI culture.
    /// Follow system: if culture starts with "zh", resolves to zh-CN; otherwise en-US.
    /// Manual override: "zh*" -> zh-CN, "en*" -> en-US.
    /// </summary>
    public static string ResolveEffectiveLanguage(string? preference, CultureInfo? systemCulture = null)
    {
        var culture = systemCulture ?? CultureInfo.CurrentUICulture;

        if (string.IsNullOrWhiteSpace(preference) || preference.Equals(LanguageSystem, StringComparison.OrdinalIgnoreCase))
        {
            if (culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ||
                culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
            {
                return LanguageChinese;
            }
            return LanguageEnglish;
        }

        if (preference.StartsWith("zh", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageChinese;
        }

        if (preference.StartsWith("en", StringComparison.OrdinalIgnoreCase))
        {
            return LanguageEnglish;
        }

        // Unknown preference: fallback to system culture
        return ResolveEffectiveLanguage(LanguageSystem, culture);
    }

    /// <summary>
    /// Initialize localization manager with the given preference and optional culture.
    /// </summary>
    public void Initialize(string? preference, CultureInfo? systemCulture = null)
    {
        Preference = string.IsNullOrWhiteSpace(preference) ? LanguageSystem : preference;
        var newLang = ResolveEffectiveLanguage(Preference, systemCulture);
        CurrentLanguage = newLang;
        ApplyMergedDictionaries();
    }

    /// <summary>
    /// Set a new preference and switch display language immediately if resolved language changed.
    /// </summary>
    public void SetPreference(string preference, CultureInfo? systemCulture = null)
    {
        Preference = string.IsNullOrWhiteSpace(preference) ? LanguageSystem : preference;
        var resolved = ResolveEffectiveLanguage(Preference, systemCulture);
        bool changed = !string.Equals(CurrentLanguage, resolved, StringComparison.OrdinalIgnoreCase);

        CurrentLanguage = resolved;
        ApplyMergedDictionaries();

        if (changed)
        {
            LanguageChanged?.Invoke(this, CurrentLanguage);
        }
    }

    /// <summary>
    /// Look up localized string by key with English fallback.
    /// Supports optional format arguments formatted with the active language culture.
    /// </summary>
    public string GetString(string key, params object[] args)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;

        // 1. Try lookup in active dictionary
        var activeDict = CurrentLanguage.Equals(LanguageChinese, StringComparison.OrdinalIgnoreCase)
            ? _zhResourceDict
            : _enResourceDict;
        string? value = TryLookup(activeDict, key);

        // 2. Fallback to English dictionary if missing in active dictionary
        if (string.IsNullOrEmpty(value) && !CurrentLanguage.Equals(LanguageEnglish, StringComparison.OrdinalIgnoreCase))
        {
            value = TryLookup(_enResourceDict, key);
        }

        // 3. Fallback to key if not found in any dictionary
        value ??= key;

        if (args != null && args.Length > 0)
        {
            try
            {
                var culture = new CultureInfo(CurrentLanguage);
                return string.Format(culture, value, args);
            }
            catch
            {
                return value;
            }
        }

        return value;
    }

    /// <summary>
    /// Centralized relative time formatting using current display language and appropriate date culture.
    /// </summary>
    public string FormatRelativeTime(DateTime timestamp, bool isHistory = false)
    {
        var diff = DateTime.Now - timestamp;
        if (diff.TotalSeconds < (isHistory ? 60 : 5))
            return GetString("Time.JustNow");
        if (diff.TotalSeconds < 60)
            return GetString("Time.SecondsAgo", (int)diff.TotalSeconds);
        if (diff.TotalMinutes < 60)
            return GetString("Time.MinutesAgo", (int)diff.TotalMinutes);
        if (isHistory && diff.TotalHours < 24)
            return GetString("Time.HoursAgo", (int)diff.TotalHours);

        var isZh = CurrentLanguage.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var culture = isZh ? new CultureInfo("zh-CN") : new CultureInfo("en-US");
        var format = isZh ? "M月d日" : "MMM d";

        return isHistory ? timestamp.ToString(format, culture) : timestamp.ToString("HH:mm", culture);
    }

    private static string? TryLookup(ResourceDictionary? dict, string key)
    {
        if (dict != null && dict.Contains(key))
        {
            var val = dict[key]?.ToString();
            if (!string.IsNullOrEmpty(val))
                return val;
        }
        return null;
    }

    private void LoadResourceDictionaries()
    {
        _enResourceDict = TryLoadDictionary(LanguageEnglish);
        _zhResourceDict = TryLoadDictionary(LanguageChinese);
    }

    private static ResourceDictionary? TryLoadDictionary(string language)
    {
        // 1. Try Pack URI if Application is running
        try
        {
            var uri = new Uri($"pack://application:,,,/NotifyLite;component/Resources/Strings.{language}.xaml", UriKind.Absolute);
            return new ResourceDictionary { Source = uri };
        }
        catch
        {
            // Pack URI may not be supported in test runner without active WPF Application
        }

        // 2. Try loading from file path on disk (useful during tests / build)
        try
        {
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var searchPaths = new[]
            {
                Path.Combine(baseDir, "Resources", $"Strings.{language}.xaml"),
                Path.Combine(baseDir, "..", "..", "..", "..", "NotifyLite", "Resources", $"Strings.{language}.xaml"),
                Path.Combine(baseDir, "..", "..", "..", "NotifyLite", "Resources", $"Strings.{language}.xaml")
            };

            foreach (var path in searchPaths)
            {
                var fullPath = Path.GetFullPath(path);
                if (File.Exists(fullPath))
                {
                    using var stream = File.OpenRead(fullPath);
                    if (XamlReader.Load(stream) is ResourceDictionary rd)
                    {
                        return rd;
                    }
                }
            }
        }
        catch
        {
            // Fallback gracefully
        }

        return null;
    }

    /// <summary>
    /// Update Application.Current.Resources.MergedDictionaries to enable {DynamicResource} live switching.
    /// </summary>
    private void ApplyMergedDictionaries()
    {
        if (Application.Current == null) return;

        try
        {
            var merged = Application.Current.Resources.MergedDictionaries;

            // Remove previous localized string dictionaries
            var toRemove = merged.Where(d =>
                d.Source != null && d.Source.OriginalString.Contains("/Strings.") ||
                (d.Contains("Settings.Save"))).ToList();

            foreach (var dict in toRemove)
            {
                merged.Remove(dict);
            }

            // Always add English as base fallback
            if (_enResourceDict != null)
            {
                merged.Add(_enResourceDict);
            }

            // If Chinese is current, add Chinese dictionary on top (checked first by WPF)
            if (CurrentLanguage.Equals(LanguageChinese, StringComparison.OrdinalIgnoreCase) && _zhResourceDict != null)
            {
                merged.Add(_zhResourceDict);
            }
        }
        catch
        {
            // Ignore if in test runner without UI thread
        }
    }
}
