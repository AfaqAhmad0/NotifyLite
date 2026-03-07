<p align="center">
  <h1 align="center">🔔 NotifyLite</h1>
  <p align="center"><strong>Custom Windows Notification Interceptor</strong></p>
  <p align="center">Replace boring Windows notification banners with beautiful, customizable toast cards.</p>
</p>

---

## ✨ Features

- 🎨 **Custom toast cards** - dark/light themed, animated slide-in/out notifications
- 🫧 **Floating Icon & History Widget** - Optional draggable badge showing unread notifications, clicking it reveals a scrollable history to review or dismiss past toasts!
- 👆 **Click to open** - click any toast to jump to the source app
- ✕ **Quick dismiss** - close button to dismiss without opening; notifications gracefully shrink into the floating widget
- 🔊 **Notification sounds** - system default or custom `.wav` per app
- 📋 **Action Center** - notifications persist in Windows notification tray (Win+N)
- ⚙️ **Fully customizable** - Settings UI accessible from tray icon, featuring a modern custom transparent UI
- 🖥️ **Tray-only app** - runs silently in system tray

## 🎛️ Customization

All settings accessible via **right-click tray icon → ⚙️ Settings**:

| Category | Options |
|----------|---------|
| **Floating Icon** | Toggle the draggable floating badge (shows unread count & history) |
| **Appearance** | Theme (Dark/Light), font family, title & body sizes |
| **Colors** | Title text, body text, card background, accent color - all via hex picker |
| **Card** | Width, corner radius, card opacity, text opacity |
| **Behavior** | Auto-dismiss duration, max visible toasts, position (**all 4 corners + custom X/Y coordinates**) |
| **Sound** | Enable/disable, system default or custom `.wav`, per-app overrides (mute specific apps) |

## 📦 Installation

### From Release ZIP
1. Download `NotifyLite-v2.0.0.zip` from [Releases](../../releases)
2. Extract the ZIP
3. Double-click **`Install.bat`**
4. Windows may show an **"Unknown Publisher" warning** (The publisher could not be verified. Are you sure you want to run this software?) - click **Run** to continue
5. Approve the admin prompt. The script handles certificate installation automatically without requiring Developer Mode.

### Requirements
- Windows 10/11 (64-bit)

### Manual Install (PowerShell as Admin)
```powershell
cd Package
certutil -addstore TrustedPeople .\NotifyLite.cer
Add-AppxPackage .\NotifyLite.msix
```

## 🔨 Build from Source

### Requirements
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Windows SDK](https://developer.microsoft.com/windows/downloads/windows-sdk/) (for `MakeAppx.exe` and `SignTool.exe`)

### Build & Install
```powershell
cd NotifyLite
powershell -ExecutionPolicy Bypass -File Scripts\build-msix.ps1
```

This single command will:
1. Publish a self-contained .NET 8 build
2. Package it as an MSIX
3. Sign with a self-signed certificate
4. Trust the certificate
5. Install the MSIX package

## �-️ Project Structure

```
NotifyLite/
├── NotifyLite.sln
├── Scripts/
│   └── build-msix.ps1              # Build → package → sign → install
├── Dist/
│   ├── Install.ps1                 # One-click installer for recipients
│   ├── Uninstall.ps1               # One-click uninstaller
│   └── README.md                   # Distribution readme
└── NotifyLite/
    ├── NotifyLite.csproj
    ├── App.xaml / App.xaml.cs       # Entry point, tray-only app
    ├── GlobalUsings.cs              # WPF/WinForms namespace resolution
    ├── MsixPackage/
    │   └── AppxManifest.xml         # MSIX identity & capabilities
    ├── Assets/                      # App icons
    ├── Services/
    │   ├── NotificationListener.cs  # WinRT UserNotificationListener API
    │   ├── NotificationSuppressor.cs# Registry-based native banner suppression
    │   ├── ActionCenterManager.cs   # Windows Action Center integration
    │   ├── TrayManager.cs           # System tray icon + context menu
    │   └── StartupManager.cs        # Auto-start via registry
    ├── Managers/
    │   └── ToastManager.cs          # Toast lifecycle, positioning, sounds
    ├── Windows/
    │   ├── ToastWindow.xaml/.cs     # Custom animated toast card
    │   └── SettingsWindow.xaml/.cs  # Settings UI
    ├── Models/
    │   └── NotificationData.cs      # Intercepted notification model
    └── Helpers/
        └── ConfigManager.cs         # JSON config persistence
```

## ⚙️ Tech Stack

- **Framework:** .NET 8, WPF
- **Notification API:** WinRT `UserNotificationListener`
- **Packaging:** MSIX (self-signed)
- **Tray Icon:** Hardcodet WPF TaskbarNotification
- **Config:** JSON (`%APPDATA%/NotifyLite/config.json`)

## 📄 License

MIT License - free to use, modify, and distribute.
