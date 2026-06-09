using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Contracts.Models;

namespace Synaptix.Compliance.Application.Consent;

internal sealed class ConsentService(IComplianceDb db) : IConsentService
{
    public async Task<ConsentRecord> RecordAsync(
        Guid userId, ConsentType type, bool given, string policyVersion,
        string? ip, string? userAgent, CancellationToken ct)
    {
        var record = new ConsentRecord
        {
            UserId = userId,
            ConsentType = type,
            ConsentGiven = given,
            PolicyVersion = policyVersion,
            IpAddress = ip,
            UserAgent = userAgent
        };

        db.ConsentRecords.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    public async Task<IReadOnlyList<ConsentRecord>> GetCurrentAsync(Guid userId, CancellationToken ct)
    {
        // One record per consent type — the most recent entry for each type
        var all = await db.ConsentRecords
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.RecordedAt)
            .ToListAsync(ct);

        return all
            .GroupBy(r => r.ConsentType)
            .Select(g => g.First())
            .ToList();
    }

    public Task<ConsentRecord?> GetLatestAsync(Guid userId, ConsentType type, CancellationToken ct)
        => db.ConsentRecords
             .Where(r => r.UserId == userId && r.ConsentType == type)
             .OrderByDescending(r => r.RecordedAt)
             .FirstOrDefaultAsync(ct);
}
