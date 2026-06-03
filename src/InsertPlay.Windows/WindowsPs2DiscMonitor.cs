using System.Runtime.Versioning;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace InsertPlay.Windows;

[SupportedOSPlatform("windows")]
public sealed class WindowsPs2DiscMonitor : IPS2DiscMonitor, IDisposable
{
    private readonly IOptions<InsertPlayOptions> _options;
    private readonly ILogger<WindowsPs2DiscMonitor> _logger;

    private readonly HashSet<string> _knownDiscs =
        new(StringComparer.OrdinalIgnoreCase);

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cts;
    private Task? _monitorTask;

    public event EventHandler<Ps2DiscEventArgs>? DiscInserted;
    public event EventHandler<Ps2DiscEventArgs>? DiscRemoved;

    public WindowsPs2DiscMonitor(
        IOptions<InsertPlayOptions> options,
        ILogger<WindowsPs2DiscMonitor> logger)
    {
        _options = options;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var opts = _options.Value.PS2Disc;

        if (!opts.Enabled)
        {
            _logger.LogInformation(
                "PS2 disc monitor disabled by configuration.");
            return Task.CompletedTask;
        }

        var pcsx2Exe = Path.Combine(
            AppContext.BaseDirectory,
            "pcsx2",
            "pcsx2-qt.exe");

        if (opts.RequireLocalPcsx2Folder && !File.Exists(pcsx2Exe))
        {
            _logger.LogInformation(
                "PS2 disc monitor not started because local PCSX2 was not found at {Path}.",
                pcsx2Exe);

            return Task.CompletedTask;
        }

        _logger.LogInformation(
            "Starting Windows PS2 disc monitor (polling mode).");

        _cts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken);

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

        _monitorTask = MonitorLoopAsync(_cts.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Stopping Windows PS2 disc monitor.");

        if (_cts is not null)
        {
            await _cts.CancelAsync();
        }

        if (_monitorTask is not null)
        {
            try
            {
                await _monitorTask;
            }
            catch (OperationCanceledException)
            {
            }
        }

        _timer?.Dispose();
        _timer = null;
    }

    private async Task MonitorLoopAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(cancellationToken))
            {
                ScanDrives();
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected error in PS2 disc monitor.");
        }
    }

    private void ScanDrives()
    {
        HashSet<string> currentDiscs =
            new(StringComparer.OrdinalIgnoreCase);

        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                if (drive.DriveType != DriveType.CDRom)
                    continue;

                if (!drive.IsReady)
                    continue;

                var drivePath = drive.RootDirectory.FullName;

                if (!LooksLikePs2Disc(drivePath))
                    continue;

                currentDiscs.Add(drivePath);

                if (_knownDiscs.Add(drivePath))
                {
                    _logger.LogInformation(
                        "PS2 disc detected at {Drive}.",
                        drivePath);

                    DiscInserted?.Invoke(
                        this,
                        new Ps2DiscEventArgs(drivePath));
                }
            }
            catch
            {
                // Drive may become unavailable during scan.
            }
        }

        var removedDiscs =
            _knownDiscs.Except(currentDiscs).ToList();

        foreach (var drivePath in removedDiscs)
        {
            _knownDiscs.Remove(drivePath);

            _logger.LogInformation(
                "PS2 disc removed from {Drive}.",
                drivePath);

            DiscRemoved?.Invoke(
                this,
                new Ps2DiscEventArgs(drivePath));
        }
    }

    private static bool LooksLikePs2Disc(
        string drivePath)
    {
        try
        {
            return File.Exists(
                       Path.Combine(drivePath, "SYSTEM.CNF"))
                   ||
                   File.Exists(
                       Path.Combine(drivePath, "system.cnf"));
        }
        catch
        {
            return false;
        }
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _timer?.Dispose();
    }
}