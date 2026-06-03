using System.Text.Json;
using InsertPlay.Core;
using InsertPlay.Core.Models;
using Microsoft.Extensions.Logging;

namespace InsertPlay.Service;

/// <summary>
/// Obtains and caches a RetroAchievements session token in memory.
/// Token is fetched via r=login2 and reused across launches while credentials do not change.
/// </summary>
public sealed class RetroAchievementsSessionProvider
{
    private static readonly HttpClient s_httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };

    private readonly ICredentialStore _credentialStore;
    private readonly ILogger<RetroAchievementsSessionProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private string? _cachedUsername;
    private string? _cachedPassword;
    private string? _cachedToken;
    private long _cachedLoginTimestamp;

    public RetroAchievementsSessionProvider(
        ICredentialStore credentialStore,
        ILogger<RetroAchievementsSessionProvider> logger)
    {
        _credentialStore = credentialStore;
        _logger = logger;
    }

    public async Task WarmupAsync(CancellationToken cancellationToken)
    {
        var credentials = _credentialStore.Load();
        if (credentials is null)
            return;

        var enriched = await EnrichAsync(credentials, cancellationToken);
        if (!string.IsNullOrWhiteSpace(enriched?.ApiToken))
            _logger.LogInformation("RetroAchievements token cached in memory at service startup.");
    }

    public async Task<RetroAchievementsCredentials?> EnrichAsync(
        RetroAchievementsCredentials? credentials,
        CancellationToken cancellationToken)
    {
        if (credentials is null)
            return null;

        var username = credentials.Username.Trim();
        var password = credentials.Password.Trim();
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return credentials;

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (IsCacheValid(username, password))
            {
                return credentials with
                {
                    ApiToken = _cachedToken!,
                    LoginTimestamp = _cachedLoginTimestamp,
                };
            }

            var (isValid, token, message) = await LoginAsync(username, password, cancellationToken);
            if (!isValid || string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("Failed to obtain RetroAchievements token: {Message}", message);
                return credentials;
            }

            _cachedUsername = username;
            _cachedPassword = password;
            _cachedToken = token;
            _cachedLoginTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            return credentials with
            {
                ApiToken = _cachedToken,
                LoginTimestamp = _cachedLoginTimestamp,
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsCacheValid(string username, string password) =>
        !string.IsNullOrWhiteSpace(_cachedToken)
        && string.Equals(_cachedUsername, username, StringComparison.Ordinal)
        && string.Equals(_cachedPassword, password, StringComparison.Ordinal);

    private static async Task<(bool IsValid, string? Token, string Message)> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        const string url = "https://retroachievements.org/dorequest.php";
        using var body = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["r"] = "login2",
            ["u"] = username,
            ["p"] = password,
        });

        try
        {
            using var response = await s_httpClient.PostAsync(url, body, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
                return (false, null, "HTTP failure while validating credentials.");

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (TryGetBoolean(root, "Success", out var success) && success
                && TryGetString(root, "Token", out var successToken)
                && !string.IsNullOrWhiteSpace(successToken))
            {
                return (true, successToken, string.Empty);
            }

            if (TryGetString(root, "Error", out var error) && !string.IsNullOrWhiteSpace(error))
                return (false, null, error);

            if (TryGetString(root, "Message", out var message) && !string.IsNullOrWhiteSpace(message))
                return (false, null, message);

            return (false, null, "Invalid username or password.");
        }
        catch (OperationCanceledException)
        {
            return (false, null, "Cancelled while validating credentials.");
        }
        catch
        {
            return (false, null, "Could not contact RetroAchievements now.");
        }
    }

    private static bool TryGetBoolean(JsonElement root, string property, out bool value)
    {
        value = false;
        if (!root.TryGetProperty(property, out var element))
            return false;

        if (element.ValueKind == JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (element.ValueKind == JsonValueKind.False)
        {
            value = false;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var number))
        {
            value = number != 0;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            var text = element.GetString();
            if (bool.TryParse(text, out var parsedBool))
            {
                value = parsedBool;
                return true;
            }
            if (int.TryParse(text, out var parsedInt))
            {
                value = parsedInt != 0;
                return true;
            }
        }

        return false;
    }

    private static bool TryGetString(JsonElement root, string property, out string? value)
    {
        value = null;
        if (!root.TryGetProperty(property, out var element))
            return false;

        if (element.ValueKind != JsonValueKind.String)
            return false;

        value = element.GetString();
        return true;
    }
}
