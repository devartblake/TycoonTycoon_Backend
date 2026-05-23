using System.Text.Json;
using Synaptix.Backend.Api.Observability;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Api.Security;

public static class AdminSecurityAudit
{
    private const string AuditChannelKey = "admin_security";

    public static async Task WriteAsync(
        IAppDb db,
        string title,
        string status,
        object metadata,
        CancellationToken ct = default)
    {
        db.AdminNotificationHistory.Add(new AdminNotificationHistory(
            id: $"audit_{Guid.NewGuid():N}",
            channelKey: AuditChannelKey,
            title: title,
            status: status,
            createdAt: DateTimeOffset.UtcNow,
            metadataJson: JsonSerializer.Serialize(metadata)));

        await db.SaveChangesAsync(ct);
        AdminSecurityMetrics.RecordAuditWrite(title);
    }
}
