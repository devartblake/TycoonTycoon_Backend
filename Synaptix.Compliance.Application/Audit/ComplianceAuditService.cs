using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Application.Audit;

internal sealed class ComplianceAuditService(IComplianceDb db) : IComplianceAuditService
{
    public async Task RecordAsync(
        Guid? userId, string eventType, string source, string? eventData, string? ip, CancellationToken ct)
    {
        db.AuditEvents.Add(new ComplianceAuditEvent
        {
            UserId = userId,
            EventType = eventType,
            Source = source,
            EventData = eventData,
            IpAddress = ip
        });

        await db.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ComplianceAuditEvent>> GetForUserAsync(
        Guid userId, int limit, CancellationToken ct)
        => await db.AuditEvents
                   .Where(e => e.UserId == userId)
                   .OrderByDescending(e => e.OccurredAt)
                   .Take(limit)
                   .ToListAsync(ct);
}
