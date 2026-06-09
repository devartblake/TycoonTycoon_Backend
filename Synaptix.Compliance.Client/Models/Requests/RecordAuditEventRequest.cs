namespace Synaptix.Compliance.Client.Models.Requests;

public sealed record RecordAuditEventRequest(
    Guid? UserId,
    string EventType,
    string Source,
    string? EventData = null,
    string? IpAddress = null);
