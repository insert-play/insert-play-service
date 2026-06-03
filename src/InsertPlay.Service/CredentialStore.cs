using System.Text.Json;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;

#if WINDOWS
using System.Security.Cryptography;
#endif

namespace InsertPlay.Service;

/// <summary>
/// Persists RetroAchievements credentials to a file on the host machine.
/// On Windows the file is encrypted with DPAPI (current-user scope).
/// On Linux the file is written as plain JSON with 0600 permissions.
/// </summary>
internal sealed class CredentialStore : ICredentialStore
{
    private readonly string _filePath;
    private readonly ILogger<CredentialStore> _logger;

    public CredentialStore(ILogger<CredentialStore> logger)
    {
        _logger = logger;
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "InsertPlay");
        Directory.CreateDirectory(dir);
        _filePath = Path.Combine(dir, "ra-credentials.bin");
    }

    public void Save(RetroAchievementsCredentials credentials)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(credentials);

#if WINDOWS
        var data = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
#else
        var data = json;
#endif

        File.WriteAllBytes(_filePath, data);

#if !WINDOWS
#pragma warning disable CA1416 // SetUnixFileMode is available on Linux and macOS; this branch never runs on Windows
        File.SetUnixFileMode(_filePath, UnixFileMode.UserRead | UnixFileMode.UserWrite);
#pragma warning restore CA1416
#endif

        _logger.LogInformation(
            "RetroAchievements credentials saved for user '{User}'.", credentials.Username);
    }

    public RetroAchievementsCredentials? Load()
    {
        if (!File.Exists(_filePath))
            return null;

        try
        {
            var data = File.ReadAllBytes(_filePath);

#if WINDOWS
            var json = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
#else
            var json = data;
#endif

            return JsonSerializer.Deserialize<RetroAchievementsCredentials>(json);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to load RetroAchievements credentials. The file may be corrupt or belong to a different user.");
            return null;
        }
    }

    public void Clear()
    {
        if (File.Exists(_filePath))
            File.Delete(_filePath);

        _logger.LogInformation("RetroAchievements credentials cleared.");
    }
}
