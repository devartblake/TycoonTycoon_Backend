using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Entities;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IComplianceDb
{
    DbSet<AgeVerification> AgeVerifications { get; }
    DbSet<ParentalConsent> ParentalConsents { get; }
    DbSet<PrivacyRequest> PrivacyRequests { get; }
    DbSet<ConsentRecord> ConsentRecords { get; }
    DbSet<ComplianceAuditEvent> AuditEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
