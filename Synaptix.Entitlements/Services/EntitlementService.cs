using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Entitlements.Abstractions;
using Synaptix.Entitlements.Entities;

namespace Synaptix.Entitlements.Services;

public sealed class EntitlementService(
    IEntitlementDb db,
    ILogger<EntitlementService> logger) : IEntitlementService
{
    public async Task GrantAsync(Guid playerId, string sku, string itemType, int quantity, Guid sourceTransactionId, string scope = "permanent", CancellationToken ct = default)
    {
        var existing = await db.PlayerEntitlements
            .FirstOrDefaultAsync(e => e.PlayerId == playerId && e.Sku == sku && e.Scope == scope, ct);

        if (existing is not null && scope == "permanent")
        {
            logger.LogInformation("Entitlement for {PlayerId}/{Sku} already exists (permanent); skipping duplicate grant", playerId, sku);
            return;
        }

        var entitlement = PlayerEntitlement.Grant(playerId, sku, itemType, quantity, sourceTransactionId, scope);
        db.PlayerEntitlements.Add(entitlement);
        await db.SaveChangesAsync(ct);
    }

    public async Task RevokeAsync(Guid playerId, string sku, int quantity, Guid sourceTransactionId, CancellationToken ct = default)
    {
        var entitlement = await db.PlayerEntitlements
            .Where(e => e.PlayerId == playerId && e.Sku == sku && e.Quantity > 0)
            .OrderBy(e => e.GrantedAtUtc)
            .FirstOrDefaultAsync(ct);

        if (entitlement is null)
        {
            logger.LogWarning("Revoke attempted for {PlayerId}/{Sku} but no entitlement found", playerId, sku);
            return;
        }

        entitlement.Consume(quantity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<EntitlementDto>> GetInventoryAsync(Guid playerId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var items = await db.PlayerEntitlements
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId && e.Quantity > 0 && (e.ExpiresAtUtc == null || e.ExpiresAtUtc > now))
            .OrderBy(e => e.ItemType)
            .ThenBy(e => e.Sku)
            .Select(e => new EntitlementDto(e.Id, e.Sku, e.ItemType, e.Quantity, e.Scope, e.GrantedAtUtc, e.ExpiresAtUtc))
            .ToListAsync(ct);
        return items;
    }
}
