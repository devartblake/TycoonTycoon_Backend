using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PaymentReconciliationIssueConfiguration : IEntityTypeConfiguration<PaymentReconciliationIssue>
    {
        public void Configure(EntityTypeBuilder<PaymentReconciliationIssue> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.PaymentCheckoutAttemptId);
            builder.HasIndex(x => new { x.ResolvedAtUtc, x.CreatedAtUtc });

            builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            builder.Property(x => x.ProviderRef).HasMaxLength(256).IsRequired();
            builder.Property(x => x.Details).HasMaxLength(1024).IsRequired();
            builder.Property(x => x.ResolvedBy).HasMaxLength(256);
            builder.Property(x => x.ResolutionNotes).HasMaxLength(1024);
            builder.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
            builder.Property(x => x.ActualAmount).HasPrecision(18, 2);
        }
    }
}
