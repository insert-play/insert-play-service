namespace InsertPlay.Core.Models;

/// <summary>
/// Strongly-typed options bound from the "InsertPlay" section of appsettings.json.
/// </summary>
public sealed class InsertPlayOptions
{
    public const string SectionName = "InsertPlay";

    /// <summary>
    /// SD card detection backend.
    /// Allowed values: Auto, WinDeviceNotification, WinWmi, LinuxFileSystemWatcher, Polling.
    /// Default: Auto.
    /// </summary>
    public string DetectionMethod { get; set; } = "Auto";

    /// <summary>
    /// Button combination used to quit the game when the manifest does not specify one.
    /// Default: ["Back", "Start"].
    /// </summary>
    public string[] DefaultStopCombination { get; set; } = ["Back", "Start"];

    /// <summary>
    /// How often (in milliseconds) the controller state is polled.
    /// Default: 50 ms.
    /// </summary>
    public int ControllerPollIntervalMs { get; set; } = 50;

    /// <summary>
    /// Directories watched for SD card mount events on Linux.
    /// $USER is expanded at startup.
    /// Default: ["/media", "/run/media/$USER"].
    /// </summary>
    public string[] LinuxMediaPaths { get; set; } = ["/media", "/run/media/$USER"];

    /// <summary>
    /// Minimum log level written to the console output.
    /// Default: Information.
    /// </summary>
    public string LogLevel { get; set; } = "Information";

    /// <summary>Options controlling pre-launch script execution.</summary>
    public PreLaunchOptions PreLaunch { get; set; } = new();
}

/// <summary>Options for the pre-launch script feature.</summary>
public sealed class PreLaunchOptions
{
    /// <summary>Set to false to skip pre-launch scripts globally. Default: true.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Maximum seconds a pre-launch script may run before being killed. Default: 30.</summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Resolution passed to scripts when the manifest does not specify one.
    /// "native" means no override — the script receives the literal string "native".
    /// Default: "native".
    /// </summary>
    public string DefaultResolution { get; set; } = "native";
}
