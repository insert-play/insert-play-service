using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;

namespace InsertPlay.Core;

/// <summary>
/// Launches the game process described by a <see cref="GameManifest"/> and hands it off to <see cref="ProcessManager"/>.
/// </summary>
public sealed class GameLauncher
{
    private readonly ILogger<GameLauncher> _logger;

    public GameLauncher(ILogger<GameLauncher> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the game process and returns the running <see cref="System.Diagnostics.Process"/>.
    /// Returns <c>null</c> if the process could not be started.
    /// </summary>
    /// <param name="manifest">Parsed game manifest.</param>
    /// <param name="drivePath">Root path of the SD card, used to resolve relative paths.</param>
    public System.Diagnostics.Process? Launch(GameManifest manifest, string drivePath)
    {
        var executablePath = Path.Combine(drivePath, manifest.Executable.Replace('/', Path.DirectorySeparatorChar));
        executablePath = Path.GetFullPath(executablePath);

        var workingDirectory = string.IsNullOrWhiteSpace(manifest.WorkingDirectory)
            ? Path.GetDirectoryName(executablePath)!
            : Path.GetFullPath(Path.Combine(drivePath, manifest.WorkingDirectory.Replace('/', Path.DirectorySeparatorChar)));

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = executablePath,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
        };

        foreach (var arg in manifest.Arguments)
            psi.ArgumentList.Add(arg);

        _logger.LogInformation("Launching '{Title}' from {Executable}", manifest.Title, executablePath);

        var process = new System.Diagnostics.Process
        {
            StartInfo = psi,
            EnableRaisingEvents = true,
        };

        try
        {
            if (!process.Start())
            {
                _logger.LogError("Process.Start() returned false for '{Title}'.", manifest.Title);
                process.Dispose();
                return null;
            }

            _logger.LogInformation("'{Title}' started (PID {Pid}).", manifest.Title, process.Id);
            return process;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start '{Title}'.", manifest.Title);
            process.Dispose();
            return null;
        }
    }
}
