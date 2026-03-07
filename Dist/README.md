# NotifyLite

**Custom Windows Notification Interceptor**

NotifyLite replaces Windows default notification banners with beautiful, customizable toast cards. Click any notification to open its source app, or dismiss it with the ✕ button.

---

## Installation

### Requirements
- Windows 10/11 (64-bit)

### Quick Install
1. Double-click **`Install.bat`**
2. Windows may show an "Unknown Publisher" warning (The publisher could not be verified. Are you sure you want to run this software?) - click **Run**
3. Approve the admin prompt. The script will handle certificate installation automatically without requiring Developer Mode.
4. Launch **NotifyLite** from the Start Menu

### Manual Install (PowerShell as Admin)
```powershell
certutil -addstore TrustedPeople .\NotifyLite.cer
Add-AppxPackage .\NotifyLite.msix
```

---

## Features

- 🔔 **Custom notification toasts** - dark/light themed, animated cards
- 🫧 **Floating Icon & History Widget** - draggable icon showing unread count, click for a scrollable notification history
- 👆 **Click to open** - click a toast to open the source app
- ✕ **Dismiss button** - close without opening the app, animates straight into the floating widget
- 🔊 **Notification sounds** - system default or custom .wav per app
- 📋 **Action Center** - notifications persist in Win+N tray
- ⚙️ **Fully customizable** - right-click the tray icon → Settings

### Settings (right-click tray icon → ⚙️ Settings)
- Floating icon toggle
- Theme (Dark/Light)
- Font family & sizes
- Card width, corner radius, opacity
- Text & background colors
- Toast position (all 4 corners, or completely custom X/Y screen coordinates)
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
Try running `Install.bat` as Administrator manually.

**No notifications appearing:**
Check that notifications are enabled for your apps in Windows Settings.

---

Built with ❤️ using WPF + WinRT on .NET 8
