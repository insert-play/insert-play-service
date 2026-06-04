# Game Manifest Specification

`insertplay.json` is the manifest file placed at the root of an SD game card. InsertPlay reads this file to identify the card, configure the game launch, and (in future versions) manage installation and save data.

Physical PS2 optical disc launches (Windows) do not use `insertplay.json`; InsertPlay generates a synthetic runtime manifest for those sessions.

---

## File Location

The file **must** be named exactly `insertplay.json` and placed at the **root** of the SD card.

```
SD:/
├── insertplay.json       ← required
├── bin/
│   └── MyGame.exe
└── assets/
    └── cover.png
```

---

## Schema Version

The current schema version is `"1.0"`. The `schemaVersion` field is required to allow future breaking changes to be detected and handled gracefully.

---

## Full JSON Schema

```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "$id": "https://insertplay.dev/schemas/manifest/1.0/manifest.schema.json",
  "title": "InsertPlay Game Manifest",
  "description": "Describes a game stored on an InsertPlay SD card.",
  "type": "object",
  "required": ["schemaVersion", "id", "title", "executable"],
  "additionalProperties": false,
  "properties": {
    "schemaVersion": {
      "type": "string",
      "description": "Schema version. Must be \"1.0\" for this version of InsertPlay.",
      "enum": ["1.0"]
    },
    "id": {
      "type": "string",
      "format": "uuid",
      "description": "UUID v4 that uniquely identifies this game card."
    },
    "title": {
      "type": "string",
      "minLength": 1,
      "description": "Display name of the game."
    },
    "executable": {
      "type": "string",
      "description": "Relative path to the game executable from the manifest file's directory. Use forward slashes."
    },
    "workingDirectory": {
      "type": "string",
      "description": "Working directory for the game process, relative to the manifest file. Defaults to the executable's parent directory."
    },
    "arguments": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Command-line arguments passed to the executable."
    },
    "stopCombination": {
      "type": "array",
      "items": { "type": "string" },
      "minItems": 1,
      "description": "Controller button names that must all be held simultaneously to quit the game. Overrides the service-level default from appsettings.json."
    },
    "preLaunchScript": {
      "oneOf": [
        { "type": "string" },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "windows": { "type": "string" },
            "linux": { "type": "string" }
          }
        }
      ],
      "description": "Script run before launch. Accepts a plain string (all platforms) or an object with platform keys."
    },
    "preLaunchTimeoutSeconds": {
      "type": "integer",
      "minimum": 1,
      "description": "Per-game timeout override for pre-launch script execution."
    },
    "preLaunchParams": {
      "type": "object",
      "additionalProperties": { "type": "string" },
      "description": "Key/value parameters forwarded to scripts as INSERTPLAY_<KEY> environment variables."
    },
    "postLaunchScript": {
      "oneOf": [
        { "type": "string" },
        {
          "type": "object",
          "additionalProperties": false,
          "properties": {
            "windows": { "type": "string" },
            "linux": { "type": "string" }
          }
        }
      ],
      "description": "Script run after game exit. Accepts a plain string (all platforms) or an object with platform keys."
    },
    "postLaunchTimeoutSeconds": {
      "type": "integer",
      "minimum": 1,
      "description": "Per-game timeout override for post-launch script execution."
    },
    "developer": {
      "type": "string",
      "description": "Name of the game developer or studio."
    },
    "publisher": {
      "type": "string",
      "description": "Name of the game publisher."
    },
    "description": {
      "type": "string",
      "description": "Short description of the game (plain text, no markup)."
    },
    "version": {
      "type": "string",
      "description": "Game version string (e.g., \"1.2.0\"). Used for update detection in future versions."
    },
    "releaseDate": {
      "type": "string",
      "format": "date",
      "description": "ISO 8601 release date (YYYY-MM-DD)."
    },
    "tags": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Descriptive tags (e.g., genre or features)."
    },
    "coverImage": {
      "type": "string",
      "description": "[Future — Priority 1] Relative path to the cover image on the SD card (JPEG or PNG, recommended 600×900 px)."
    },
    "saveDataPath": {
      "type": "string",
      "description": "[Future — Priority 2] Local file system path where the game stores its save files. Supports environment variable expansion (%APPDATA%, $HOME, ~)."
    },
    "sdSaveDirectory": {
      "type": "string",
      "description": "[Future — Priority 2] Directory on the SD card used to store portable save data. Relative to the manifest file."
    },
    "steamAppId": {
      "type": "integer",
      "minimum": 1,
      "description": "[Future — Priority 1] Steam App ID, used when registering a non-Steam shortcut."
    },
    "installScript": {
      "type": "string",
      "description": "[Future — Priority 1] Relative path to an install script (.ps1 on Windows, .sh on Linux) executed during game installation."
    }
  }
}
```

---

## Field Reference

### MVP Fields

| Field | Type | Required | Description |
|---|---|---|---|
| `schemaVersion` | `string` | ✅ | Must be `"1.0"` |
| `id` | `string` (UUID v4) | ✅ | Unique card identifier. Generate once and never change. |
| `title` | `string` | ✅ | Display name shown in logs and future UI |
| `executable` | `string` | ✅ | Relative path to the game binary. Use forward slashes on both platforms. |
| `workingDirectory` | `string` | | Defaults to the executable's parent directory |
| `arguments` | `string[]` | | CLI arguments passed at launch |
| `stopCombination` | `string[]` | | Button combo to quit the game. Overrides `appsettings.json` default. |
| `preLaunchScript` | `string` or `object` | | Script path to run before launch. Can be shorthand string or `{ "windows": "...", "linux": "..." }`. |
| `preLaunchTimeoutSeconds` | `integer` | | Per-game timeout override for pre-launch script. |
| `preLaunchParams` | `object` | | Script parameters exported as `INSERTPLAY_<KEY>` environment variables. |
| `postLaunchScript` | `string` or `object` | | Script path to run after the game exits. |
| `postLaunchTimeoutSeconds` | `integer` | | Per-game timeout override for post-launch script. |
| `developer` | `string` | | Developer name |
| `publisher` | `string` | | Publisher name |
| `description` | `string` | | Short game description |
| `version` | `string` | | Game version string |
| `releaseDate` | `string` | | ISO 8601 date (YYYY-MM-DD) |
| `tags` | `string[]` | | Descriptive tags |

### Future Fields — Priority 1 (Installation & Steam)

| Field | Type | Description |
|---|---|---|
| `coverImage` | `string` | Relative path to cover image on SD card |
| `steamAppId` | `integer` | Steam App ID for non-Steam shortcut |
| `installScript` | `string` | Relative path to install script |

### Future Fields — Priority 2 (Portable Saves)

| Field | Type | Description |
|---|---|---|
| `saveDataPath` | `string` | Local save path (environment variable expansion supported) |
| `sdSaveDirectory` | `string` | Save directory on the SD card |

---

## Stop Combination Button Names

The `stopCombination` array contains SDL2 game controller button names (case-insensitive):

| Button Name | Description |
|---|---|
| `A` | A / Cross |
| `B` | B / Circle |
| `X` | X / Square |
| `Y` | Y / Triangle |
| `Back` | Back / Select / Share |
| `Guide` | Guide / Home / PS button |
| `Start` | Start / Options |
| `LeftStick` | Left analog stick click (L3) |
| `RightStick` | Right analog stick click (R3) |
| `LeftShoulder` | Left bumper (LB / L1) |
| `RightShoulder` | Right bumper (RB / R1) |
| `DPadUp` | D-pad up |
| `DPadDown` | D-pad down |
| `DPadLeft` | D-pad left |
| `DPadRight` | D-pad right |

> If `stopCombination` is omitted from the manifest, the value from `appsettings.json` (`DefaultStopCombination`) is used as a fallback.

---

## Environment Variable Expansion

The `saveDataPath` field (future) supports the following variable tokens on both platforms:

| Token | Windows example | Linux/SteamOS example |
|---|---|---|
| `%APPDATA%` | `C:\Users\user\AppData\Roaming` | `~/.config` |
| `%LOCALAPPDATA%` | `C:\Users\user\AppData\Local` | `~/.local/share` |
| `%USERPROFILE%` | `C:\Users\user` | `~` |
| `$HOME` | _(not expanded on Windows)_ | `/home/user` |
| `~` | _(not expanded on Windows)_ | `/home/user` |

InsertPlay normalizes path separators for the current OS automatically after expansion.

---

## Script Environment Variables

When pre/post-launch scripts run, InsertPlay provides a base set of environment variables:

- `INSERTPLAY_CARD_PATH`
- `INSERTPLAY_GAME_TITLE`
- `INSERTPLAY_RESOLUTION` (from `preLaunchParams.resolution` or global default)
- `INSERTPLAY_<KEY>` for each entry in `preLaunchParams`

When RetroAchievements credentials are configured, scripts can also receive:

- `INSERTPLAY_RA_USERNAME`
- `INSERTPLAY_RA_PASSWORD`
- `INSERTPLAY_RA_TOKEN` (compatibility variable)
- `INSERTPLAY_RA_LOGIN_TIMESTAMP`

---

## Validation Rules

InsertPlay validates the manifest immediately on card insertion. Validation errors halt launch and are logged.

| Rule | Severity |
|---|---|
| `schemaVersion` is `"1.0"` | Error |
| `id` is a valid UUID v4 format | Error |
| `title` is non-empty | Error |
| `executable` file exists on the SD card | Error |
| `executable` resolves to a file, not a directory | Error |

---

## Complete Example

```json
{
  "schemaVersion": "1.0",
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "title": "Hollow Knight",
  "version": "1.5.78.11833",
  "developer": "Team Cherry",
  "publisher": "Team Cherry",
  "description": "A challenging 2D action-adventure through a vast ruined kingdom of insects and heroes.",
  "releaseDate": "2017-02-24",
  "executable": "bin/hollow_knight.exe",
  "workingDirectory": "bin/",
  "arguments": [],
  "stopCombination": ["Back", "Start"],
  "tags": ["action", "metroidvania", "singleplayer"],
  "coverImage": "assets/cover.jpg",
  "saveDataPath": "%APPDATA%\\Team Cherry\\Hollow Knight\\",
  "sdSaveDirectory": "saves/"
}
```

> `coverImage`, `saveDataPath`, and `sdSaveDirectory` are included here for completeness. They are **silently ignored** by MVP v0.1 and will be used by future versions as those features are implemented.
