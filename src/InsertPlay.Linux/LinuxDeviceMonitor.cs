using System.Runtime.Versioning;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Linux;

/// <summary>
/// Detects removable media insertion and removal on Linux/SteamOS by watching
/// the standard udev mount directories with <see cref="FileSystemWatcher"/>.
/// </summary>
[SupportedOSPlatform("linux")]
public sealed class LinuxDeviceMonitor : IDeviceMonitor, IDisposable
{
    private const string ManifestFileName = "insertplay.json";

    private readonly ILogger<LinuxDeviceMonitor> _logger;
    private readonly string[] _mediaPaths;
    private readonly List<FileSystemWatcher> _watchers = [];

    // Tracks card paths currently considered "inserted" to suppress duplicate events.
    private readonly HashSet<string> _activePaths = [];
    private readonly object _lock = new();

    public event EventHandler<CardEventArgs>? CardInserted;
    public event EventHandler<CardEventArgs>? CardRemoved;

    public LinuxDeviceMonitor(ILogger<LinuxDeviceMonitor> logger, IOptions<InsertPlayOptions> options)
    {
        _logger = logger;
        _mediaPaths = ExpandUserPaths(options.Value.LinuxMediaPaths);
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Linux device monitor.");

        foreach (var basePath in _mediaPaths)
        {
            if (!Directory.Exists(basePath))
            {
                _logger.LogDebug("Media path {Path} does not exist. Skipping.", basePath);
                continue;
            }

            // Watch the base path for new subdirectory mounts (e.g., /run/media/user/CARD_LABEL/)
            var watcher = new FileSystemWatcher(basePath)
            {
                NotifyFilter = NotifyFilters.DirectoryName,
                IncludeSubdirectories = false,
                EnableRaisingEvents = true,
            };

            watcher.Created += (_, e) => OnDirectoryCreated(e.FullPath);
            watcher.Deleted += (_, e) => OnDirectoryDeleted(e.FullPath);
            _watchers.Add(watcher);

            _logger.LogDebug("Watching {Path} for mount events.", basePath);
        }

        // Check if any cards are already mounted when the service starts
        ScanExistingMounts();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Linux device monitor.");
        foreach (var w in _watchers)
            w.Dispose();
        _watchers.Clear();
        return Task.CompletedTask;
    }

    private void OnDirectoryCreated(string path)
    {
        // The filesystem may not be fully mounted immediately; wait briefly before probing.
        Task.Delay(TimeSpan.FromMilliseconds(500)).ContinueWith(_ =>
        {
            var manifestPath = Path.Combine(path, ManifestFileName);
            if (!File.Exists(manifestPath))
            {
                _logger.LogDebug("Directory {Path} appeared but no manifest found. Ignoring.", path);
                return;
            }

            bool isNew;
            lock (_lock) isNew = _activePaths.Add(path);

            if (isNew)
            {
                _logger.LogInformation("Game card detected at {Path}.", path);
                CardInserted?.Invoke(this, new CardEventArgs(path));
            }
        });
    }

    private void OnDirectoryDeleted(string path)
    {
        bool wasActive;
        lock (_lock) wasActive = _activePaths.Remove(path);

        if (wasActive)
        {
            _logger.LogInformation("Game card removed: {Path}.", path);
            CardRemoved?.Invoke(this, new CardEventArgs(path));
        }
    }

    private void ScanExistingMounts()
    {
        foreach (var basePath in _mediaPaths)
        {
            if (!Directory.Exists(basePath))
                continue;

            foreach (var dir in Directory.EnumerateDirectories(basePath))
            {
                var manifestPath = Path.Combine(dir, ManifestFileName);
                if (File.Exists(manifestPath))
                {
                    bool isNew;
                    lock (_lock) isNew = _activePaths.Add(dir);

                    if (isNew)
                    {
                        _logger.LogInformation("Game card already mounted at {Path}.", dir);
                        CardInserted?.Invoke(this, new CardEventArgs(dir));
                    }
                }
            }
        }
    }

    private static string[] ExpandUserPaths(string[] paths)
    {
        var user = Environment.UserName;
        return paths.Select(p => p.Replace("$USER", user, StringComparison.Ordinal)).ToArray();
    }

    public void Dispose()
    {
        foreach (var w in _watchers)
            w.Dispose();
        _watchers.Clear();
    }
}
