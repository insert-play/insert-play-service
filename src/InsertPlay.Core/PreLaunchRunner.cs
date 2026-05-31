using System.Diagnostics;
using System.Runtime.InteropServices;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Core;

/// <summary>
/// Executes the pre-launch script declared in a game manifest before the game process starts.
/// </summary>
public sealed class PreLaunchRunner
{
    private readonly IOptions<InsertPlayOptions> _options;
    private readonly ILogger<PreLaunchRunner> _logger;

    public PreLaunchRunner(IOptions<InsertPlayOptions> options, ILogger<PreLaunchRunner> logger)
    {
        _options = options;
        _logger = logger;
    }

    /// <summary>
    /// Runs the pre-launch script for the given manifest, if one is declared.
    /// </summary>
    /// <param name="manifest">The parsed game manifest.</param>
    /// <param name="cardPath">Root path of the mounted SD card.</param>
    /// <param name="cancellationToken">Host shutdown token.</param>
    /// <returns>
    /// <c>true</c> if the script succeeded (or was skipped); <c>false</c> if it failed or
    /// timed out — the caller should abort the game launch in that case.
    /// </returns>
    public async Task<bool> RunAsync(
        GameManifest manifest, string cardPath, CancellationToken cancellationToken)
    {
        var opts = _options.Value;

        if (!opts.PreLaunch.Enabled)
            return true;

        if (manifest.PreLaunchScript is null)
            return true;

        var relPath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? manifest.PreLaunchScript.Windows
            : manifest.PreLaunchScript.Linux;

        if (string.IsNullOrEmpty(relPath))
            return true;

        var scriptPath = Path.Combine(cardPath, relPath.Replace('/', Path.DirectorySeparatorChar));

        if (!File.Exists(scriptPath))
        {
            _logger.LogWarning(
                "Pre-launch script declared in manifest but not found at {Path}. Skipping.",
                scriptPath);
            return true; // missing script is non-fatal
        }

        var timeout = manifest.PreLaunchTimeoutSeconds.HasValue
            ? TimeSpan.FromSeconds(manifest.PreLaunchTimeoutSeconds.Value)
            : TimeSpan.FromSeconds(opts.PreLaunch.TimeoutSeconds);

        var env = BuildEnvironment(manifest, cardPath, opts);
        var (interpreter, arguments) = ResolveInterpreter(scriptPath);

        _logger.LogInformation(
            "Running pre-launch script: {Script} (timeout: {Timeout}s)", scriptPath, timeout.TotalSeconds);

        var psi = new ProcessStartInfo(interpreter, arguments)
        {
            UseShellExecute = false,
            CreateNoWindow  = true,
        };

        foreach (var (key, value) in env)
            psi.Environment[key] = value;

        using var process = Process.Start(psi);
        if (process is null)
        {
            _logger.LogError("Failed to start pre-launch script process.");
            return false;
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);

        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogError(
                "Pre-launch script exceeded timeout of {Timeout}s. Aborting game launch.", timeout.TotalSeconds);
            try { process.Kill(entireProcessTree: true); } catch { /* best-effort */ }
            return false;
        }

        if (process.ExitCode != 0)
        {
            _logger.LogError(
                "Pre-launch script exited with code {Code}. Aborting game launch.", process.ExitCode);
            return false;
        }

        _logger.LogInformation("Pre-launch script completed successfully.");
        return true;
    }

    public static (string interpreter, string arguments) ResolveInterpreter(string scriptPath)
    {
        var ext = Path.GetExtension(scriptPath).ToLowerInvariant();
        return ext switch
        {
            ".ps1"          => ("powershell", $"-ExecutionPolicy Bypass -File \"{scriptPath}\""),
            ".bat" or ".cmd"=> ("cmd",        $"/c \"{scriptPath}\""),
            ".sh"           => ("bash",       $"\"{scriptPath}\""),
            _               => (scriptPath,   string.Empty), // treat as a direct executable
        };
    }

    private static Dictionary<string, string> BuildEnvironment(
        GameManifest manifest, string cardPath, InsertPlayOptions opts)
    {
        var env = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["INSERTPLAY_CARD_PATH"]   = cardPath,
            ["INSERTPLAY_GAME_TITLE"]  = manifest.Title,
        };

        // Resolution: manifest params take precedence over the global default
        var resolution = manifest.PreLaunchParams is not null
                         && manifest.PreLaunchParams.TryGetValue("resolution", out var r)
            ? r
            : opts.PreLaunch.DefaultResolution;

        env["INSERTPLAY_RESOLUTION"] = resolution;

        // Forward all preLaunchParams as INSERTPLAY_<KEY> (uppercased)
        if (manifest.PreLaunchParams is not null)
        {
            foreach (var (key, value) in manifest.PreLaunchParams)
                env[$"INSERTPLAY_{key.ToUpperInvariant()}"] = value;
        }

        return env;
    }
}
