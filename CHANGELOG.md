# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
