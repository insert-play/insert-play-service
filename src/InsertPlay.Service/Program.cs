using System.Runtime.InteropServices;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using InsertPlay.Linux;
using InsertPlay.Service;
using InsertPlay.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args);

// Enable Windows Service or Linux systemd lifetime as appropriate
if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    builder.UseWindowsService(options => options.ServiceName = "InsertPlay");
else
    builder.UseSystemd();

builder.ConfigureServices((context, services) =>
{
    // Bind strongly-typed options
    services.Configure<InsertPlayOptions>(
        context.Configuration.GetSection(InsertPlayOptions.SectionName));

    // Register the OS-appropriate device monitor
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        services.AddSingleton<IDeviceMonitor, WindowsDeviceMonitor>();
    else
        services.AddSingleton<IDeviceMonitor, LinuxDeviceMonitor>();

    // Register Core services
    services.AddSingleton<ManifestParser>();
    services.AddSingleton<GameLauncher>();
    services.AddSingleton<ProcessManager>();
    services.AddSingleton<ControllerInputHandler>();

    // Register the main worker
    services.AddHostedService<InsertPlayWorker>();
});

var host = builder.Build();
await host.RunAsync();
