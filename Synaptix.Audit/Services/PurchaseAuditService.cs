using Microsoft.Extensions.Logging;
using Synaptix.Compliance.Client.Abstractions;
using Synaptix.Compliance.Client.Models.Requests;

namespace Synaptix.Audit.Services;

public sealed class PurchaseAuditService(
    IComplianceClient compliance,
    ILogger<PurchaseAuditService> logger) : IAuditService
{
    public async Task RecordPurchaseAsync(
        Guid userId,
        string transactionKind,
        Guid transactionId,
        string sku,
        int quantity,
        string? receipt,
        CancellationToken ct = default)
    {
        try
        {
            var eventData = System.Text.Json.JsonSerializer.Serialize(new
            {
                transactionId,
                sku,
                quantity,
                receipt
            });

            await compliance.RecordAuditEventAsync(new RecordAuditEventRequest(
                UserId: userId,
                EventType: $"purchase_{transactionKind.Replace('-', '_')}",
                Source: "synaptix.commerce",
                EventData: eventData,
                IpAddress: null
            ), ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record purchase audit event for transaction {TransactionId}; continuing", transactionId);
        }
    }
}
