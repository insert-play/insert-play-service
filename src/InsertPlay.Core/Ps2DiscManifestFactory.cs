using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Core;

/// <summary>
/// Creates a synthetic runtime manifest to boot a physical PS2 disc in PCSX2.
/// </summary>
public sealed class Ps2DiscManifestFactory
{
    private readonly IOptions<InsertPlayOptions> _options;
    private readonly ILogger<Ps2DiscManifestFactory> _logger;

    public Ps2DiscManifestFactory(
        IOptions<InsertPlayOptions> options,
        ILogger<Ps2DiscManifestFactory> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to create a manifest for launching a PS2 disc.
    /// </summary>
    /// <param name="discDrivePath">Detected optical drive path (e.g., D:\).</param>
    /// <param name="manifest">Synthetic launch manifest.</param>
    /// <param name="launchBasePath">Base path used to resolve executable/working directory.</param>
    /// <returns><c>true</c> when a valid local PCSX2 installation is available and feature is enabled.</returns>
    public bool TryCreateForDrive(
        string discDrivePath,
        out GameManifest? manifest,
        out string launchBasePath)
    {
        manifest = null;
        launchBasePath = AppContext.BaseDirectory;

        var opts = _options.Value.PS2Disc;
        if (!opts.Enabled || !opts.AutoLaunch)
            return false;

        var pcsx2Exe = Path.Combine(AppContext.BaseDirectory, "pcsx2", "pcsx2-qt.exe");
        if (opts.RequireLocalPcsx2Folder && !File.Exists(pcsx2Exe))
        {
            _logger.LogDebug(
                "PS2 disc launch skipped: local PCSX2 not found at {Path}.", pcsx2Exe);
            return false;
        }

        manifest = new GameManifest
        {
            SchemaVersion = "1.0",
            Id = Guid.NewGuid().ToString(),
            Title = "PS2 Disc",
            Executable = "pcsx2/pcsx2-qt.exe",
            WorkingDirectory = "pcsx2",
            Arguments =
            [
                "-fullscreen",
                "-disc",
                discDrivePath
            ]
        };

        return true;
    }
}
