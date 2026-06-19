using Microsoft.EntityFrameworkCore;
using Synaptix.Entitlements.Entities;

namespace Synaptix.Entitlements.Abstractions;

public interface IEntitlementDb
{
    DbSet<PlayerEntitlement> PlayerEntitlements { get; }
    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
