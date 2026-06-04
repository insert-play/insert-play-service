# insert-play-service

> Physical media detection and automatic game launching for PC.

[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%2FSteamOS-blue)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()
[![Status](https://img.shields.io/badge/status-MVP-orange)]()

**InsertPlay** is a cross-platform background service that detects removable game media and automatically launches the associated title. Today, the service supports SD cards (Windows/Linux) and physical PS2 discs (Windows + local PCSX2). When you're done playing, press a configurable button combination on your controller to gracefully exit.

For emulator workflows, InsertPlay also supports RetroAchievements credentials and forwards them to pre/post-launch scripts through environment variables.

No launchers. No menus. Just insert and play.

---

## How It Works

### SD Card Flow (Windows + Linux)

1. **Insert** an SD card containing an `insertplay.json` manifest file at its root.
2. **InsertPlay** detects the card, reads the manifest, and launches the game.
3. **Play** normally — the service monitors the game process in the background.
4. **Exit** by pressing the configured button combination on your controller (or close the game window normally).
5. **Remove** the SD card and the cycle is complete.

### Physical Disc Flow (PS2 on Windows)

1. **Insert** a PS2 disc into an optical drive.
2. **InsertPlay** detects the disc (looks for `SYSTEM.CNF`).
3. If enabled and idle, InsertPlay creates a runtime manifest and starts local `pcsx2/pcsx2-qt.exe` with `-disc <drive>`.
4. **Exit** with the same controller stop combination (or close PCSX2 normally).

---

## Features

- Automatic SD card insertion and removal detection on Windows and Linux/SteamOS
- Physical PS2 optical disc detection and auto-launch on Windows (with local PCSX2)
- JSON manifest-driven game launch (`insertplay.json` at the SD card root)
- Pre-launch and post-launch scripts (per-platform, per-game)
- RetroAchievements credential support for script-based emulator setup
- Controller button combination to quit the running game
- Works as a background Windows Service or Linux systemd unit
- Cross-platform: Windows 10+ and Linux/SteamOS (Arch-based)

---

## Roadmap

See [docs/roadmap.md](docs/roadmap.md) for the full roadmap, including:

- **Priority 1 — Game installation & Steam integration** — Install games from the SD card and register them as Steam non-Steam shortcuts
- **Priority 2 — Portable savegame sync** — Automatically sync save files between the SD card and the local machine on insert/exit
- Additional future suggestions

---

## Prerequisites

| Platform | Requirement |
|---|---|
| Both | [.NET 8 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) |
| Both | SDL2 runtime library (`libSDL2`) |
| Windows | Windows 10 version 1903 or later |
| Linux | systemd-based distro (SteamOS 3+, Ubuntu 20.04+, Arch Linux) |
| Windows (PS2 discs) | Local `pcsx2/pcsx2-qt.exe` under the service base directory |

---

## Installation

### Windows

```powershell
# Clone the repository
git clone https://github.com/your-org/insert-play-service.git
cd insert-play-service

# Download the SDL2 native DLL (run once after cloning)
powershell -ExecutionPolicy Bypass -File tools/get-sdl2-native.ps1

# Build
dotnet build -c Release

# Install and start as a Windows Service
sc.exe create InsertPlayService binpath="<path-to-InsertPlay.Service.exe>"
sc.exe start InsertPlayService
```

### Linux / SteamOS

```bash
git clone https://github.com/your-org/insert-play-service.git
cd insert-play-service

dotnet build -c Release

# Copy the systemd unit file and enable the service
sudo cp deploy/linux/insertplay.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now insertplay
```

---

## Preparing Media

### SD Card Manifest

Place an `insertplay.json` file at the **root** of your SD card. Minimal example:

```json
{
  "schemaVersion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "My Game",
  "executable": "bin/MyGame.exe",
  "stopCombination": ["Back", "Start"]
}
```

See [docs/manifest-spec.md](docs/manifest-spec.md) for the full specification.

### Physical PS2 Disc (Windows)

No `insertplay.json` is required for physical PS2 discs. InsertPlay can synthesize a runtime manifest when:

- `InsertPlay:PS2Disc:Enabled` is `true`
- `InsertPlay:PS2Disc:AutoLaunch` is `true`
- `InsertPlay:PS2Disc:RequireLocalPcsx2Folder` passes (or is disabled)

See [docs/configuration.md](docs/configuration.md) for PS2 disc options.

---

## RetroAchievements

On Windows, you can configure RetroAchievements credentials from the tray menu (`Conta RetroAchievements...`). InsertPlay stores credentials locally and injects runtime variables (username/password/token/timestamp) into pre/post-launch scripts.

See [docs/configuration.md](docs/configuration.md) and [docs/manifest-spec.md](docs/manifest-spec.md) for details.

---

## Configuration

The service is configured via `appsettings.json`. See [docs/configuration.md](docs/configuration.md) for all available options.

---

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full component architecture, data flow diagrams, and technical design decisions.

---

## Contributing

See [docs/contribution-guide.md](docs/contribution-guide.md) for build instructions, project structure, and coding standards.

---

## License

MIT — see [LICENSE](LICENSE).
