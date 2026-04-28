---
layout: default
title: FAQ
---

# Frequently Asked Questions

### Can I customize Windows notifications?
Yes! While Microsoft doesn't provide built-in tools for deep customization, NotifyLite acts as a complete Windows notification replacement. It intercepts the alerts before they show up and draws them using your own personalized styling.

### Can I replace Windows toast notifications?
Absolutely. NotifyLite disables the native `ToastEnabled` registry key and uses the `UserNotificationListener` API to catch notifications in the background, showing its own custom toast cards instead.

### Is this an open source notification manager?
Yes, NotifyLite is 100% free and open source under the MIT License. Contributions are welcome!

### Does this support Windows 10 and Windows 11?
Yes, NotifyLite provides Windows 11 notification customization and is fully backwards compatible with Windows 10 (64-bit).

### Does it hide from the Alt+Tab menu?
Yes! A major feature is that the custom toasts, history widget, and floating icon use a special Windows flag so they never clutter your Alt+Tab or Win+Tab task switcher.

### Troubleshooting
- **I uninstalled NotifyLite, but I'm not getting native notifications anymore!**
  Run the included `Uninstall.ps1` script, which safely restores your registry keys. If you deleted the app manually, open Registry Editor, navigate to `HKCU\Software\Microsoft\Windows\CurrentVersion\PushNotifications`, and set `ToastEnabled` to `1`.
