using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Synaptix.Shared.Abstractions.Core.Domain;

namespace Synaptix.Shared.EF.Interceptors;

// https://khalidabuhakmeh.com/entity-framework-core-5-interceptors
// https://learn.microsoft.com/en-us/ef/core/logging-events-diagnostics/interceptors#savechanges-interception
// Ref: https://www.meziantou.net/entity-framework-core-generate-tracking-columns.htm
public class AuditInterceptor(IHttpContextAccessor httpContextAccessor) : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default
    )
    {
        if (eventData.Context == null)
            return base.SavingChangesAsync(eventData, result, cancellationToken);

        var now = DateTime.Now;
        var userId = ResolveCurrentUserId();

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IHaveAudit>())
        {
            switch (entry.State)
            {
                case EntityState.Modified:
                    entry.CurrentValues[nameof(IHaveAudit.LastModified)] = now;
                    entry.CurrentValues[nameof(IHaveAudit.LastModifiedBy)] = userId;
                    break;
                case EntityState.Added:
                    entry.CurrentValues[nameof(IHaveAudit.Created)] = now;
                    entry.CurrentValues[nameof(IHaveAudit.CreatedBy)] = userId;
                    break;
            }
        }

        foreach (var entry in eventData.Context.ChangeTracker.Entries<IHaveCreator>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.CurrentValues[nameof(IHaveCreator.Created)] = now;
                entry.CurrentValues[nameof(IHaveCreator.CreatedBy)] = userId;
            }
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private int ResolveCurrentUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var parsedUserId) ? parsedUserId : 1;
    }
}
