using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace InsertPlay.Core.Tests;

public class PreLaunchRunnerTests
{
    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private static PreLaunchRunner BuildRunner(Action<InsertPlayOptions>? configure = null)
    {
        var opts = new InsertPlayOptions();
        configure?.Invoke(opts);
        return new PreLaunchRunner(
            Options.Create(opts),
            NullLogger<PreLaunchRunner>.Instance);
    }

    private static GameManifest MinimalManifest(PreLaunchScriptSpec? script = null,
        Dictionary<string, string>? preLaunchParams = null,
        int? timeoutSeconds = null) => new()
    {
        SchemaVersion        = "1.0",
        Id                   = "550e8400-e29b-41d4-a716-446655440000",
        Title                = "Test Game",
        Executable           = "game.exe",
        PreLaunchScript      = script,
        PreLaunchParams      = preLaunchParams,
        PreLaunchTimeoutSeconds = timeoutSeconds,
    };

    // Creates a temp directory with a tiny native script that exits with the given code.
    private static (string tempDir, string scriptPath) CreateTempScript(int exitCode)
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(dir);

        string scriptPath;
        if (OperatingSystem.IsWindows())
        {
            scriptPath = Path.Combine(dir, "prelaunch.bat");
            File.WriteAllText(scriptPath, $"@echo off\r\nexit {exitCode}\r\n");
        }
        else
        {
            scriptPath = Path.Combine(dir, "prelaunch.sh");
            File.WriteAllText(scriptPath, $"#!/usr/bin/env bash\nexit {exitCode}\n");
            // Ensure executable bit on Linux
            System.Diagnostics.Process.Start("chmod", $"+x \"{scriptPath}\"")?.WaitForExit();
        }

        return (dir, scriptPath);
    }

    private static PreLaunchScriptSpec PlatformScript(string scriptPath) =>
        OperatingSystem.IsWindows()
            ? new PreLaunchScriptSpec { Windows = Path.GetFileName(scriptPath) }
            : new PreLaunchScriptSpec { Linux   = Path.GetFileName(scriptPath) };

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenPreLaunchDisabled()
    {
        var runner = BuildRunner(o => o.PreLaunch.Enabled = false);
        // Even with a bogus script spec, should immediately return true
        var spec     = new PreLaunchScriptSpec { Windows = "nonexistent.ps1", Linux = "nonexistent.sh" };
        var manifest = MinimalManifest(spec);

        var result = await runner.RunAsync(manifest, Path.GetTempPath(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenNoScriptDeclared()
    {
        var runner   = BuildRunner();
        var manifest = MinimalManifest(script: null);

        var result = await runner.RunAsync(manifest, Path.GetTempPath(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenCurrentPlatformKeyIsAbsent()
    {
        var runner = BuildRunner();
        // Provide only the other-platform key — current platform key is null
        var spec = OperatingSystem.IsWindows()
            ? new PreLaunchScriptSpec { Linux   = "scripts/configure.sh" }
            : new PreLaunchScriptSpec { Windows = "scripts/configure.ps1" };
        var manifest = MinimalManifest(spec);

        var result = await runner.RunAsync(manifest, Path.GetTempPath(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenScriptFileIsMissing()
    {
        var runner   = BuildRunner();
        var spec     = new PreLaunchScriptSpec { Windows = "missing.bat", Linux = "missing.sh" };
        var manifest = MinimalManifest(spec);

        var result = await runner.RunAsync(manifest, Path.GetTempPath(), CancellationToken.None);

        // Missing script is non-fatal — service logs a warning and continues
        Assert.True(result);
    }

    [Fact]
    public async Task RunAsync_ReturnsTrue_WhenScriptExitsWithZero()
    {
        var (tempDir, scriptPath) = CreateTempScript(exitCode: 0);
        try
        {
            var runner   = BuildRunner();
            var spec     = PlatformScript(scriptPath);
            var manifest = MinimalManifest(spec);

            var result = await runner.RunAsync(manifest, tempDir, CancellationToken.None);

            Assert.True(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task RunAsync_ReturnsFalse_WhenScriptExitsWithNonZero()
    {
        var (tempDir, scriptPath) = CreateTempScript(exitCode: 1);
        try
        {
            var runner   = BuildRunner();
            var spec     = PlatformScript(scriptPath);
            var manifest = MinimalManifest(spec);

            var result = await runner.RunAsync(manifest, tempDir, CancellationToken.None);

            Assert.False(result);
        }
        finally
        {
            Directory.Delete(tempDir, recursive: true);
        }
    }

    // -------------------------------------------------------------------------
    // ResolveInterpreter (static, platform-agnostic)
    // -------------------------------------------------------------------------

    [Theory]
    [InlineData("setup.ps1",  "powershell")]
    [InlineData("setup.bat",  "cmd")]
    [InlineData("setup.cmd",  "cmd")]
    [InlineData("setup.sh",   "bash")]
    public void ResolveInterpreter_ReturnsCorrectInterpreter(string filename, string expectedInterpreter)
    {
        var (interpreter, _) = PreLaunchRunner.ResolveInterpreter(filename);

        Assert.Equal(expectedInterpreter, interpreter);
    }

    [Fact]
    public void ResolveInterpreter_TreatsUnknownExtensionAsDirectExecutable()
    {
        var (interpreter, args) = PreLaunchRunner.ResolveInterpreter("/opt/game/prelaunch");

        Assert.Equal("/opt/game/prelaunch", interpreter);
        Assert.Equal(string.Empty, args);
    }
}
