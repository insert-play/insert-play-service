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
    "LogLevel": "Information"
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
