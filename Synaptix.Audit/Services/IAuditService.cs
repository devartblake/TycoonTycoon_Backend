namespace Synaptix.Audit.Services;

public interface IAuditService
{
    Task RecordPurchaseAsync(
        Guid userId,
        string transactionKind,
        Guid transactionId,
        string sku,
        int quantity,
        string? receipt,
        CancellationToken ct = default);
}
