namespace InsertPlay.Core.Models;

/// <summary>
/// Holds the RetroAchievements login credentials stored on the host machine.
/// </summary>
public sealed record class RetroAchievementsCredentials
{
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Password used by emulators to obtain a session token.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Legacy field kept for backward compatibility with already-saved credentials.
    /// Older builds stored the second field as ApiToken.
    /// </summary>
    public string ApiToken { get; init; } = string.Empty;

    /// <summary>
    /// Unix timestamp associated with the session token.
    /// Used by PCSX2 as LoginTimestamp.
    /// </summary>
    public long LoginTimestamp { get; init; }
}
