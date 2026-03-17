namespace Tycoon.OperatorDashboard.Services;

/// <summary>
/// Server-side in-memory token store keyed by session user ID.
/// Tokens are never sent to the browser — only the encrypted cookie is.
/// </summary>
public sealed class TokenStore
{
    private readonly Dictionary<string, TokenEntry> _tokens = new();
    private readonly Lock _lock = new();

    public void Set(string userId, string accessToken, string refreshToken, DateTimeOffset expiresAt)
    {
        lock (_lock)
            _tokens[userId] = new TokenEntry(accessToken, refreshToken, expiresAt);
    }

    public TokenEntry? Get(string userId)
    {
        lock (_lock)
            return _tokens.GetValueOrDefault(userId);
    }

    public void Remove(string userId)
    {
        lock (_lock)
            _tokens.Remove(userId);
    }

    public sealed record TokenEntry(string AccessToken, string RefreshToken, DateTimeOffset ExpiresAt);
}
