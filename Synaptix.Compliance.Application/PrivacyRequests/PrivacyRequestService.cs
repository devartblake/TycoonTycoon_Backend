using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.PrivacyRequests;

internal sealed class PrivacyRequestService(IComplianceDb db) : IPrivacyRequestService
{
    public async Task<PrivacyRequest> SubmitAsync(
        Guid userId, PrivacyRequestType type, string? ip, CancellationToken ct)
    {
        var record = new PrivacyRequest
        {
            UserId = userId,
            RequestType = type,
            IpAddress = ip
        };

        db.PrivacyRequests.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    public Task<PrivacyRequest?> GetAsync(Guid requestId, CancellationToken ct)
        => db.PrivacyRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct);

    public async Task<IReadOnlyList<PrivacyRequest>> GetPendingAsync(int limit, CancellationToken ct)
        => await db.PrivacyRequests
                   .Where(r => r.Status == PrivacyRequestStatus.Pending)
                   .OrderBy(r => r.SubmittedAt)
                   .Take(limit)
                   .ToListAsync(ct);

    public async Task<PrivacyRequest> UpdateStatusAsync(
        Guid requestId, PrivacyRequestStatus status, string? notes, CancellationToken ct)
    {
        var record = await db.PrivacyRequests.FirstOrDefaultAsync(r => r.Id == requestId, ct)
            ?? throw new InvalidOperationException("privacy_request_not_found");

        record.Status = status;
        record.Notes = notes;

        if (status is PrivacyRequestStatus.Completed or PrivacyRequestStatus.Failed)
            record.CompletedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return record;
    }
}
