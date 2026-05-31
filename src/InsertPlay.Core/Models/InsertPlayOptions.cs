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
}
