# Launch Drafts

## 1. Reddit (r/Windows11, r/Windows10, r/opensource)
**Title:** I built an open source replacement for Windows Notifications — customizable toasts, history widget, and per-app muting!

**Body:**
Hey everyone! 

I've always been frustrated by the default Windows notification banners. They're rigid, you can't customize how they look, and if you miss one, you have to dig into the cluttered Action Center to find it.

So I built **NotifyLite**. It's an open source Windows utility that completely intercepts your system notifications and replaces them with beautiful, customizable toast cards. 

**Features:**
*   🎨 **Full Customization:** Change colors, fonts, corner radius, and opacity.
*   🫧 **History Widget:** A floating badge that keeps track of unread alerts. Click it to scroll through your history.
*   📍 **Custom Positioning:** Put notifications exactly where you want them, or have them dynamically anchor to the floating badge.
*   🔊 **App-Level Controls:** Mute annoying apps or set custom `.wav` sounds for specific ones.
*   🖥️ **Invisible Footprint:** It runs in the tray and is completely hidden from the Alt+Tab menu.

It's completely free and open source. Check it out on GitHub!
[GitHub Link]

Would love to hear your feedback or feature requests!

---

## 2. Hacker News (Show HN)
**Title:** Show HN: NotifyLite – Open source Windows notification replacement

**Body:**
I got tired of the lack of customization in the Windows notification system, so I built an interceptor using C# / WPF and the WinRT UserNotificationListener API. 

NotifyLite suppresses the native OS banners and renders its own highly customizable toast cards. It also adds features that Windows is missing natively, like a floating, scrollable history widget, per-app muting, and per-app custom audio alerts. 

It's packaged as an MSIX but includes a script to easily self-sign and install without needing Developer Mode. It’s also completely hidden from the Alt+Tab task switcher using the `WS_EX_TOOLWINDOW` style.

Repo: [GitHub Link]
Docs: [Docs Link]

Happy to answer any technical questions about the WinRT interception or the WPF rendering!

---

## 3. Product Hunt
**Tagline:** The ultimate open source Windows notification manager.

**Description:**
NotifyLite replaces boring, rigid Windows notification banners with fully customizable, animated toast cards. Control colors, fonts, and positioning. Never miss an alert with the floating history widget, and boost productivity with per-app muting and custom sounds. Free and open source!
