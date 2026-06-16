using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Entities;
using AgeVerificationEntity = Synaptix.Compliance.Application.Entities.AgeVerification;
using ParentalConsentEntity = Synaptix.Compliance.Application.Entities.ParentalConsent;

namespace Synaptix.Compliance.Application.Abstractions;

public interface IComplianceDb
{
    DbSet<AgeVerificationEntity> AgeVerifications { get; }
    DbSet<ParentalConsentEntity> ParentalConsents { get; }
    DbSet<PrivacyRequest> PrivacyRequests { get; }
    DbSet<ConsentRecord> ConsentRecords { get; }
    DbSet<ComplianceAuditEvent> AuditEvents { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
