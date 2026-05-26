namespace Synaptix.Backend.Domain.Entities;

public sealed class PlayerLookupCode
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid PlayerId { get; private set; }
    public Guid? UserId { get; private set; }
    public string ShortCode { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

    private PlayerLookupCode() { }

    public PlayerLookupCode(Guid playerId, string shortCode, Guid? userId = null)
    {
        PlayerId = playerId;
        UserId = userId;
        ShortCode = Normalize(shortCode);
    }

    public void LinkUser(Guid? userId)
    {
        if (UserId == userId) return;
        UserId = userId;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private static string Normalize(string value)
        => value.Trim().ToUpperInvariant();
}
