using InsertPlay.Core;
using Microsoft.Extensions.Logging;

namespace InsertPlay.Linux;

/// <summary>
/// V1 Linux placeholder. PS2 optical support is currently Windows-first.
/// </summary>
public sealed class LinuxPs2DiscMonitor : IPS2DiscMonitor
{
    private readonly ILogger<LinuxPs2DiscMonitor> _logger;

#pragma warning disable CS0067 // No-op implementation for v1 Linux scope.
    public event EventHandler<Ps2DiscEventArgs>? DiscInserted;
    public event EventHandler<Ps2DiscEventArgs>? DiscRemoved;
#pragma warning restore CS0067

    public LinuxPs2DiscMonitor(ILogger<LinuxPs2DiscMonitor> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PS2 disc monitor on Linux is not implemented yet (v1 scope).");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
