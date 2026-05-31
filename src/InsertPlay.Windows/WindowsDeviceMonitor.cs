using System.Management;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using InsertPlay.Core;
using Microsoft.Extensions.Logging;

namespace InsertPlay.Windows;

/// <summary>
/// Detects removable media insertion and removal on Windows.
/// Primary strategy: WMI <c>Win32_VolumeChangeEvent</c>.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class WindowsDeviceMonitor : IDeviceMonitor, IDisposable
{
    private const string ManifestFileName = "insertplay.json";

    // EventType 2 = insertion, EventType 3 = removal
    private const string WmiQuery =
        "SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2 OR EventType = 3";

    private readonly ILogger<WindowsDeviceMonitor> _logger;
    private ManagementEventWatcher? _watcher;

    public event EventHandler<CardEventArgs>? CardInserted;
    public event EventHandler<CardEventArgs>? CardRemoved;

    public WindowsDeviceMonitor(ILogger<WindowsDeviceMonitor> logger)
    {
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Windows device monitor (WMI).");

        _watcher = new ManagementEventWatcher(new WqlEventQuery(WmiQuery));
        _watcher.EventArrived += OnWmiEventArrived;
        _watcher.Start();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Windows device monitor.");
        _watcher?.Stop();
        return Task.CompletedTask;
    }

    private void OnWmiEventArrived(object sender, EventArrivedEventArgs e)
    {
        try
        {
            var eventType = Convert.ToUInt16(e.NewEvent["EventType"]);
            var driveName = e.NewEvent["DriveName"]?.ToString();

            if (string.IsNullOrWhiteSpace(driveName))
                return;

            // Normalize to trailing backslash (e.g., "E:\")
            if (!driveName.EndsWith(Path.DirectorySeparatorChar))
                driveName += Path.DirectorySeparatorChar;

            if (eventType == 2)
                HandleInsertion(driveName);
            else if (eventType == 3)
                HandleRemoval(driveName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling WMI volume change event.");
        }
    }

    private void HandleInsertion(string drivePath)
    {
        var manifestPath = Path.Combine(drivePath, ManifestFileName);
        if (!File.Exists(manifestPath))
        {
            _logger.LogDebug("Drive {Drive} inserted but no manifest found. Ignoring.", drivePath);
            return;
        }

        _logger.LogInformation("Game card detected at {Drive}.", drivePath);
        CardInserted?.Invoke(this, new CardEventArgs(drivePath));
    }

    private void HandleRemoval(string drivePath)
    {
        _logger.LogInformation("Drive removed: {Drive}.", drivePath);
        CardRemoved?.Invoke(this, new CardEventArgs(drivePath));
    }

    public void Dispose()
    {
        if (_watcher is not null)
        {
            _watcher.EventArrived -= OnWmiEventArrived;
            _watcher.Dispose();
            _watcher = null;
        }
    }
}
