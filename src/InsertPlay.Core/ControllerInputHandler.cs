using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SDL2;

namespace InsertPlay.Core;

/// <summary>
/// Polls SDL2 for controller input and fires <see cref="StopRequested"/> when
/// all buttons in the configured stop combination are held simultaneously.
/// </summary>
public sealed class ControllerInputHandler : IDisposable
{
    private readonly ILogger<ControllerInputHandler> _logger;
    private readonly InsertPlayOptions _options;

    private CancellationTokenSource? _pollCts;
    private Task? _pollTask;

    public event EventHandler? StopRequested;

    public ControllerInputHandler(ILogger<ControllerInputHandler> logger, IOptions<InsertPlayOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Begins polling for the stop combination defined in <paramref name="manifest"/>
    /// (or the service default if the manifest does not specify one).
    /// </summary>
    public void BeginPolling(GameManifest manifest)
    {
        StopPolling();

        var combo = manifest.StopCombination is { Length: > 0 }
            ? manifest.StopCombination
            : _options.DefaultStopCombination;

        _logger.LogInformation("Controller polling started. Stop combo: [{Combo}]", string.Join(" + ", combo));

        _pollCts = new CancellationTokenSource();
        _pollTask = Task.Run(() => PollLoop(combo, _pollCts.Token));
    }

    /// <summary>Stops the polling loop and releases resources.</summary>
    public void StopPolling()
    {
        if (_pollCts is null)
            return;

        _pollCts.Cancel();
        try { _pollTask?.Wait(TimeSpan.FromSeconds(2)); } catch { /* ignore */ }
        _pollCts.Dispose();
        _pollCts = null;
        _pollTask = null;
    }

    private void PollLoop(string[] combo, CancellationToken ct)
    {
        if (SDL.SDL_Init(SDL.SDL_INIT_GAMECONTROLLER | SDL.SDL_INIT_JOYSTICK) < 0)
        {
            _logger.LogError("SDL_Init failed: {Error}", SDL.SDL_GetError());
            return;
        }

        try
        {
            var buttonIndices = ResolveButtonIndices(combo);
            var interval = TimeSpan.FromMilliseconds(_options.ControllerPollIntervalMs);
            nint controller = nint.Zero;

            while (!ct.IsCancellationRequested)
            {
                SDL.SDL_GameControllerUpdate();

                // Open controller if not yet open
                if (controller == nint.Zero && SDL.SDL_NumJoysticks() > 0)
                {
                    controller = SDL.SDL_GameControllerOpen(0);
                    if (controller != nint.Zero)
                        _logger.LogDebug("Controller opened: {Name}", SDL.SDL_GameControllerName(controller));
                }

                if (controller != nint.Zero && buttonIndices.Length > 0)
                {
                    var allHeld = buttonIndices.All(b =>
                        SDL.SDL_GameControllerGetButton(controller, (SDL.SDL_GameControllerButton)b) == 1);

                    if (allHeld)
                    {
                        _logger.LogInformation("Stop combination detected. Requesting game exit.");
                        StopRequested?.Invoke(this, EventArgs.Empty);
                        return;
                    }
                }

                Thread.Sleep(interval);
            }

            if (controller != nint.Zero)
                SDL.SDL_GameControllerClose(controller);
        }
        finally
        {
            SDL.SDL_Quit();
        }
    }

    private int[] ResolveButtonIndices(string[] combo)
    {
        var indices = new List<int>();
        foreach (var name in combo)
        {
            var button = SDL.SDL_GameControllerGetButtonFromString(name.ToLowerInvariant());
            if (button == SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
            {
                _logger.LogWarning("Unknown controller button name '{Name}'. Ignoring.", name);
                continue;
            }
            indices.Add((int)button);
        }
        return [.. indices];
    }

    public void Dispose()
    {
        StopPolling();
    }
}
