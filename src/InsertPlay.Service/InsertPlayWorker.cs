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
    private readonly IPS2DiscMonitor _ps2DiscMonitor;
    private readonly ManifestParser _manifestParser;
    private readonly Ps2DiscManifestFactory _ps2DiscManifestFactory;
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
    private string? _activeLaunchBasePath;
    private GameManifest? _activeManifest;
    private RetroAchievementsCredentials? _activeRaCredentials;
    private readonly object _stateLock = new();
    private CancellationToken _stoppingToken;

    public InsertPlayWorker(
        IDeviceMonitor deviceMonitor,
        IPS2DiscMonitor ps2DiscMonitor,
        ManifestParser manifestParser,
        Ps2DiscManifestFactory ps2DiscManifestFactory,
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
        _ps2DiscMonitor   = ps2DiscMonitor;
        _manifestParser   = manifestParser;
        _ps2DiscManifestFactory = ps2DiscManifestFactory;
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
        _ps2DiscMonitor.DiscInserted += OnPs2DiscInserted;
        _ps2DiscMonitor.DiscRemoved += OnPs2DiscRemoved;
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
        await _ps2DiscMonitor.StartAsync(stoppingToken);
        _logger.LogInformation("InsertPlay service running. Waiting for SD game cards.");

        // Keep the worker alive until the host requests cancellation.
        await Task.Delay(Timeout.Infinite, stoppingToken).ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

        _logger.LogInformation("InsertPlay service stopping.");
        _controllerInput.StopPolling();
        await _processManager.StopAsync(CancellationToken.None);
        await _ps2DiscMonitor.StopAsync(CancellationToken.None);
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

        await TryLaunchAsync(manifest, e.DrivePath, e.DrivePath);
    }

    private async void OnPs2DiscInserted(object? sender, Ps2DiscEventArgs e)
    {
        _logger.LogInformation("PS2 disc inserted at {Path}.", e.DrivePath);

        if (!_ps2DiscManifestFactory.TryCreateForDrive(
                e.DrivePath,
                out var manifest,
                out var launchBasePath)
            || manifest is null)
        {
            _logger.LogDebug("No PS2 launch manifest produced for drive {Path}.", e.DrivePath);
            return;
        }

        await TryLaunchAsync(manifest, e.DrivePath, launchBasePath);
    }

    private async Task TryLaunchAsync(GameManifest manifest, string mediaPath, string launchBasePath)
    {
        if (_processManager.IsRunning)
        {
            _logger.LogWarning("A game is already running. Ignoring media at {Path}.", mediaPath);
            return;
        }

        var savedCredentials = _credentialStore.Load();
        var runtimeCredentials = await _raSessionProvider.EnrichAsync(savedCredentials, _stoppingToken);

        lock (_stateLock)
        {
            if (_processManager.IsRunning)
            {
                _logger.LogWarning("A game is already running. Ignoring media at {Path}.", mediaPath);
                return;
            }
            _activeCardPath        = mediaPath;
            _activeLaunchBasePath  = launchBasePath;
            _activeManifest        = manifest;
            _activeRaCredentials   = runtimeCredentials;
        }

        var preLaunchOk = await _preLaunchRunner.RunAsync(manifest, launchBasePath, _activeRaCredentials, _stoppingToken);
        if (!preLaunchOk)
        {
            lock (_stateLock)
            {
                _activeCardPath      = null;
                _activeLaunchBasePath = null;
                _activeManifest      = null;
                _activeRaCredentials = null;
            }
            return;
        }

        var process = _gameLauncher.Launch(manifest, launchBasePath);
        if (process is null)
        {
            lock (_stateLock)
            {
                _activeCardPath      = null;
                _activeLaunchBasePath = null;
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
        await HandleMediaRemovedAsync(e.DrivePath);
    }

    private async void OnPs2DiscRemoved(object? sender, Ps2DiscEventArgs e)
    {
        await HandleMediaRemovedAsync(e.DrivePath);
    }

    private async Task HandleMediaRemovedAsync(string mediaPath)
    {
        bool isActiveCard;
        lock (_stateLock) isActiveCard = _activeCardPath == mediaPath;

        if (!isActiveCard)
            return;

        _logger.LogWarning("Active media removed at {Path}. Stopping game.", mediaPath);
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
        string? launchBasePath;
        RetroAchievementsCredentials? raCredentials;

        lock (_stateLock)
        {
            manifest         = _activeManifest;
            cardPath         = _activeCardPath;
            launchBasePath   = _activeLaunchBasePath;
            raCredentials    = _activeRaCredentials;
            _activeCardPath      = null;
            _activeLaunchBasePath = null;
            _activeManifest      = null;
            _activeRaCredentials = null;
        }

        _controllerInput.StopPolling();

        if (manifest is not null && launchBasePath is not null)
            await _postLaunchRunner.RunAsync(manifest, launchBasePath, raCredentials, CancellationToken.None);

        _logger.LogInformation("Game session ended. Waiting for next card insertion.");
    }
}
