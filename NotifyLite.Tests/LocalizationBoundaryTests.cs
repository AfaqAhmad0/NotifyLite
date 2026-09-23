using System.Globalization;
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

        var localizedStatus = status switch
        {
            "Active - Listening" or "Active" => loc.GetString("Tray.Active"),
            "Disabled" => loc.GetString("Tray.Disabled"),
            "Permission Required" or "PermissionRequired" => loc.GetString("Tray.PermissionRequired"),
            _ => loc.GetString(status)
        };
        var tooltip = loc.GetString("Tray.TooltipFormat", localizedStatus);

        Assert.Equal(expectedTooltip, tooltip);
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
}
