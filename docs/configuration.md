# Configuration

InsertPlay is configured via `appsettings.json`, located next to the service executable. Environment-specific overrides can be placed in `appsettings.Production.json` or `appsettings.Development.json`.

---

## File Location

| Platform | Default Path |
|---|---|
| Windows | `<install-dir>\appsettings.json` |
| Linux / SteamOS | `<install-dir>/appsettings.json` or `/etc/insertplay/appsettings.json` |

---

## Full Example

```json
{
  "InsertPlay": {
    "DetectionMethod": "Auto",
    "DefaultStopCombination": ["Back", "Start"],
    "ControllerPollIntervalMs": 50,
    "LinuxMediaPaths": [
      "/media",
      "/run/media/$USER"
    ],
    "LogLevel": "Information",
    "PreLaunch": {
      "Enabled": true,
      "TimeoutSeconds": 30,
      "DefaultResolution": "native"
    },
    "PS2Disc": {
      "Enabled": true,
      "AutoLaunch": true,
      "RequireLocalPcsx2Folder": true
    }
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

---

## Options Reference

### `InsertPlay` Section

| Key | Type | Default | Description |
|---|---|---|---|
| `DetectionMethod` | `string` | `"Auto"` | SD card detection backend. See [Detection Methods](#detection-methods). |
| `DefaultStopCombination` | `string[]` | `["Back", "Start"]` | Button combination to quit the game when no `stopCombination` is defined in the manifest. See [button names](manifest-spec.md#stop-combination-button-names). |
| `ControllerPollIntervalMs` | `integer` | `50` | How often (in milliseconds) the controller state is polled. Lower values increase responsiveness at the cost of slightly higher CPU usage. |
| `LinuxMediaPaths` | `string[]` | `["/media", "/run/media/$USER"]` | Directories watched for SD card mount events on Linux. Add custom paths if your distribution mounts removable drives elsewhere. `$USER` is expanded at startup. |
| `LogLevel` | `string` | `"Information"` | Minimum log level: `Trace`, `Debug`, `Information`, `Warning`, `Error`, `Critical`. |
| `PreLaunch.Enabled` | `bool` | `true` | Globally enables or disables pre-launch and post-launch scripts. |
| `PreLaunch.TimeoutSeconds` | `integer` | `30` | Default timeout used by pre/post-launch scripts when the manifest does not override it. |
| `PreLaunch.DefaultResolution` | `string` | `"native"` | Default `INSERTPLAY_RESOLUTION` value forwarded to scripts when no manifest param overrides it. |
| `PS2Disc.Enabled` | `bool` | `true` | Enables the PS2 optical disc monitoring module. |
| `PS2Disc.AutoLaunch` | `bool` | `true` | Auto-launches a detected PS2 disc when no game is already running. |
| `PS2Disc.RequireLocalPcsx2Folder` | `bool` | `true` | Requires `pcsx2/pcsx2-qt.exe` to exist under the service base directory before enabling PS2 disc launch. |

---

## PS2 Optical Disc Support

PS2 optical support is currently Windows-first:

- Windows: implemented (polling optical drives, looking for `SYSTEM.CNF`)
- Linux/SteamOS: placeholder monitor in v1 (no auto-launch yet)

When enabled, InsertPlay creates a synthetic runtime manifest and launches local PCSX2 with `-fullscreen -disc <drive>`.

---

## RetroAchievements Credentials

RetroAchievements credentials are not configured in `appsettings.json`.

- On Windows, credentials are set from the tray UI (`Conta RetroAchievements...`).
- Credentials are persisted under `%APPDATA%\\InsertPlay\\ra-credentials.bin`.
- On Windows this file is DPAPI-protected (current user scope).
- On Linux builds, the file is plain JSON with restrictive file permissions.

At runtime, InsertPlay logs in to RetroAchievements (`r=login2`) and caches a session token in memory. When pre/post-launch scripts run, these variables may be available:

- `INSERTPLAY_RA_USERNAME`
- `INSERTPLAY_RA_PASSWORD`
- `INSERTPLAY_RA_TOKEN` (compatibility variable)
- `INSERTPLAY_RA_LOGIN_TIMESTAMP`

### Future Options (Reserved — Not Active in v0.1)

The following options are reserved for future features and have no effect in the current version.

| Key | Type | Description |
|---|---|---|
| `InstallDirectory` | `string` | Local directory where games are installed _(Priority 1)_ |
| `SteamUserDataPath` | `string` | Path to Steam's `userdata` folder for shortcut registration _(Priority 1)_ |
| `SaveSync.Enabled` | `bool` | Enable or disable portable save sync globally _(Priority 2)_ |
| `SaveSync.ConflictResolution` | `string` | Strategy when both SD and local saves exist: `newest-wins`, `sd-wins`, `local-wins` _(Priority 2)_ |

---

## Detection Methods

| Value | Platform | Description |
|---|---|---|
| `"Auto"` | Both | Automatically selects the best available method for the current OS. **Recommended.** |
| `"WinDeviceNotification"` | Windows | Uses `RegisterDeviceNotification` via a hidden native window. Lowest latency (~100 ms). |
| `"WinWmi"` | Windows | Uses WMI `Win32_VolumeChangeEvent`. Compatible with all service contexts. ~1–2 s latency. |
| `"LinuxFileSystemWatcher"` | Linux | Watches configured media mount paths with `FileSystemWatcher`. Default on Linux. |
| `"Polling"` | Both | Polls `DriveInfo.GetDrives()` every second. Useful fallback for unusual environments or during development. |

---

## Logging

InsertPlay uses the standard .NET `ILogger` abstraction. Logs are written to:

- **Console (stdout)** — always enabled
- **Windows Event Log** — when running as a Windows Service (requires the `Microsoft.Extensions.Logging.EventLog` package)
- **systemd journal** — when running under systemd on Linux (captured via stdout automatically)

The base package does not bundle a file logging provider. To enable file logging, add a provider such as [Serilog](https://serilog.net/) or [NLog](https://nlog-project.org/) and configure it in `appsettings.json`.

---

## Development Override

When running locally with `dotnet run`, create an `appsettings.Development.json` file alongside `appsettings.json`. It is merged on top of the base config and takes precedence. This file should not be committed to source control.

Example for local development:

```json
{
  "InsertPlay": {
    "DetectionMethod": "Polling",
    "LogLevel": "Debug"
  }
}
```

Using `Polling` detection is recommended during development to avoid requiring elevated permissions or a message pump.
