using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IComplianceAuditService
{
    Task RecordAsync(Guid? userId, string eventType, string source, string? eventData, string? ip, CancellationToken ct);
    Task<IReadOnlyList<ComplianceAuditEvent>> GetForUserAsync(Guid userId, int limit, CancellationToken ct);
}
