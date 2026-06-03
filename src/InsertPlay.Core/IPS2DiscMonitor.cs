namespace InsertPlay.Core;

/// <summary>
/// Event arguments raised when a PS2 optical disc is inserted or removed.
/// </summary>
public sealed class Ps2DiscEventArgs : EventArgs
{
    /// <summary>
    /// Optical drive path (for example, "D:\" on Windows).
    /// </summary>
    public string DrivePath { get; }

    public Ps2DiscEventArgs(string drivePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drivePath);
        DrivePath = drivePath;
    }
}

/// <summary>
/// Abstraction over OS-specific PS2 optical disc detection.
/// </summary>
public interface IPS2DiscMonitor
{
    event EventHandler<Ps2DiscEventArgs> DiscInserted;
    event EventHandler<Ps2DiscEventArgs> DiscRemoved;

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
