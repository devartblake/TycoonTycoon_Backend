namespace Synaptix.Entitlements.Services;

public sealed record EntitlementDto(
    Guid Id,
    string Sku,
    string ItemType,
    int Quantity,
    string Scope,
    DateTimeOffset GrantedAtUtc,
    DateTimeOffset? ExpiresAtUtc);

public interface IEntitlementService
{
    Task GrantAsync(Guid playerId, string sku, string itemType, int quantity, Guid sourceTransactionId, string scope = "permanent", CancellationToken ct = default);
    Task RevokeAsync(Guid playerId, string sku, int quantity, Guid sourceTransactionId, CancellationToken ct = default);
    Task UpdateExpiryAsync(Guid playerId, string sku, DateTimeOffset? expiresAt, CancellationToken ct = default);
    Task<IReadOnlyList<EntitlementDto>> GetInventoryAsync(Guid playerId, CancellationToken ct = default);
}
