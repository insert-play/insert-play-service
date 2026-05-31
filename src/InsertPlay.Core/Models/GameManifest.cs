using System.Text.Json.Serialization;

namespace InsertPlay.Core.Models;

/// <summary>
/// Represents the contents of an insertplay.json manifest file placed at the root of an SD game card.
/// </summary>
public sealed class GameManifest
{
    /// <summary>Schema version. Must be "1.0" for this version of InsertPlay.</summary>
    [JsonPropertyName("schemaVersion")]
    public string SchemaVersion { get; init; } = string.Empty;

    /// <summary>UUID v4 that uniquely identifies this game card.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    /// <summary>Display name of the game.</summary>
    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Relative path to the game executable from the manifest file's directory.
    /// Use forward slashes on all platforms.
    /// </summary>
    [JsonPropertyName("executable")]
    public string Executable { get; init; } = string.Empty;

    /// <summary>
    /// Working directory for the game process, relative to the manifest file.
    /// Defaults to the executable's parent directory when null or empty.
    /// </summary>
    [JsonPropertyName("workingDirectory")]
    public string? WorkingDirectory { get; init; }

    /// <summary>Command-line arguments passed to the executable.</summary>
    [JsonPropertyName("arguments")]
    public string[] Arguments { get; init; } = [];

    /// <summary>
    /// Controller button names that must all be held simultaneously to quit the game.
    /// Overrides the service-level default from appsettings.json when set.
    /// </summary>
    [JsonPropertyName("stopCombination")]
    public string[]? StopCombination { get; init; }

    /// <summary>Name of the game developer or studio.</summary>
    [JsonPropertyName("developer")]
    public string? Developer { get; init; }

    /// <summary>Name of the game publisher.</summary>
    [JsonPropertyName("publisher")]
    public string? Publisher { get; init; }

    /// <summary>Short description of the game.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Game version string (e.g., "1.2.0").</summary>
    [JsonPropertyName("version")]
    public string? Version { get; init; }

    /// <summary>ISO 8601 release date (YYYY-MM-DD).</summary>
    [JsonPropertyName("releaseDate")]
    public string? ReleaseDate { get; init; }

    /// <summary>Descriptive tags (e.g., genre or features).</summary>
    [JsonPropertyName("tags")]
    public string[] Tags { get; init; } = [];

    // -------------------------------------------------------------------------
    // Future fields — Priority 1 (Installation & Steam)
    // -------------------------------------------------------------------------

    /// <summary>[Future — Priority 1] Relative path to the cover image on the SD card.</summary>
    [JsonPropertyName("coverImage")]
    public string? CoverImage { get; init; }

    /// <summary>[Future — Priority 1] Steam App ID for non-Steam shortcut registration.</summary>
    [JsonPropertyName("steamAppId")]
    public int? SteamAppId { get; init; }

    /// <summary>[Future — Priority 1] Relative path to an install script on the SD card.</summary>
    [JsonPropertyName("installScript")]
    public string? InstallScript { get; init; }

    // -------------------------------------------------------------------------
    // Future fields — Priority 2 (Portable Saves)
    // -------------------------------------------------------------------------

    /// <summary>
    /// [Future — Priority 2] Local file system path where the game stores save files.
    /// Supports environment variable expansion (%APPDATA%, $HOME, ~).
    /// </summary>
    [JsonPropertyName("saveDataPath")]
    public string? SaveDataPath { get; init; }

    /// <summary>[Future — Priority 2] Directory on the SD card used to store portable save data.</summary>
    [JsonPropertyName("sdSaveDirectory")]
    public string? SdSaveDirectory { get; init; }
}
