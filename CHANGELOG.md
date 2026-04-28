# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [2.0.0] - 2026-04-07
### Added
- **Stick to Icon Position Mode:** Toasts can now anchor dynamically to the floating history badge.
- **Hidden from Task Switcher:** Custom toasts, the history widget, and the floating icon are now completely hidden from the Alt+Tab and Win+Tab menus.
- **History Widget:** A new popup allowing users to review and clear past notifications.
- **Per-App Controls:** Mute specific apps or assign them custom `.wav` sounds.
- **Settings UI Rewrite:** A brand new, beautiful, transparent Settings interface.

### Changed
- Refactored Registry suppression to ensure reliable hiding of native Windows banners.
- MSIX build script updated to handle self-signed certificates without needing Developer Mode.

## [1.0.0] - 2026-03-01
### Added
- Initial release of NotifyLite.
- Basic Windows notification interception via WinRT.
- Simple customizable toast card rendering.
