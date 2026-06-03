using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Service;

/// <summary>
/// Main background worker. Wires together <see cref="IDeviceMonitor"/>,
/// <see cref="ManifestParser"/>, <see cref="GameLauncher"/>, <see cref="ProcessManager"/>,
/// and <see cref="ControllerInputHandler"/> into the full insert-and-play flow.
/// </summary>
public sealed class InsertPlayWorker : BackgroundService
{
    private readonly IDeviceMonitor _deviceMonitor;
    private readonly ManifestParser _manifestParser;
    private readonly PreLaunchRunner _preLaunchRunner;
    private readonly PostLaunchRunner _postLaunchRunner;
    private readonly RetroAchievementsSessionProvider _raSessionProvider;
    private readonly GameLauncher _gameLauncher;
    private readonly ProcessManager _processManager;
    private readonly ControllerInputHandler _controllerInput;
    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<InsertPlayWorker> _logger;

    // State of the currently running game session.
    private string? _activeCardPath;
    private GameManifest? _activeManifest;
    private RetroAchievementsCredentials? _activeRaCredentials;
    private readonly object _stateLock = new();
    private CancellationToken _stoppingToken;

    public InsertPlayWorker(
        IDeviceMonitor deviceMonitor,
        ManifestParser manifestParser,
        PreLaunchRunner preLaunchRunner,
        PostLaunchRunner postLaunchRunner,
        RetroAchievementsSessionProvider raSessionProvider,
        GameLauncher gameLauncher,
        ProcessManager processManager,
        ControllerInputHandler controllerInput,
        ICredentialStore credentialStore,
        ILogger<InsertPlayWorker> logger)
    {
        _deviceMonitor    = deviceMonitor;
        _manifestParser   = manifestParser;
        _preLaunchRunner  = preLaunchRunner;
        _postLaunchRunner = postLaunchRunner;
        _raSessionProvider = raSessionProvider;
        _gameLauncher     = gameLauncher;
        _processManager   = processManager;
        _controllerInput  = controllerInput;
        _credentialStore  = credentialStore;
        _logger           = logger;

        _deviceMonitor.CardInserted += OnCardInserted;
        _deviceMonitor.CardRemoved += OnCardRemoved;
        _processManager.GameExited += OnGameExited;
        _controllerInput.StopRequested += OnStopRequested;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _stoppingToken = stoppingToken;
        _logger.LogInformation("InsertPlay service starting.");

        // Pre-authenticate on startup to have a session token ready for the first launch.
        await _raSessionProvider.WarmupAsync(stoppingToken);

        await _deviceMonitor.StartAsync(stoppingToken);
        _logger.LogInformation("InsertPlay service running. Waiting for SD game cards.");

        // Keep the worker alive until the host requests cancellation.
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("InsertPlay service stopping.");
        _controllerInput.StopPolling();
        await _processManager.StopAsync(CancellationToken.None);
        await _deviceMonitor.StopAsync(CancellationToken.None);
    }

    private async void OnCardInserted(object? sender, CardEventArgs e)
    {
        _logger.LogInformation("Card inserted at {Path}. Parsing manifest...", e.DrivePath);

        var manifest = await _manifestParser.TryParseAsync(e.DrivePath);
        if (manifest is null)
        {
            _logger.LogWarning("No valid manifest on card at {Path}. Ignoring.", e.DrivePath);
            return;
        }

        var savedCredentials = _credentialStore.Load();
        var runtimeCredentials = await _raSessionProvider.EnrichAsync(savedCredentials, _stoppingToken);

        lock (_stateLock)
        {
            if (_processManager.IsRunning)
            {
                _logger.LogWarning("A game is already running. Ignoring card at {Path}.", e.DrivePath);
                return;
            }
            _activeCardPath        = e.DrivePath;
            _activeManifest        = manifest;
            _activeRaCredentials   = runtimeCredentials;
        }

        var preLaunchOk = await _preLaunchRunner.RunAsync(manifest, e.DrivePath, _activeRaCredentials, _stoppingToken);
        if (!preLaunchOk)
        {
            lock (_stateLock)
            {
                _activeCardPath      = null;
                _activeManifest      = null;
                _activeRaCredentials = null;
            }
            return;
        }

        var process = _gameLauncher.Launch(manifest, e.DrivePath);
        if (process is null)
        {
            lock (_stateLock)
            {
                _activeCardPath      = null;
                _activeManifest      = null;
                _activeRaCredentials = null;
            }
            return;
        }

        _processManager.Track(process);
        _controllerInput.BeginPolling(manifest);
    }

    private async void OnCardRemoved(object? sender, CardEventArgs e)
    {
        bool isActiveCard;
        lock (_stateLock) isActiveCard = _activeCardPath == e.DrivePath;

        if (!isActiveCard)
            return;

        _logger.LogWarning("Active game card removed at {Path}. Stopping game.", e.DrivePath);
        _controllerInput.StopPolling();
        await _processManager.StopAsync(CancellationToken.None);
    }

    private async void OnStopRequested(object? sender, EventArgs e)
    {
        _logger.LogInformation("Stop combination pressed. Stopping game.");
        _controllerInput.StopPolling();
        await _processManager.StopAsync(CancellationToken.None);
    }

    private async void OnGameExited(object? sender, EventArgs e)
    {
        GameManifest? manifest;
        string? cardPath;
        RetroAchievementsCredentials? raCredentials;

        lock (_stateLock)
        {
            manifest         = _activeManifest;
            cardPath         = _activeCardPath;
            raCredentials    = _activeRaCredentials;
            _activeCardPath      = null;
            _activeManifest      = null;
            _activeRaCredentials = null;
        }

        _controllerInput.StopPolling();

        if (manifest is not null && cardPath is not null)
            await _postLaunchRunner.RunAsync(manifest, cardPath, raCredentials, CancellationToken.None);

        _logger.LogInformation("Game session ended. Waiting for next card insertion.");
    }
}
