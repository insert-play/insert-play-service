using System.Text.Json;
using System.Text.Json.Serialization;

namespace InsertPlay.Core.Models;

/// <summary>
/// Specifies the pre-launch script path(s) for each supported platform.
/// Can be deserialized from either a plain JSON string (same path on all platforms)
/// or an object with per-platform keys.
/// </summary>
/// <example>
/// Shorthand (same script on both platforms):
/// <code>"preLaunchScript": "scripts/configure.ps1"</code>
/// Per-platform (recommended):
/// <code>
/// "preLaunchScript": {
///   "windows": "scripts/configure.ps1",
///   "linux":   "scripts/configure.sh"
/// }
/// </code>
/// </example>
[JsonConverter(typeof(PreLaunchScriptSpecConverter))]
public sealed class PreLaunchScriptSpec
{
    /// <summary>Script path used on Windows, relative to the SD card root.</summary>
    public string? Windows { get; init; }

    /// <summary>Script path used on Linux/SteamOS, relative to the SD card root.</summary>
    public string? Linux { get; init; }
}

internal sealed class PreLaunchScriptSpecConverter : JsonConverter<PreLaunchScriptSpec>
{
    public override PreLaunchScriptSpec? Read(
        ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var path = reader.GetString();
            return path is null ? null : new PreLaunchScriptSpec { Windows = path, Linux = path };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            string? windows = null, linux = null;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
            {
                if (reader.TokenType != JsonTokenType.PropertyName)
                    continue;

                var key = reader.GetString();
                reader.Read();
                switch (key)
                {
                    case "windows": windows = reader.GetString(); break;
                    case "linux":   linux   = reader.GetString(); break;
                    default:        reader.Skip();                break;
                }
            }
            return new PreLaunchScriptSpec { Windows = windows, Linux = linux };
        }

        return null;
    }

    public override void Write(
        Utf8JsonWriter writer, PreLaunchScriptSpec value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        if (value.Windows is not null) writer.WriteString("windows", value.Windows);
        if (value.Linux   is not null) writer.WriteString("linux",   value.Linux);
        writer.WriteEndObject();
    }
}
