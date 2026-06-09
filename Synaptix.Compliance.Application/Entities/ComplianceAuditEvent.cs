namespace Synaptix.Compliance.Application.Entities;

public sealed class ComplianceAuditEvent
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid? UserId { get; init; }

    // e.g. "payment_processed", "data_export_requested", "account_deleted"
    public string EventType { get; init; } = string.Empty;
    public string? EventData { get; init; }  // JSON payload

    // e.g. "backend-api", "compliance-api", "admin-dashboard"
    public string Source { get; init; } = string.Empty;
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
    public string? IpAddress { get; init; }
}
