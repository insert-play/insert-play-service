using System.Runtime.InteropServices;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using InsertPlay.Service;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;

#if WINDOWS
using InsertPlay.Service.Windows;
using InsertPlay.Windows;
#else
using InsertPlay.Linux;
#endif

namespace InsertPlay.Service;

internal static class Program
{
#if WINDOWS
    [System.STAThread]
    private static void Main(string[] args)
    {
        System.Windows.Forms.Application.EnableVisualStyles();
        System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

        var host = BuildHost(args);
        host.StartAsync().GetAwaiter().GetResult();
        var credentialStore = host.Services.GetRequiredService<ICredentialStore>();
        System.Windows.Forms.Application.Run(new TrayApplicationContext(host, credentialStore));
        host.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
    }
#else
    private static async Task Main(string[] args)
    {
        var host = BuildHost(args);
        await host.RunAsync();
    }
#endif

    private static IHost BuildHost(string[] args)
    {
        var builder = Host.CreateDefaultBuilder(args);

        builder.UseSerilog((_, lc) =>
        {
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "insertplay-.log");

            lc.MinimumLevel.Information()
              .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
              .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
              .Enrich.FromLogContext()
              .WriteTo.File(
                  logPath,
                  rollingInterval: RollingInterval.Day,
                  retainedFileCountLimit: 7,
                  outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");

#if WINDOWS
            lc.WriteTo.Sink(InMemoryLogSink.Instance);
#else
            lc.WriteTo.Console(
                outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}");
#endif
        });

        builder.ConfigureServices((context, services) =>
        {
            services.Configure<InsertPlayOptions>(
                context.Configuration.GetSection(InsertPlayOptions.SectionName));

#if WINDOWS
            services.AddSingleton<IDeviceMonitor, WindowsDeviceMonitor>();
#else
#pragma warning disable CA1416 // LinuxDeviceMonitor is linux-only; this branch only runs on Linux
            services.AddSingleton<IDeviceMonitor, LinuxDeviceMonitor>();
#pragma warning restore CA1416
#endif

            services.AddSingleton<ICredentialStore, CredentialStore>();
            services.AddSingleton<RetroAchievementsSessionProvider>();
            services.AddSingleton<ManifestParser>();
            services.AddSingleton<PreLaunchRunner>();
            services.AddSingleton<PostLaunchRunner>();
            services.AddSingleton<GameLauncher>();
            services.AddSingleton<ProcessManager>();
            services.AddSingleton<ControllerInputHandler>();
            services.AddHostedService<InsertPlayWorker>();
        });

        return builder.Build();
    }
}

