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

## Priority 1 — Game Installation & Steam Integration

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

## Priority 2 — Portable Savegame Sync

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
