using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Application.AgeVerification;

internal sealed class AgeVerificationService(IComplianceDb db) : IAgeVerificationService
{
    private const int MinorThreshold = 13; // COPPA: under-13 in the US

    public async Task<Entities.AgeVerification> SubmitAsync(
        Guid userId, int declaredAge, string method, string? ip, CancellationToken ct)
    {
        var record = new Entities.AgeVerification
        {
            UserId = userId,
            DeclaredAge = declaredAge,
            IsMinor = declaredAge < MinorThreshold,
            VerificationMethod = method,
            IpAddress = ip
        };

        db.AgeVerifications.Add(record);
        await db.SaveChangesAsync(ct);
        return record;
    }

    public Task<Entities.AgeVerification?> GetLatestAsync(Guid userId, CancellationToken ct)
        => db.AgeVerifications
             .Where(v => v.UserId == userId)
             .OrderByDescending(v => v.VerifiedAt)
             .FirstOrDefaultAsync(ct);
}
