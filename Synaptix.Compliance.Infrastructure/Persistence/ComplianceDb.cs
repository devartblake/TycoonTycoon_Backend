using Microsoft.EntityFrameworkCore;
using Synaptix.Compliance.Application.Abstractions;
using Synaptix.Compliance.Application.Entities;
using Synaptix.Compliance.Infrastructure.Persistence.Configurations;

namespace Synaptix.Compliance.Infrastructure.Persistence;

public sealed class ComplianceDb(DbContextOptions<ComplianceDb> options)
    : DbContext(options), IComplianceDb
{
    public DbSet<AgeVerification> AgeVerifications => Set<AgeVerification>();
    public DbSet<ParentalConsent> ParentalConsents => Set<ParentalConsent>();
    public DbSet<PrivacyRequest> PrivacyRequests => Set<PrivacyRequest>();
    public DbSet<ConsentRecord> ConsentRecords => Set<ConsentRecord>();
    public DbSet<ComplianceAuditEvent> AuditEvents => Set<ComplianceAuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("compliance");
        builder.ApplyConfiguration(new AgeVerificationConfiguration());
        builder.ApplyConfiguration(new ParentalConsentConfiguration());
        builder.ApplyConfiguration(new PrivacyRequestConfiguration());
        builder.ApplyConfiguration(new ConsentRecordConfiguration());
        builder.ApplyConfiguration(new ComplianceAuditEventConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
        => builder.UseSnakeCaseNamingConvention();
}
