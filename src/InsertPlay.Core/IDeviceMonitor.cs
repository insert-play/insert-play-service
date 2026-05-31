namespace InsertPlay.Core;

/// <summary>
/// Event arguments raised when an SD game card is inserted or removed.
/// </summary>
public sealed class CardEventArgs : EventArgs
{
    /// <summary>
    /// Root path of the mounted SD card drive (e.g., "E:\" on Windows or "/run/media/user/CARD" on Linux).
    /// </summary>
    public string DrivePath { get; }

    public CardEventArgs(string drivePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(drivePath);
        DrivePath = drivePath;
    }
}

/// <summary>
/// Abstraction over OS-specific removable media detection.
/// Implementations: <c>WindowsDeviceMonitor</c> and <c>LinuxDeviceMonitor</c>.
/// </summary>
public interface IDeviceMonitor
{
    /// <summary>Raised when a removable drive containing an insertplay.json manifest is mounted.</summary>
    event EventHandler<CardEventArgs> CardInserted;

    /// <summary>Raised when a previously detected game card drive is unmounted.</summary>
    event EventHandler<CardEventArgs> CardRemoved;

    Task StartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
