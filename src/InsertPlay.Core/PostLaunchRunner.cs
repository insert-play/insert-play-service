using System.Diagnostics;
using System.Runtime.InteropServices;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Core;

/// <summary>
/// Executes the post-launch script declared in a game manifest after the game process exits.
/// Unlike <see cref="PreLaunchRunner"/>, failures here are non-fatal and only logged.
/// </summary>
public sealed class PostLaunchRunner
{
    private readonly IOptions<InsertPlayOptions> _options;
    private readonly ILogger<PostLaunchRunner> _logger;

    public PostLaunchRunner(IOptions<InsertPlayOptions> options, ILogger<PostLaunchRunner> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Runs the post-launch script for the given manifest, if one is declared.
    /// Errors are logged but never propagated — the game has already exited.
    /// </summary>
    public async Task RunAsync(
        GameManifest manifest, string cardPath,
        RetroAchievementsCredentials? raCredentials,
        CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        // Honour the same global enabled flag as pre-launch.
        if (!opts.PreLaunch.Enabled)
            return;

        if (manifest.PostLaunchScript is null)
            return;

        var relPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? manifest.PostLaunchScript.Windows
            : manifest.PostLaunchScript.Linux;

        if (string.IsNullOrEmpty(relPath))
            return;

        var scriptPath = Path.Combine(cardPath, relPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning(
                "Post-launch script declared in manifest but not found at {Path}. Skipping.",
                scriptPath);
            return;
        }

        var timeout = manifest.PostLaunchTimeoutSeconds.HasValue
            ? TimeSpan.FromSeconds(manifest.PostLaunchTimeoutSeconds.Value)
            : TimeSpan.FromSeconds(opts.PreLaunch.TimeoutSeconds);

        var env = PreLaunchRunner.BuildEnvironment(manifest, cardPath, opts, raCredentials);
        var (interpreter, arguments) = PreLaunchRunner.ResolveInterpreter(scriptPath);

        _logger.LogInformation(
            "Running post-launch script: {Script} (timeout: {Timeout}s)", scriptPath, timeout.TotalSeconds);

        var psi = new ProcessStartInfo(interpreter, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow  = true,
        };

        foreach (var (key, value) in env)
            psi.Environment[key] = value;

        try
        {
            using var process = Process.Start(psi);
            if (process is null)
            {
                _logger.LogWarning("Failed to start post-launch script process.");
                return;
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(timeout);

            try
            {
                await process.WaitForExitAsync(timeoutCts.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Post-launch script exceeded timeout of {Timeout}s.", timeout.TotalSeconds);
                try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
                return;
            }

            if (process.ExitCode != 0)
                _logger.LogWarning(
                    "Post-launch script exited with code {Code}.", process.ExitCode);
            else
                _logger.LogInformation("Post-launch script completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Post-launch script failed with an exception.");
        }
    }
}
