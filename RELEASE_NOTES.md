# 🔔 NotifyLite v1.0.0

Replace boring default Windows notification banners with beautiful, fully customizable toast cards.

---

## ✨ Features

- 🎨 **Custom animated toast cards** - sleek dark/light themed notifications with smooth slide-in/out animations
- 👆 **Click-to-open** - click any toast to jump directly to the source application
- ✕ **Quick dismiss** - close button to dismiss without opening the app
- 🔊 **Notification sounds** - system default or custom `.wav` file, with per-app overrides
- 📋 **Action Center integration** - notifications persist in the Windows notification tray (Win+N)
- ⚙️ **Fully customizable Settings UI** - everything adjustable from the system tray
- 🖥️ **Tray-only app** - runs silently in the system tray, zero desktop clutter

## 🎛️ Customization Options

| Category | What you can change |
|----------|-------------------|
| 🎨 Appearance | Theme (Dark/Light), font family, title & body font sizes |
| 🎯 Colors | Title text, body text, card background, accent (timer bar), all with hex color picker and live preview |
| 📐 Card | Width, corner radius, separate card opacity & text opacity |
| ⏱️ Behavior | Auto-dismiss duration, max visible toasts, position (all 4 corners) |
| 🔔 Sound | Enable/disable, system default or custom `.wav`, per-app overrides (Default / Custom / Muted) |

## 📦 Installation

### Requirements
- Windows 10/11 (64-bit)
- **Developer Mode** enabled:
  - Windows 11: `Settings > System > For developers > ON`
  - Windows 10: `Settings > Privacy & Security > For developers > ON`

### Quick Install
1. Download **`NotifyLite-v1.0.zip`** below
2. Extract the ZIP
3. Double-click **`Install.bat`**
4. Approve the admin prompt and follow the instructions

### Manual Install (PowerShell as Admin)
```powershell
cd Package
certutil -addstore TrustedPeople .\NotifyLite.cer
Add-AppxPackage .\NotifyLite.msix
```

### First Launch
- Windows will ask for notification access, click **Allow**
- The app runs in the system tray (look for the purple **N** icon)
- Right-click the tray icon for controls and **⚙️ Settings**

### ZIP Structure
```
NotifyLite-v1.0/
  Install.bat           <-- double-click this
  README.md
  Package/
    NotifyLite.msix
    NotifyLite.cer
    Install.ps1
    Uninstall.ps1
```

## 🗑️ Uninstall
Run `Package\Uninstall.ps1` or:
```powershell
Get-AppxPackage -Name "NotifyLite" | Remove-AppxPackage
```
