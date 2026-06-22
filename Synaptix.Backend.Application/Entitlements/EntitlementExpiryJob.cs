using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Synaptix.Entitlements.Abstractions;

namespace Synaptix.Backend.Application.Entitlements;

public sealed class EntitlementExpiryJob(
    IEntitlementDb db,
    ILogger<EntitlementExpiryJob> logger)
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var count = await db.PlayerEntitlements
            .Where(e => e.ExpiresAtUtc < now && e.Quantity > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.Quantity, 0), ct);

        if (count > 0)
            logger.LogInformation("EntitlementExpiryJob: expired {Count} lapsed entitlement rows", count);
    }
}
