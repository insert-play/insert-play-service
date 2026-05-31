using Microsoft.Extensions.Logging;

namespace InsertPlay.Core;

/// <summary>
/// Tracks the lifecycle of the running game process and handles graceful and forced termination.
/// </summary>
public sealed class ProcessManager : IDisposable
{
    private System.Diagnostics.Process? _process;
    private readonly ILogger<ProcessManager> _logger;
    private readonly object _lock = new();

    public event EventHandler? GameExited;

    public bool IsRunning
    {
        get
        {
            lock (_lock)
                return _process is { HasExited: false };
        }
    }

    public ProcessManager(ILogger<ProcessManager> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Begins tracking the given process.
    /// </summary>
    public void Track(System.Diagnostics.Process process)
    {
        lock (_lock)
        {
            _process = process;
            _process.Exited += OnProcessExited;
        }
    }

    /// <summary>
    /// Waits asynchronously for the tracked process to exit.
    /// </summary>
    public async Task WaitForExitAsync(CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Process? p;
        lock (_lock) p = _process;

        if (p is null || p.HasExited)
            return;

        await p.WaitForExitAsync(cancellationToken);
    }

    /// <summary>
    /// Requests termination of the running game. Attempts a graceful close first,
    /// then kills after a short timeout.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        System.Diagnostics.Process? p;
        lock (_lock) p = _process;

        if (p is null || p.HasExited)
            return;

        _logger.LogInformation("Requesting game exit (PID {Pid}).", p.Id);

        try
        {
            p.CloseMainWindow();

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await p.WaitForExitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Game did not exit gracefully. Killing process.");
            try { p.Kill(entireProcessTree: true); } catch { /* already exited */ }
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        System.Diagnostics.Process? p;
        lock (_lock) p = _process;

        if (p is not null)
        {
            var duration = p.ExitTime - p.StartTime;
            _logger.LogInformation(
                "Game exited with code {Code}. Session duration: {Duration}.",
                p.ExitCode, duration);
        }

        GameExited?.Invoke(this, EventArgs.Empty);
        Cleanup();
    }

    private void Cleanup()
    {
        lock (_lock)
        {
            if (_process is not null)
            {
                _process.Exited -= OnProcessExited;
                _process.Dispose();
                _process = null;
            }
        }
    }

    public void Dispose() => Cleanup();
}
