# Roadmap

This document describes the planned evolution of **InsertPlay**, from the current MVP to future capabilities.

---

## MVP — v0.1 (Current)

The minimum viable product focuses on the core insert-and-play experience.

### Goals

- Detect SD card insertion and removal reliably on both Windows and Linux/SteamOS.
- Read a JSON manifest (`insertplay.json`) from the card root.
- Launch the game automatically with no user interaction.
- Allow the user to quit the game at any time using a configurable controller button combination.

### Scope

| Feature | Status |
|---|---|
| SD card detection (Windows) | ✅ MVP |
| SD card detection (Linux/SteamOS) | ✅ MVP |
| `insertplay.json` manifest parsing | ✅ MVP |
| Auto-launch game on insert | ✅ MVP |
| Controller stop combination | ✅ MVP |
| Background service (Windows Service + systemd) | ✅ MVP |
| Cross-platform: Windows 10+ / Linux/SteamOS | ✅ MVP |

---

## Priority 1 — Pre-Launch Script

Allow each game card to ship a script that runs **before the game process is started**. The service calls the script, waits for it to finish, and only then launches the game. This enables per-game, per-machine configuration that requires no changes to the game binary itself — e.g. setting a custom display resolution, adjusting graphics settings files, mounting overlays, or patching config files in place.

### Planned Behavior

1. If the manifest declares a `preLaunchScript`, the service resolves its path relative to the SD card root.
2. The service invokes the script, passing the configured parameters as **environment variables** (cross-platform) and optionally as positional CLI arguments.
3. The script runs synchronously. If it exits with a non-zero code the launch is aborted and the error is logged.
4. A configurable `preLaunchTimeoutSeconds` (default: `30`) is enforced to prevent a hanging script from blocking the service indefinitely.
5. On Windows, `.ps1` scripts are executed via `powershell -ExecutionPolicy Bypass`; `.bat` / `.cmd` via `cmd /c`. On Linux/SteamOS, `.sh` scripts are executed via `bash`.

### Parameters Passed to the Script

Parameters are exposed as environment variables prefixed with `INSERTPLAY_`, so scripts on any platform can read them uniformly.

| Environment variable | Source | Default |
|---|---|---|
| `INSERTPLAY_RESOLUTION` | `preLaunchParams.resolution` in manifest, overridable in `appsettings.json` | `native` |
| `INSERTPLAY_CARD_PATH` | Mount path of the SD card (drive root on Windows, mount point on Linux) | *(always set)* |
| `INSERTPLAY_GAME_TITLE` | `title` field from the manifest | *(always set)* |

Additional user-defined key/value pairs can be passed via `preLaunchParams` in the manifest and are forwarded as `INSERTPLAY_<KEY>` (uppercased).

### Manifest Fields

```json
{
  "preLaunchScript": {
    "windows": "scripts/configure.ps1",
    "linux":   "scripts/configure.sh"
  },
  "preLaunchTimeoutSeconds": 20,
  "preLaunchParams": {
    "resolution": "1920x1080",
    "graphicsPreset": "high"
  }
}
```

`preLaunchScript` also accepts a plain string as shorthand when the same script works on both platforms:

```json
{ "preLaunchScript": "scripts/configure.ps1" }
```

- **`preLaunchScript`** *(string | object, optional)* — Per-platform script paths relative to the SD card root. Use the object form to ship separate scripts for Windows and Linux on the same card. If a platform key is absent the pre-launch step is silently skipped on that platform.
- **`preLaunchTimeoutSeconds`** *(integer, optional)* — Per-game timeout override. Falls back to the global `appsettings.json` value.
- **`preLaunchParams`** *(object, optional)* — Arbitrary key/value pairs forwarded as environment variables. The `resolution` key is treated specially: `"native"` means no override (the script receives the literal string `"native"` and can decide what to do with it).

### New Configuration Options

- `PreLaunch.Enabled` — toggle pre-launch execution globally (default: `true`)
- `PreLaunch.TimeoutSeconds` — global timeout in seconds (default: `30`)
- `PreLaunch.DefaultResolution` — fallback resolution when the manifest does not specify one (default: `"native"`)

### Example Script (Windows — `scripts/configure.ps1`)

```powershell
param()
# Resolution is available as an environment variable
$res = $env:INSERTPLAY_RESOLUTION

if ($res -ne "native") {
    # Write resolution into the game's config file
    $configPath = Join-Path $env:INSERTPLAY_CARD_PATH "data\config.ini"
    (Get-Content $configPath) -replace '^(Width\s*=).*', "Width = $($res.Split('x')[0])" |
        Set-Content $configPath
    (Get-Content $configPath) -replace '^(Height\s*=).*', "Height = $($res.Split('x')[1])" |
        Set-Content $configPath
}
```

### Example Script (Linux/SteamOS — `scripts/configure.sh`)

```bash
#!/usr/bin/env bash
set -euo pipefail

if [[ "$INSERTPLAY_RESOLUTION" != "native" ]]; then
    WIDTH="${INSERTPLAY_RESOLUTION%x*}"
    HEIGHT="${INSERTPLAY_RESOLUTION#*x}"
    sed -i "s/^width=.*/width=${WIDTH}/" "${INSERTPLAY_CARD_PATH}/data/config.cfg"
    sed -i "s/^height=.*/height=${HEIGHT}/" "${INSERTPLAY_CARD_PATH}/data/config.cfg"
fi
```

---

## Priority 2 — Game Installation & Steam Integration

Extend InsertPlay to install games from the SD card to the local machine and register them as Steam non-Steam shortcuts, for a seamless Steam Deck and Big Picture Mode experience.

### Planned Features

**Game Installation**
- Detect whether the game from a given card is already installed locally.
- Execute an install script (`installScript` field in the manifest) to copy or extract game files to a configurable install directory.
- Track installed games in a local registry (JSON database).
- Support incremental updates: compare the installed version against the `version` field in the manifest.

**Steam Non-Steam Shortcut Registration**
- After installation, automatically add the game as a non-Steam shortcut using Steam's `shortcuts.vdf` file.
- Apply cover art (`coverImage`) to the Steam library entry.
- Support Steam Deck artwork variants (hero, logo, icon) if provided on the card.
- Optionally remove the shortcut when the game is uninstalled.

### Manifest Fields Used

`installScript`, `version`, `coverImage`, `steamAppId`, `title`

### New Configuration Options

- `InstallDirectory` — where games are installed locally
- `SteamUserDataPath` — path to Steam's `userdata` folder

---

## Priority 3 — Portable Savegame Sync

Enable a fully portable gaming experience by syncing save data between the SD card and the local machine automatically.

### Planned Behavior

**On card insertion (before game launch):**
1. Check for a save directory on the SD card (`sdSaveDirectory` in manifest).
2. If save data exists on the SD, copy it to the local save path (`saveDataPath`).
3. If a local save also exists, compare timestamps and apply the configured conflict resolution strategy.

**On game exit:**
1. Copy the local save data (`saveDataPath`) to the SD card (`sdSaveDirectory`).
2. Verify the copy was successful before marking the sync complete.
3. If the copy fails, log a warning and leave local saves intact.

### Key Design Considerations

- `saveDataPath` supports environment variable expansion (`%APPDATA%`, `$HOME`, `~`, etc.) on both platforms.
- Conflict resolution strategy is configurable: `newest-wins` (default), `sd-wins`, `local-wins`.
- A sync log is written to the SD card for auditing (`sync.log`).

### Manifest Fields Used

`saveDataPath`, `sdSaveDirectory`

### New Configuration Options

- `SaveSync.Enabled` — toggle sync on/off globally
- `SaveSync.ConflictResolution` — conflict resolution strategy

---

## Future Suggestions

The following features are not yet prioritized but are valuable candidates for future versions.

### Cover Art & Notification Overlay

Show a system notification (toast on Windows, libnotify on Linux) with the game's cover art and title when a card is inserted. Provides immediate visual feedback that the card was recognized and the game is about to launch.

### Playtime Tracking

Record session playtime locally and on the SD card itself (`playtime.json`). Enables per-card play statistics and a portable play history that travels with the card across machines.

### Multi-Reader Support

Handle multiple SD card readers simultaneously. Each reader is monitored independently, allowing multiple games to be queued or managed concurrently. Particularly useful for kiosk or multi-station setups.

### SD Card Health Monitoring

Detect and warn about potential card issues:
- Read/write errors during manifest parsing or save sync
- Low available space on the card
- Unusually slow read speeds (potential card degradation)

Alert the user via system notification or a tray icon state change.

### Per-Game Controller Profiles

Allow each game manifest to define a custom button mapping (e.g., remap the stop combination or define a screenshot shortcut). Stored in the manifest under a `controllerProfile` field.

### Game Library Tray UI

A lightweight system tray application that displays:
- A list of all known SD cards (by `id` from previously seen manifests)
- The currently inserted card and active game
- Quick access to settings and logs

Builds on the local game registry introduced in Priority 1.

### Remote Card Imaging

Generate and restore full SD card images (game + manifest + saves) remotely, enabling backup and distribution of game cards without physical access to the original card.
