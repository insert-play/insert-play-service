# insert-play-service

> Physical game card detection and automatic game launching for PC.

[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%2FSteamOS-blue)]()
[![.NET](https://img.shields.io/badge/.NET-8.0-purple)]()
[![License](https://img.shields.io/badge/license-MIT-green)]()
[![Status](https://img.shields.io/badge/status-MVP-orange)]()

**InsertPlay** is a cross-platform background service that detects SD game cards and automatically launches the associated game. When you're done playing, press a configurable button combination on your controller to gracefully exit. Pull out the card, and everything stops.

No launchers. No menus. Just insert and play.

---

## How It Works

1. **Insert** an SD card containing an `insertplay.json` manifest file at its root.
2. **InsertPlay** detects the card, reads the manifest, and launches the game.
3. **Play** normally — the service monitors the game process in the background.
4. **Exit** by pressing the configured button combination on your controller (or close the game window normally).
5. **Remove** the SD card and the cycle is complete.

---

## Features (MVP v0.1)

- Automatic SD card insertion and removal detection on Windows and Linux/SteamOS
- JSON manifest-driven game launch (`insertplay.json` at the SD card root)
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
sudo cp deploy/insertplay.service /etc/systemd/system/
sudo systemctl daemon-reload
sudo systemctl enable --now insertplay
```

---

## Preparing an SD Card

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
