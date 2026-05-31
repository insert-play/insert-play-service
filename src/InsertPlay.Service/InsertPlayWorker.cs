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
    private readonly GameLauncher _gameLauncher;
    private readonly ProcessManager _processManager;
    private readonly ControllerInputHandler _controllerInput;
    private readonly ILogger<InsertPlayWorker> _logger;

    // Track which drive path the currently running game was launched from.
    private string? _activeCardPath;
    private readonly object _stateLock = new();

    public InsertPlayWorker(
        IDeviceMonitor deviceMonitor,
        ManifestParser manifestParser,
        GameLauncher gameLauncher,
        ProcessManager processManager,
        ControllerInputHandler controllerInput,
        ILogger<InsertPlayWorker> logger)
    {
        _deviceMonitor = deviceMonitor;
        _manifestParser = manifestParser;
        _gameLauncher = gameLauncher;
        _processManager = processManager;
        _controllerInput = controllerInput;
        _logger = logger;

        _deviceMonitor.CardInserted += OnCardInserted;
        _deviceMonitor.CardRemoved += OnCardRemoved;
        _processManager.GameExited += OnGameExited;
        _controllerInput.StopRequested += OnStopRequested;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("InsertPlay service starting.");
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

        lock (_stateLock)
        {
            if (_processManager.IsRunning)
            {
                _logger.LogWarning("A game is already running. Ignoring card at {Path}.", e.DrivePath);
                return;
            }
            _activeCardPath = e.DrivePath;
        }

        var process = _gameLauncher.Launch(manifest, e.DrivePath);
        if (process is null)
        {
            lock (_stateLock) _activeCardPath = null;
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

    private void OnGameExited(object? sender, EventArgs e)
    {
        lock (_stateLock) _activeCardPath = null;
        _controllerInput.StopPolling();
        _logger.LogInformation("Game session ended. Waiting for next card insertion.");
    }
}
