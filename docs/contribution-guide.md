# Contribution Guide

Thank you for contributing to InsertPlay. This guide covers how to set up your development environment, build the project, and follow our coding and contribution standards.

---

## Prerequisites

### All Platforms

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (8.0.x or later)
- Git
- SDL2 runtime library (required at runtime for controller input)

### Windows

- Windows 10 version 1903 or later
- Visual Studio 2022 (17.8+), JetBrains Rider 2024.1+, or VS Code with the C# Dev Kit extension
- SDL2 native runtime — obtained automatically via the setup script below (see [Getting Started](#getting-started))

### Linux / SteamOS

- GCC or Clang (for native interop compilation if needed)
- `libsdl2` development package

```bash
# Ubuntu / Debian
sudo apt install libsdl2-dev

# Arch Linux / SteamOS
sudo pacman -S sdl2

# Fedora
sudo dnf install SDL2-devel
```

---

## Getting Started

```bash
# Clone the repository
git clone https://github.com/your-org/insert-play-service.git
cd insert-play-service

# Restore dependencies
dotnet restore
```

**Windows only — download the SDL2 native runtime:**

```powershell
# Run once after cloning. Downloads SDL2.dll from the official SDL2 GitHub releases
# and places it at src/InsertPlay.Service/native/win-x64/SDL2.dll.
# The build system copies it automatically to the output directory.
powershell -ExecutionPolicy Bypass -File tools/get-sdl2-native.ps1
```

On **Linux/SteamOS**, `libSDL2.so` is provided by the system package manager (see [Prerequisites](#prerequisites)) and is resolved automatically at runtime — no extra step needed.

```bash
# Build (Debug)
dotnet build

# Build (Release)
dotnet build -c Release
```

---

## Running Locally

InsertPlay can be run directly without installing as a service, which is the recommended approach during development:

```bash
dotnet run --project src/InsertPlay.Service
```

By default it will use `appsettings.Development.json` if present. Create one to override settings for local development:

```json
{
  "InsertPlay": {
    "DetectionMethod": "Polling",
    "LogLevel": "Debug"
  }
}
```

Using `Polling` detection is recommended during development to avoid requiring elevated permissions or a native message pump.

---

## Project Structure

```
insert-play-service/
├── src/
│   ├── InsertPlay.Core/             # Platform-agnostic business logic
│   │   ├── IDeviceMonitor.cs
│   │   ├── ManifestParser.cs
│   │   ├── GameLauncher.cs
│   │   ├── ProcessManager.cs
│   │   ├── ControllerInputHandler.cs
│   │   ├── Models/
│   │   │   ├── GameManifest.cs
│   │   │   └── InsertPlayOptions.cs
│   │   └── SDL2/                    # Vendored SDL2-CS source files
│   ├── InsertPlay.Windows/          # Windows-specific implementations
│   │   └── WindowsDeviceMonitor.cs
│   ├── InsertPlay.Linux/            # Linux-specific implementations
│   │   └── LinuxDeviceMonitor.cs
│   └── InsertPlay.Service/          # Service host entry point
│       ├── Program.cs
│       ├── InsertPlayWorker.cs
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── native/win-x64/          # SDL2.dll (git-ignored, run get-sdl2-native.ps1)
├── tests/
│   └── InsertPlay.Core.Tests/       # Unit tests for Core logic
├── deploy/
│   └── insertplay.service           # systemd unit file (Linux)
├── examples/
│   ├── nfs-most-wanted/insertplay.json
│   └── nfs-underground/insertplay.json
├── tools/
│   └── get-sdl2-native.ps1          # Downloads SDL2.dll for Windows
├── docs/                            # Documentation
├── README.md
├── CHANGELOG.md
└── insert-play-service.sln
```

---

## Running Tests

```bash
# Run all tests
dotnet test

# Run with code coverage
dotnet test --collect:"XPlat Code Coverage"

# Run a specific test project
dotnet test tests/InsertPlay.Core.Tests
```

---

## Installing as a Service (Development)

### Windows

```powershell
# Build Release first
dotnet build -c Release src/InsertPlay.Service

# Install as a Windows Service
$exePath = Resolve-Path "src/InsertPlay.Service/bin/Release/net8.0/InsertPlay.Service.exe"
sc.exe create InsertPlayService binpath="$exePath"
sc.exe start InsertPlayService

# View logs (Windows Event Viewer or journal)
Get-EventLog -LogName Application -Source InsertPlayService -Newest 20

# Uninstall
sc.exe stop InsertPlayService
sc.exe delete InsertPlayService
```

### Linux / SteamOS

```bash
# Copy the systemd unit file
sudo cp deploy/insertplay.service /etc/systemd/system/

# Edit ExecStart to point to your build output
sudo nano /etc/systemd/system/insertplay.service

sudo systemctl daemon-reload

# Start and enable on boot
sudo systemctl enable --now insertplay

# View logs
journalctl -u insertplay -f

# Stop and disable
sudo systemctl disable --now insertplay
```

**Example `insertplay.service`:**

```ini
[Unit]
Description=InsertPlay — SD game card auto-launcher
After=network.target

[Service]
Type=simple
ExecStart=/opt/insertplay/InsertPlay.Service
Restart=on-failure
RestartSec=5
User=%i

[Install]
WantedBy=default.target
```

---

## Coding Standards

### General

- Target .NET 8 and use C# 12 language features where appropriate.
- Prefer `async`/`await` over raw `Task.ContinueWith` or `Thread`.
- Use `ILogger<T>` for all logging — no `Console.WriteLine` in library code.
- Keep platform-specific code strictly within `InsertPlay.Windows` or `InsertPlay.Linux`. Never place `RuntimeInformation.IsOSPlatform` checks inside `InsertPlay.Core`.
- Always `Dispose()` `Process` handles and unmanaged SDL2 resources.

### Naming

- Follow [Microsoft's C# naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/naming-conventions).
- Interfaces: `IFoo`. Implementations: `FooService`, `WindowsFoo`, `LinuxFoo`.
- Async methods: suffix with `Async`.

### Testing

- Unit tests go in `InsertPlay.Core.Tests`.
- Use [xUnit](https://xunit.net/) as the test framework and [Moq](https://github.com/devlooped/moq) for mocking.
- Test filenames mirror source files: `ManifestParser.cs` → `ManifestParserTests.cs`.
- `ManifestParser` and `ControllerInputHandler` (pure logic) are the primary coverage targets.

---

## Pull Request Guidelines

1. **Fork** the repository and create a branch from `main`:
   ```bash
   git checkout -b feat/my-feature
   ```
2. **Write tests** for any new logic added to `InsertPlay.Core`.
3. **Update documentation** in `docs/` if you're changing behavior, adding configuration options, or modifying the manifest schema.
4. **Update `CHANGELOG.md`** under `[Unreleased]` with a brief description of your change.
5. Open a PR against `main` and fill out the PR template.
6. Ensure `dotnet build` and `dotnet test` pass on **both Windows and Linux** before requesting review.
