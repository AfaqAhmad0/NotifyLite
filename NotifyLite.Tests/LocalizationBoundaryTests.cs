using System.Globalization;
using System.IO;
using System.Text.Json;
using NotifyLite.Helpers;
using NotifyLite.Services;
using Xunit;

namespace NotifyLite.Tests;

public class LocalizationBoundaryTests
{
    [Theory]
    [InlineData("zh-CN", "zh-CN")]
    [InlineData("zh-Hans", "zh-CN")]
    [InlineData("zh-TW", "zh-CN")]
    [InlineData("zh-HK", "zh-CN")]
    [InlineData("zh", "zh-CN")]
    [InlineData("en-US", "en-US")]
    [InlineData("en-GB", "en-US")]
    [InlineData("ja-JP", "en-US")]
    [InlineData("de-DE", "en-US")]
    [InlineData("fr-FR", "en-US")]
    public void ResolveEffectiveLanguage_FollowSystem_ResolvesBasedOnCulture(string cultureName, string expectedLanguage)
    {
        var culture = new CultureInfo(cultureName);
        var effective = LocalizationManager.ResolveEffectiveLanguage("System", culture);
        Assert.Equal(expectedLanguage, effective);
    }

    [Theory]
    [InlineData("zh-CN", "en-US", "zh-CN")]
    [InlineData("zh-CN", "zh-CN", "zh-CN")]
    [InlineData("en-US", "zh-CN", "en-US")]
    [InlineData("en-US", "en-US", "en-US")]
    public void ResolveEffectiveLanguage_ManualOverride_OverridesSystemCulture(string preference, string systemCultureName, string expectedLanguage)
    {
        var culture = new CultureInfo(systemCultureName);
        var effective = LocalizationManager.ResolveEffectiveLanguage(preference, culture);
        Assert.Equal(expectedLanguage, effective);
    }

    [Fact]
    public void AppConfig_LegacyJsonWithoutLanguage_DefaultsToSystem()
    {
        string legacyJson = """
        {
          "theme": "Dark",
          "accentColor": "#6C63FF",
          "dismissSeconds": 4.0
        }
        """;

        var config = JsonSerializer.Deserialize<AppConfig>(legacyJson, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(config);
        Assert.Equal("System", config.Language);
    }

    [Fact]
    public void AppConfig_Persistence_RoundTripsLanguagePreference()
    {
        var config = new AppConfig
        {
            Language = "zh-CN"
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        var deserialized = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        Assert.NotNull(deserialized);
        Assert.Equal("zh-CN", deserialized.Language);
    }

    [Fact]
    public void GetString_ReturnsEnglishForEnUS()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US", systemCulture: new CultureInfo("zh-CN"));

        var text = loc.GetString("Settings.Save");
        Assert.Equal("Save", text);
    }

    [Fact]
    public void GetString_ReturnsChineseForZhCN()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "zh-CN", systemCulture: new CultureInfo("en-US"));

        var text = loc.GetString("Settings.Save");
        Assert.Equal("保存", text);
    }

    [Fact]
    public void GetString_MissingChineseKey_FallsBackToEnglish()
    {
        var enDict = new System.Windows.ResourceDictionary
        {
            ["Test.MissingInChinese"] = "English Only Fallback Value"
        };
        var zhDict = new System.Windows.ResourceDictionary();

        var loc = new LocalizationManager(enDict, zhDict);
        loc.Initialize(preference: "zh-CN", systemCulture: new CultureInfo("en-US"));

        var result = loc.GetString("Test.MissingInChinese");
        Assert.Equal("English Only Fallback Value", result);
    }

    [Fact]
    public void FormatRelativeTime_FormatsAccordingToDisplayLanguage()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        var justNow = DateTime.Now.AddSeconds(-2);
        Assert.Equal("just now", loc.FormatRelativeTime(justNow, isHistory: false));

        var secsAgo = DateTime.Now.AddSeconds(-30);
        Assert.Equal("30s ago", loc.FormatRelativeTime(secsAgo, isHistory: false));

        loc.SetPreference("zh-CN");
        Assert.Equal("刚刚", loc.FormatRelativeTime(justNow, isHistory: false));
        Assert.Equal("30 秒前", loc.FormatRelativeTime(secsAgo, isHistory: false));
    }

    [Fact]
    public void SetPreference_FiresLanguageChanged_WhenEffectiveLanguageChanges()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        string? changedTo = null;
        loc.LanguageChanged += (_, lang) => changedTo = lang;

        loc.SetPreference("zh-CN");

        Assert.Equal("zh-CN", changedTo);
        Assert.Equal("zh-CN", loc.CurrentLanguage);
    }

    [Fact]
    public void SetPreference_DoesNotFireLanguageChanged_WhenEffectiveLanguageUnchanged()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "zh-CN");

        bool fired = false;
        loc.LanguageChanged += (_, _) => fired = true;

        // Switch from zh-CN to another zh alias that resolves to same effective language
        loc.SetPreference("zh");

        Assert.False(fired);
    }

    [Fact]
    public void GetString_WithFormatArgs_FormatsCorrectly()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        var text = loc.GetString("Time.SecondsAgo", 15);
        Assert.Equal("15s ago", text);

        loc.SetPreference("zh-CN");
        var textZh = loc.GetString("Time.SecondsAgo", 15);
        Assert.Equal("15 秒前", textZh);
    }

    [Fact]
    public void GetString_CompletelyMissingKey_ReturnsKey()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        var result = loc.GetString("NonExistent.Key.123");
        Assert.Equal("NonExistent.Key.123", result);
    }

    [Theory]
    [InlineData("en-US", "NotifyLite - Permission Required", "Settings > Privacy > Notifications")]
    [InlineData("zh-CN", "NotifyLite - 需要权限", "设置 > 隐私 > 通知")]
    public void DynamicPrompts_PermissionStrings_LocalizedCorrectly(string lang, string expectedTitle, string expectedSnippet)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        var title = loc.GetString("Permission.Title");
        var msg = loc.GetString("Permission.Message");

        Assert.Equal(expectedTitle, title);
        Assert.Contains(expectedSnippet, msg);
    }

    [Theory]
    [InlineData("en-US", "Active - Listening", "NotifyLite — Active - Listening")]
    [InlineData("zh-CN", "Active - Listening", "NotifyLite — 运行中 - 正在监听")]
    [InlineData("en-US", "Disabled", "NotifyLite — Disabled")]
    [InlineData("zh-CN", "Disabled", "NotifyLite — 已禁用")]
    [InlineData("en-US", "Permission Required", "NotifyLite — Permission Required")]
    [InlineData("zh-CN", "Permission Required", "NotifyLite — 需要通知权限")]
    public void TrayTooltips_LocalizedCorrectly(string lang, string status, string expectedTooltip)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        var tooltip = TrayManager.FormatTooltip(status, loc);

        Assert.Equal(expectedTooltip, tooltip);
    }

    [Theory]
    [InlineData("en-US", "Tray.Menu.Enabled", "✅ Enabled")]
    [InlineData("zh-CN", "Tray.Menu.Enabled", "✅ 已启用")]
    [InlineData("en-US", "Tray.Menu.Disabled", "⬜ Disabled")]
    [InlineData("zh-CN", "Tray.Menu.Disabled", "⬜ 已禁用")]
    [InlineData("en-US", "Tray.Menu.DarkTheme", "🌙 Dark Theme")]
    [InlineData("zh-CN", "Tray.Menu.DarkTheme", "🌙 深色模式")]
    [InlineData("en-US", "Tray.Menu.LightTheme", "☀️ Light Theme")]
    [InlineData("zh-CN", "Tray.Menu.LightTheme", "☀️ 浅色模式")]
    [InlineData("en-US", "Tray.Menu.SoundOn", "🔔 Sound: ON")]
    [InlineData("zh-CN", "Tray.Menu.SoundOn", "🔔 提示音: 开启")]
    [InlineData("en-US", "Tray.Menu.SoundOff", "🔕 Sound: OFF")]
    [InlineData("zh-CN", "Tray.Menu.SoundOff", "🔕 提示音: 关闭")]
    [InlineData("en-US", "Tray.Menu.AutoStartOn", "🔄 Auto-start: ON")]
    [InlineData("zh-CN", "Tray.Menu.AutoStartOn", "🔄 开机自启: 开启")]
    [InlineData("en-US", "Tray.Menu.AutoStartOff", "🔄 Auto-start: OFF")]
    [InlineData("zh-CN", "Tray.Menu.AutoStartOff", "🔄 开机自启: 关闭")]
    [InlineData("en-US", "Tray.Menu.Settings", "⚙️ Settings")]
    [InlineData("zh-CN", "Tray.Menu.Settings", "⚙️ 设置")]
    [InlineData("en-US", "Tray.Menu.Exit", "❌ Exit")]
    [InlineData("zh-CN", "Tray.Menu.Exit", "❌ 退出")]
    public void TrayMenus_LocalizedCorrectly(string lang, string key, string expected)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        Assert.Equal(expected, loc.GetString(key));
    }

    [Theory]
    [InlineData("en-US", "Reset", "Reset all settings to defaults?", "Saved ✔", "Save", "Select Notification Sound", "Select App Sound", "Sound Files|*.wav|All Files|*.*")]
    [InlineData("zh-CN", "恢复默认", "确定将所有设置恢复为默认值吗？", "已保存 ✔", "保存", "选择通知提示音", "选择应用提示音", "音频文件|*.wav|所有文件|*.*")]
    public void SettingsPromptsAndDialogs_LocalizedCorrectly(
        string lang,
        string expectedResetTitle,
        string expectedResetMsg,
        string expectedSaved,
        string expectedSave,
        string expectedBrowseTitle,
        string expectedAppBrowseTitle,
        string expectedFilter)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        Assert.Equal(expectedResetTitle, loc.GetString("Settings.ResetConfirm.Title"));
        Assert.Equal(expectedResetMsg, loc.GetString("Settings.ResetConfirm.Message"));
        Assert.Equal(expectedSaved, loc.GetString("Settings.Saved"));
        Assert.Equal(expectedSave, loc.GetString("Settings.Save"));
        Assert.Equal(expectedBrowseTitle, loc.GetString("Settings.Sound.BrowseDialogTitle"));
        Assert.Equal(expectedAppBrowseTitle, loc.GetString("Settings.Sound.AppSoundDialogTitle"));
        Assert.Equal(expectedFilter, loc.GetString("Settings.Sound.Filter"));
    }

    [Fact]
    public void DynamicSwitching_ImmediatelyUpdatesTrayAndPrompts()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        // Verify initial English
        Assert.Equal("NotifyLite — Active - Listening", TrayManager.FormatTooltip("Active - Listening", loc));
        Assert.Equal("Saved ✔", loc.GetString("Settings.Saved"));
        Assert.Equal("Reset all settings to defaults?", loc.GetString("Settings.ResetConfirm.Message"));
        Assert.Equal("Select Notification Sound", loc.GetString("Settings.Sound.BrowseDialogTitle"));
        Assert.Equal("✅ Enabled", loc.GetString("Tray.Menu.Enabled"));

        // Switch to Chinese
        loc.SetPreference("zh-CN");

        Assert.Equal("NotifyLite — 运行中 - 正在监听", TrayManager.FormatTooltip("Active - Listening", loc));
        Assert.Equal("已保存 ✔", loc.GetString("Settings.Saved"));
        Assert.Equal("确定将所有设置恢复为默认值吗？", loc.GetString("Settings.ResetConfirm.Message"));
        Assert.Equal("选择通知提示音", loc.GetString("Settings.Sound.BrowseDialogTitle"));
        Assert.Equal("✅ 已启用", loc.GetString("Tray.Menu.Enabled"));

        // Switch back to English
        loc.SetPreference("en-US");

        Assert.Equal("NotifyLite — Active - Listening", TrayManager.FormatTooltip("Active - Listening", loc));
        Assert.Equal("Saved ✔", loc.GetString("Settings.Saved"));
    }

    [Fact]
    public void AllResourceDictionaries_HaveCompleteKeyParity()
    {
        var enPath = FindResourceFile("Strings.en-US.xaml");
        var zhPath = FindResourceFile("Strings.zh-CN.xaml");

        Assert.True(File.Exists(enPath), $"en-US resource file not found at {enPath}");
        Assert.True(File.Exists(zhPath), $"zh-CN resource file not found at {zhPath}");

        var enXml = new System.Xml.XmlDocument();
        enXml.Load(enPath);
        var zhXml = new System.Xml.XmlDocument();
        zhXml.Load(zhPath);

        var nsManager = new System.Xml.XmlNamespaceManager(enXml.NameTable);
        nsManager.AddNamespace("x", "http://schemas.microsoft.com/winfx/2006/xaml");
        nsManager.AddNamespace("sys", "clr-namespace:System;assembly=mscorlib");

        var enKeys = ExtractKeys(enXml, nsManager);
        var zhKeys = ExtractKeys(zhXml, nsManager);

        Assert.NotEmpty(enKeys);
        Assert.Equal(enKeys, zhKeys);
    }

    private static string FindResourceFile(string fileName)
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "Resources", fileName),
            Path.Combine(baseDir, "..", "..", "..", "..", "NotifyLite", "Resources", fileName),
            Path.Combine(baseDir, "..", "..", "..", "NotifyLite", "Resources", fileName)
        };

        foreach (var candidate in candidates)
        {
            var fullPath = Path.GetFullPath(candidate);
            if (File.Exists(fullPath)) return fullPath;
        }

        return Path.GetFullPath(candidates[1]);
    }

    private static List<string> ExtractKeys(System.Xml.XmlDocument doc, System.Xml.XmlNamespaceManager nsManager)
    {
        return doc.SelectNodes("//sys:String/@x:Key", nsManager)?
            .Cast<System.Xml.XmlAttribute>()
            .Select(a => a.Value)
            .OrderBy(k => k)
            .ToList() ?? new List<string>();
    }

    [Fact]
    public void ThirdPartyNotificationContent_IsNeverAlteredByLocalization()
    {
        var notif = new NotifyLite.Models.InterceptedNotification
        {
            AppName = "WeChat",
            Title = "张三: 晚上一起吃饭吗？",
            Body = "我在老地方等你",
            Timestamp = DateTime.Now
        };

        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        // Third-party notification properties must remain exactly untouched
        Assert.Equal("WeChat", notif.AppName);
        Assert.Equal("张三: 晚上一起吃饭吗？", notif.Title);
        Assert.Equal("我在老地方等你", notif.Body);

        loc.SetPreference("zh-CN");

        Assert.Equal("WeChat", notif.AppName);
        Assert.Equal("张三: 晚上一起吃饭吗？", notif.Title);
        Assert.Equal("我在老地方等你", notif.Body);
    }

    [Theory]
    [InlineData("en-US", "NotifyLite Toast", "Close", "Unknown")]
    [InlineData("zh-CN", "NotifyLite Toast", "关闭", "未知应用")]
    public void ToastDisplaySurfaces_StringKeys_LocalizedCorrectly(string lang, string expectedTitle, string expectedClose, string expectedUnknownApp)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        Assert.Equal(expectedTitle, loc.GetString("Toast.Title"));
        Assert.Equal(expectedClose, loc.GetString("Toast.Close"));
        Assert.Equal(expectedUnknownApp, loc.GetString("Toast.UnknownApp"));
    }

    [Theory]
    [InlineData("en-US", "Notifications", "Clear All", "No notifications", "Unknown", "Dismiss")]
    [InlineData("zh-CN", "通知历史", "清空全部", "暂无通知", "未知应用", "移除")]
    public void HistoryDisplaySurfaces_StringKeys_LocalizedCorrectly(
        string lang,
        string expectedTitle,
        string expectedClearAll,
        string expectedEmpty,
        string expectedUnknownApp,
        string expectedDismiss)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        Assert.Equal(expectedTitle, loc.GetString("History.Title"));
        Assert.Equal(expectedClearAll, loc.GetString("History.ClearAll"));
        Assert.Equal(expectedEmpty, loc.GetString("History.Empty"));
        Assert.Equal(expectedUnknownApp, loc.GetString("History.UnknownApp"));
        Assert.Equal(expectedDismiss, loc.GetString("History.Dismiss"));
    }

    [Theory]
    [InlineData("en-US", "NotifyLite", "NotifyLite — Click to view history")]
    [InlineData("zh-CN", "NotifyLite", "NotifyLite — 点击查看通知历史")]
    public void FloatingIconDisplaySurfaces_StringKeys_LocalizedCorrectly(string lang, string expectedTitle, string expectedTooltip)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        Assert.Equal(expectedTitle, loc.GetString("FloatingIcon.Title"));
        Assert.Equal(expectedTooltip, loc.GetString("FloatingIcon.Tooltip"));
    }

    [Fact]
    public void DisplaySurfaces_DynamicSwitching_SwitchesAllSurfaceKeys()
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: "en-US");

        // Verify initial English
        Assert.Equal("Notifications", loc.GetString("History.Title"));
        Assert.Equal("Clear All", loc.GetString("History.ClearAll"));
        Assert.Equal("No notifications", loc.GetString("History.Empty"));
        Assert.Equal("Close", loc.GetString("Toast.Close"));
        Assert.Equal("NotifyLite — Click to view history", loc.GetString("FloatingIcon.Tooltip"));

        // Switch to Chinese
        loc.SetPreference("zh-CN");
        Assert.Equal("通知历史", loc.GetString("History.Title"));
        Assert.Equal("清空全部", loc.GetString("History.ClearAll"));
        Assert.Equal("暂无通知", loc.GetString("History.Empty"));
        Assert.Equal("关闭", loc.GetString("Toast.Close"));
        Assert.Equal("NotifyLite — 点击查看通知历史", loc.GetString("FloatingIcon.Tooltip"));

        // Switch back to English
        loc.SetPreference("en-US");
        Assert.Equal("Notifications", loc.GetString("History.Title"));
        Assert.Equal("Clear All", loc.GetString("History.ClearAll"));
        Assert.Equal("No notifications", loc.GetString("History.Empty"));
        Assert.Equal("Close", loc.GetString("Toast.Close"));
        Assert.Equal("NotifyLite — Click to view history", loc.GetString("FloatingIcon.Tooltip"));
    }

    [Theory]
    [InlineData("en-US", "Unknown")]
    [InlineData("zh-CN", "未知应用")]
    public void DisplaySurfaces_EmptyOrNullAppName_FallsBackToLocalizedUnknownApp(string lang, string expectedUnknown)
    {
        var loc = new LocalizationManager();
        loc.Initialize(preference: lang);

        var resolvedEmpty = NotifyLite.Windows.ToastWindow.ResolveDisplayAppName("", loc);
        var resolvedNull = NotifyLite.Windows.ToastWindow.ResolveDisplayAppName(null, loc);
        var resolvedCustom = NotifyLite.Windows.ToastWindow.ResolveDisplayAppName("Slack", loc);

        Assert.Equal(expectedUnknown, resolvedEmpty);
        Assert.Equal(expectedUnknown, resolvedNull);
        Assert.Equal("Slack", resolvedCustom);
    }

    [Fact]
    public void DisplaySurfaces_RebootSimulation_LoadsPersistedLanguageAndInitializesSurfaces()
    {
        // 1. Prepare simulated persisted config with zh-CN
        var tempConfigFile = Path.Combine(Path.GetTempPath(), $"notifylite_test_{Guid.NewGuid():N}.json");
        try
        {
            var initialConfig = new AppConfig { Language = "zh-CN" };
            File.WriteAllText(tempConfigFile, JsonSerializer.Serialize(initialConfig));

            // 2. Simulate application startup / reboot: load config and initialize localization
            var loadedConfig = JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(tempConfigFile));
            Assert.NotNull(loadedConfig);
            Assert.Equal("zh-CN", loadedConfig.Language);

            var loc = new LocalizationManager();
            loc.Initialize(loadedConfig.Language);

            // 3. Verify effective language and display surface strings on reboot
            Assert.Equal("zh-CN", loc.CurrentLanguage);
            Assert.Equal("通知历史", loc.GetString("History.Title"));
            Assert.Equal("清空全部", loc.GetString("History.ClearAll"));
            Assert.Equal("暂无通知", loc.GetString("History.Empty"));
            Assert.Equal("关闭", loc.GetString("Toast.Close"));
            Assert.Equal("NotifyLite — 点击查看通知历史", loc.GetString("FloatingIcon.Tooltip"));
        }
        finally
        {
            if (File.Exists(tempConfigFile)) File.Delete(tempConfigFile);
        }
    }
}
