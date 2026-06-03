using InsertPlay.Core.Models;

namespace InsertPlay.Core;

/// <summary>
/// Persists RetroAchievements credentials on the host machine.
/// </summary>
public interface ICredentialStore
{
    /// <summary>Saves credentials, replacing any previously stored value.</summary>
    void Save(RetroAchievementsCredentials credentials);

    /// <summary>Loads stored credentials, or <c>null</c> if none are saved.</summary>
    RetroAchievementsCredentials? Load();

    /// <summary>Removes stored credentials.</summary>
    void Clear();
}
