---
layout: default
title: Installation
---

# Installation

Getting started with your new custom toast notifications for Windows is fast and easy.

## Requirements
- Windows 10 or Windows 11 (64-bit)
- Developer Mode is **not** required when using the provided batch script.

## Download Methods

### Recommended: Pre-packaged Release
1. Navigate to the [Releases](https://github.com/AfaqAhmad0/NotifyLite/releases) page on GitHub.
2. Download the latest `NotifyLite-vX.X.X.zip` archive.
3. Extract the contents to a folder.

## Setup Instructions
1. Open the extracted folder and double-click **`Install.bat`**.
2. If Windows SmartScreen shows a warning, click **More info** -> **Run anyway**.
3. Accept the Administrator prompt (UAC) to allow the script to trust the local application certificate.

## First Run Guide
On your first launch:
1. Windows will prompt you to grant **Notification Access**.
2. Go to **Settings > Privacy > Notifications** and toggle the switch to allow NotifyLite to read notifications.
3. Once granted, you'll see the purple Floating Icon appear on your screen. Right-click the system tray icon to open Settings and start customizing!

## Troubleshooting
- **No notifications are appearing?** Ensure you granted Notification Access in Windows Privacy Settings.
- **Installation failed?** Try running `Install.bat` explicitly as Administrator.
- **Still seeing default Windows banners?** Restart NotifyLite so it can properly apply the registry suppression keys.
