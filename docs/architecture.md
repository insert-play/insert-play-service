# Architecture

This document describes the internal architecture of `insert-play-service`, its components, data flows, and platform-specific design decisions.

---

## Overview

InsertPlay is a long-running background service built on the .NET 8 Generic Host (`IHostedService`). It listens for removable media events, parses game manifests from SD cards, supports synthetic manifests for physical PS2 discs, and manages the full lifecycle of game processes.

The service is structured around a thin platform abstraction layer that isolates OS-specific code (device detection) from cross-platform business logic.

---

## Component Diagram

```mermaid
graph TD
    Host["InsertPlayWorker\n(IHostedService / Generic Host)"]
    DM["IDeviceMonitor"]
    PM2["IPS2DiscMonitor"]
    WDM["WindowsDeviceMonitor\nWMI Win32_VolumeChangeEvent"]
    LDM["LinuxDeviceMonitor\nFileSystemWatcher + udev"]
    WPM2["WindowsPs2DiscMonitor\nOptical drive polling"]
    LPM2["LinuxPs2DiscMonitor\n(v1 placeholder)"]
    MP["ManifestParser\nSystem.Text.Json"]
    P2MF["Ps2DiscManifestFactory"]
    PR["PreLaunchRunner"]
    POR["PostLaunchRunner"]
    RA["RetroAchievementsSessionProvider"]
    CS["ICredentialStore"]
    GL["GameLauncher\nSystem.Diagnostics.Process"]
    PM["ProcessManager"]
    CI["ControllerInputHandler\nSDL2-CS"]
    Config["appsettings.json\nInsertPlayOptions"]
    Manifest["insertplay.json\n(SD card root)"]

    Host --> DM
    Host --> PM2
    DM --> WDM
    DM --> LDM
    PM2 --> WPM2
    PM2 --> LPM2
    Host --> MP
    Host --> P2MF
    Host --> PR
    Host --> POR
    Host --> RA
    RA --> CS
    Host --> GL
    Host --> CI
    GL --> PM
    CI --> PM
    Config --> Host
    Manifest --> MP
    PM -->|"kill / WaitForExitAsync"| GL
```

---

## Data Flow — Card Insertion

```mermaid
sequenceDiagram
    participant SD as SD Card
    participant DM as DeviceMonitor
    participant MP as ManifestParser
    participant GL as GameLauncher
    participant PM as ProcessManager
    participant CI as ControllerInputHandler
    participant Game as Game Process

    SD->>DM: Card inserted (drive path)
    DM->>MP: ParseManifestAsync(drivePath)
    MP-->>DM: GameManifest
    DM->>GL: LaunchAsync(manifest, drivePath)
    GL->>Game: Process.Start()
    GL->>PM: TrackProcess(process)
    GL->>CI: BeginPolling(manifest.StopCombination)

    alt Stop combination held
        CI->>PM: StopRequested()
        PM->>Game: Kill() / CloseMainWindow()
    else Game exits naturally
        Game-->>PM: Exited event
    end

    PM->>CI: StopPolling()
    Note over PM: Cleanup resources
```

## Data Flow — Physical PS2 Disc (Windows)

```mermaid
sequenceDiagram
    participant Disc as PS2 Disc
    participant PDM as WindowsPs2DiscMonitor
    participant MF as Ps2DiscManifestFactory
    participant PR as PreLaunchRunner
    participant RA as RetroAchievementsSessionProvider
    participant GL as GameLauncher
    participant PM as ProcessManager

    Disc->>PDM: Optical disc inserted
    PDM->>MF: TryCreateForDrive(drivePath)
    MF-->>PDM: Synthetic GameManifest
    PDM->>RA: EnrichAsync(credentials)
    PDM->>PR: RunAsync(manifest, basePath, creds)
    PR->>GL: Launch(manifest, basePath)
    GL->>PM: Track(process)
```

## Data Flow — Card Removal

```mermaid
sequenceDiagram
    participant SD as SD Card
    participant DM as DeviceMonitor
    participant PM as ProcessManager
    participant Game as Game Process

    SD->>DM: Card removed (drive path)
    DM->>PM: CardRemoved(drivePath)

    alt Game is running from this card
        PM->>Game: Kill()
        PM->>PM: Cleanup resources
    else No game running
        Note over PM: No action required
    end
```

---

## Components

### `InsertPlayWorker` (Entry Point)

The `IHostedService` implementation hosted by the .NET Generic Host. Owns the top-level lifecycle: starts `IDeviceMonitor` and `IPS2DiscMonitor`, wires media events, coordinates launch flow, and handles session cleanup.

### `IDeviceMonitor`

Abstraction interface for OS-specific removable media detection.

```csharp
public interface IDeviceMonitor
{
    event EventHandler<CardEventArgs> CardInserted;
    event EventHandler<CardEventArgs> CardRemoved;
    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

#### `WindowsDeviceMonitor`

- Uses WMI `ManagementEventWatcher` with a `Win32_VolumeChangeEvent` WQL query (`EventType` 2 = insert, 3 = remove) to detect removable drive changes.
- Identifies a valid game card by the presence of `insertplay.json` at the drive root.

#### `LinuxDeviceMonitor`

- Watches `/media` and `/run/media/$USER` using `FileSystemWatcher` for directory creation/deletion events. udev mounts removable drives there by default on SteamOS, Arch Linux, and Ubuntu.
- Falls back to polling `DriveInfo.GetDrives()` filtered by `DriveType.Removable` on distributions where the mount path differs.
- Same card identification strategy: checks for `insertplay.json` at the mounted path.

### `IPS2DiscMonitor`

Abstraction for physical PS2 optical disc detection.

#### `WindowsPs2DiscMonitor`

- Polls optical drives and detects likely PS2 media by checking for `SYSTEM.CNF`.
- Emits `DiscInserted` / `DiscRemoved` events.
- Can be gated by the `PS2Disc` options and local PCSX2 availability.

#### `LinuxPs2DiscMonitor`

- v1 placeholder implementation.
- Logs that Linux physical disc auto-launch is not implemented yet.

### `ManifestParser`

Reads and deserializes `insertplay.json` from the SD card root using `System.Text.Json`. Validates required fields (`schemaVersion`, `id`, `title`, `executable`) and verifies that the executable exists on the mounted media.

### `Ps2DiscManifestFactory`

Creates a synthetic runtime `GameManifest` for physical PS2 discs. The generated launch config targets local `pcsx2/pcsx2-qt.exe` and passes `-fullscreen -disc <drive>`.

This keeps the launcher path uniform: SD cards use file manifests, while optical media uses generated manifests.

### `GameLauncher`

Starts the game process using `System.Diagnostics.Process` with `ProcessStartInfo` built from the manifest fields. Key behaviors:

- Sets `WorkingDirectory` from the manifest or defaults to the executable's parent directory.
- Does not redirect stdio by default (the game owns its own terminal or window).
- On Windows, supports optional elevation via `Verb = "runas"` when configured.
- Returns the started `Process` object to `ProcessManager`.

### `ProcessManager`

Tracks the active `Process` and handles termination:

- Subscribes to `process.Exited` (`EnableRaisingEvents = true`) for natural exit detection.
- Exposes `StopAsync()` for forced termination triggered by `ControllerInputHandler` or a `CardRemoved` event.
- Uses `WaitForExitAsync(CancellationToken)` (.NET 5+) as the primary async wait mechanism.
- Logs exit code and total session duration after each game session.
- Always `Dispose()`s the `Process` handle after exit to prevent handle leaks.

### `ControllerInputHandler`

Polls SDL2 (via SDL2-CS) on a dedicated background thread at a configurable interval (default: 50 ms). Detects when **all** buttons in `StopCombination` are simultaneously held. Fires a `StopRequested` event consumed by `ProcessManager`.

Button names in `stopCombination` are mapped to `SDL_GameControllerButton` enum values, with a fallback to raw joystick button indices for non-standard devices. See [manifest-spec.md — Stop Combination Button Names](manifest-spec.md#stop-combination-button-names) for the full list.

### `PreLaunchRunner` and `PostLaunchRunner`

- Resolve per-platform script paths from `preLaunchScript` and `postLaunchScript`.
- Run scripts with configured timeout and environment variables.
- Pre-launch failures can block the game start; post-launch failures are logged and ignored.

### `RetroAchievementsSessionProvider` and `ICredentialStore`

- `ICredentialStore` persists RetroAchievements credentials on the host machine.
- `RetroAchievementsSessionProvider` authenticates against RetroAchievements (`r=login2`) and caches a runtime token in memory.
- Runtime credential data is forwarded to pre/post-launch scripts as environment variables.

### Configuration (`InsertPlayOptions`)

Strongly-typed options class bound from `appsettings.json` via `IOptions<InsertPlayOptions>`. See [configuration.md](configuration.md) for all options.

---

## Platform Abstraction Design

```
src/
├── InsertPlay.Core/                 # Platform-agnostic business logic
│   ├── IDeviceMonitor.cs
│   ├── ManifestParser.cs
│   ├── GameLauncher.cs
│   ├── ProcessManager.cs
│   ├── ControllerInputHandler.cs
│   └── Models/
│       ├── GameManifest.cs
│       └── InsertPlayOptions.cs
├── InsertPlay.Windows/              # Windows-specific implementations
│   └── WindowsDeviceMonitor.cs
├── InsertPlay.Linux/                # Linux-specific implementations
│   └── LinuxDeviceMonitor.cs
└── InsertPlay.Service/              # Service host entry point
    └── Program.cs                   ← DI wiring: registers the correct
                                       IDeviceMonitor per OS at startup
```

The correct `IDeviceMonitor` implementation is registered at startup based on `RuntimeInformation.IsOSPlatform(OSPlatform.Windows)`. All other components in `InsertPlay.Core` are OS-agnostic and never reference platform-specific types.

---

## Technology Decisions

| Concern | Choice | Rationale |
|---|---|---|
| Runtime | .NET 8 | LTS, cross-platform, Native AOT support |
| Service host | Generic Host (`IHostedService`) | Standard .NET service model; maps cleanly to both Windows Service and systemd |
| SD detection — Windows | `RegisterDeviceNotification` + WMI fallback | Lowest latency (~100 ms); WMI fallback covers service contexts without a message pump |
| SD detection — Linux | `FileSystemWatcher` on `/media` paths | No native dependencies; covers SteamOS and most Linux distros without udev rules |
| Controller input | SDL2-CS | Only actively maintained cross-platform gamepad library for .NET; handles XInput, DirectInput, HID, and evdev transparently |
| JSON parsing | `System.Text.Json` + source generation | No external dependencies; AOT-compatible; built-in schema-like validation via required properties |
| Process management | `System.Diagnostics.Process` | Built-in; `WaitForExitAsync` available since .NET 5; no extra dependencies |
