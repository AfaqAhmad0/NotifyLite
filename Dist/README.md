# NotifyLite

**Custom Windows Notification Interceptor**

NotifyLite replaces Windows default notification banners with beautiful, customizable toast cards. Click any notification to open its source app, or dismiss it with the ✕ button.

---

## Installation

### Requirements
- Windows 10/11 (64-bit)
- **Developer Mode** must be enabled:
  - Windows 11: `Settings > System > For developers > ON`
  - Windows 10: `Settings > Privacy & Security > For developers > ON`

### Quick Install
1. Right-click `Install.ps1` → **Run with PowerShell** (as Administrator)
2. Follow the prompts
3. Launch **NotifyLite** from the Start Menu

### Manual Install (PowerShell as Admin)
```powershell
certutil -addstore TrustedPeople .\NotifyLite.cer
Add-AppxPackage .\NotifyLite.msix
```

---

## Features

- 🔔 **Custom notification toasts** - dark/light themed, animated cards
- 👆 **Click to open** - click a toast to open the source app
- ✕ **Dismiss button** - close without opening the app
- 🔊 **Notification sounds** - system default or custom .wav per app
- 📋 **Action Center** - notifications persist in Win+N tray
- ⚙️ **Fully customizable** - right-click the tray icon → Settings

### Settings (right-click tray icon → ⚙️ Settings)
- Theme (Dark/Light)
- Font family & sizes
- Card width, corner radius, opacity
- Text & background colors
- Toast position (all 4 corners)
- Auto-dismiss duration
- Per-app sound configuration
- Auto-start with Windows

---

## Uninstall

Run `Uninstall.ps1` or:
```powershell
Get-AppxPackage -Name "NotifyLite" | Remove-AppxPackage
```

---

## Troubleshooting

**"Permission Required" dialog:**
Grant notification access in `Settings → Privacy → Notifications`

**App doesn't start:**
Make sure Developer Mode is enabled in Windows Settings.

**No notifications appearing:**
Check that notifications are enabled for your apps in Windows Settings.

---

Built with ❤️ using WPF + WinRT on .NET 8
