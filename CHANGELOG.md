# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.1.4](https://github.com/insert-play/insert-play-service/compare/v0.1.3...v0.1.4) (2026-05-31)


### Features

* **release:** add workflow_dispatch trigger to release workflow ([725d706](https://github.com/insert-play/insert-play-service/commit/725d706545f3a849c13ea54391e9d515a7ce2eac))
* **release:** add workflow_dispatch trigger to release workflow ([e793d0a](https://github.com/insert-play/insert-play-service/commit/e793d0ad417112dff4d882102f38a01fb38ae337))

## [0.1.3](https://github.com/insert-play/insert-play-service/compare/v0.1.2...v0.1.3) (2026-05-31)


### Bug Fixes

* **release:** update artifact paths for GitHub release assets ([4b3dfc2](https://github.com/insert-play/insert-play-service/commit/4b3dfc23192a682c7961dc25fc10da7040c667f5))
* **release:** update artifact paths for GitHub release assets ([30fff60](https://github.com/insert-play/insert-play-service/commit/30fff60cb8af9bc8dcb512f7aadea6389f807a46))

## [0.1.2](https://github.com/insert-play/insert-play-service/compare/v0.1.1...v0.1.2) (2026-05-31)


### Features

* **release:** streamline GitHub release process and remove CHANGELOG extraction step ([f3c8ed3](https://github.com/insert-play/insert-play-service/commit/f3c8ed3f393a8c30327493ead405a7d0f6e8cb06))
* **release:** streamline GitHub release process and remove CHANGELOG… ([e59aed8](https://github.com/insert-play/insert-play-service/commit/e59aed8c3df078965c29144e330e496473d36fd6))

## [0.1.1](https://github.com/insert-play/insert-play-service/compare/v0.1.0...v0.1.1) (2026-05-31)


### Features

* **manifest:** add pre-launch script and parameters support ([223f5a4](https://github.com/insert-play/insert-play-service/commit/223f5a49a07d8b1272f2a8e735b21e4166537576))
* **pre-launch:** add insertplay.json for Need for Speed: Underground 2 ([fa9b9fa](https://github.com/insert-play/insert-play-service/commit/fa9b9fa8480f9f5f5075fc34a665b907470aa6de))
* **pre-launch:** add options for pre-launch script execution control ([b198491](https://github.com/insert-play/insert-play-service/commit/b198491a9c69ac10fe6abaa92a677f02e89be68f))
* **pre-launch:** add pre-launch configuration to appsettings ([9dfae31](https://github.com/insert-play/insert-play-service/commit/9dfae31535d477e8194105c3d70d94562a7fc21f))
* **pre-launch:** add PreLaunchScriptSpec for platform-specific scripts ([d1316f4](https://github.com/insert-play/insert-play-service/commit/d1316f45d72fc240635e9d2332bdea100112f38e))
* **pre-launch:** implement pre-launch script execution logic ([8cb888e](https://github.com/insert-play/insert-play-service/commit/8cb888e110bd7680463dc370eb051f9a52d3327b))
* **pre-launch:** register PreLaunchRunner as a core service ([a357920](https://github.com/insert-play/insert-play-service/commit/a3579206ce42ab70f93b039db4fff12f4c47908e))
* **worker:** integrate pre-launch runner into insert play workflow ([7d7a1c2](https://github.com/insert-play/insert-play-service/commit/7d7a1c2a93009b055a00af09aafe4edd7e33ae88))

## [Unreleased]

## [0.1.0] - 2026-05-30

### Added
- SD card insertion and removal detection on Windows via WMI `Win32_VolumeChangeEvent` (`ManagementEventWatcher`)
- SD card insertion and removal detection on Linux/SteamOS via `FileSystemWatcher` on `/media` and `/run/media/$USER`
- JSON manifest (`insertplay.json`) parsing and validation using `System.Text.Json`
- Automatic game process launch on card insertion
- Controller button combination detection to quit the running game (SDL2-CS vendored as source in `InsertPlay.Core/SDL2/`)
- Game process lifecycle monitoring with clean exit handling
- Background service host: Windows Service (`IHostedService`) and Linux systemd unit
- Cross-platform support: Windows 10+ and Linux/SteamOS (Arch-based)
- `appsettings.json` configuration for default stop combination, controller poll interval, log level, and Linux media path overrides
- `IDeviceMonitor` abstraction with platform-specific implementations (`WindowsDeviceMonitor`, `LinuxDeviceMonitor`)
- `tools/get-sdl2-native.ps1` script to download the official SDL2 native DLL for Windows
- Example manifests under `examples/` for Need for Speed: Most Wanted and Need for Speed: Underground

[Unreleased]: https://github.com/your-org/insert-play-service/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/your-org/insert-play-service/releases/tag/v0.1.0
