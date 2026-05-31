using System.Text.Json;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;

namespace InsertPlay.Core;

/// <summary>
/// Reads and validates an <c>insertplay.json</c> manifest from an SD card drive root.
/// </summary>
public sealed class ManifestParser
{
    private const string ManifestFileName = "insertplay.json";
    private const string SupportedSchemaVersion = "1.0";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
    };

    private readonly ILogger<ManifestParser> _logger;

    public ManifestParser(ILogger<ManifestParser> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Attempts to parse the manifest file at the root of the given drive path.
    /// Returns <c>null</c> if no manifest is found or if the manifest is invalid.
    /// </summary>
    /// <param name="drivePath">Root path of the mounted SD card (e.g., "E:\" or "/run/media/user/CARD").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task<GameManifest?> TryParseAsync(string drivePath, CancellationToken cancellationToken = default)
    {
        var manifestPath = Path.Combine(drivePath, ManifestFileName);

        if (!File.Exists(manifestPath))
        {
            _logger.LogDebug("No manifest found at {Path}. Ignoring drive.", manifestPath);
            return null;
        }

        GameManifest manifest;
        try
        {
            await using var stream = File.OpenRead(manifestPath);
            var result = await JsonSerializer.DeserializeAsync<GameManifest>(stream, JsonOptions, cancellationToken);
            if (result is null)
            {
                _logger.LogWarning("Manifest at {Path} deserialized to null.", manifestPath);
                return null;
            }
            manifest = result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse manifest at {Path}. Check JSON syntax.", manifestPath);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogError(ex, "Failed to read manifest at {Path}.", manifestPath);
            return null;
        }

        if (!Validate(manifest, drivePath))
            return null;

        return manifest;
    }

    private bool Validate(GameManifest manifest, string drivePath)
    {
        var valid = true;

        if (manifest.SchemaVersion != SupportedSchemaVersion)
        {
            _logger.LogError(
                "Unsupported schemaVersion '{Version}'. Expected '{Expected}'.",
                manifest.SchemaVersion, SupportedSchemaVersion);
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Id) || !Guid.TryParse(manifest.Id, out _))
        {
            _logger.LogError("Manifest 'id' is missing or not a valid UUID: '{Id}'.", manifest.Id);
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Title))
        {
            _logger.LogError("Manifest 'title' is required and must not be empty.");
            valid = false;
        }

        if (string.IsNullOrWhiteSpace(manifest.Executable))
        {
            _logger.LogError("Manifest 'executable' is required and must not be empty.");
            valid = false;
        }
        else
        {
            var execPath = Path.Combine(drivePath, manifest.Executable.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(execPath))
            {
                _logger.LogError("Executable '{Executable}' not found on the SD card at '{Path}'.", manifest.Executable, execPath);
                valid = false;
            }
        }

        return valid;
    }
}
