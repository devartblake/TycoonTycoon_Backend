using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PaymentCheckoutAttemptConfiguration : IEntityTypeConfiguration<PaymentCheckoutAttempt>
    {
        public void Configure(EntityTypeBuilder<PaymentCheckoutAttempt> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Provider, x.ProviderRef }).IsUnique();
            builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
            builder.HasIndex(x => x.PlayerId);

            builder.Property(x => x.Provider).HasMaxLength(32).IsRequired();
            builder.Property(x => x.Sku).HasMaxLength(128).IsRequired();
            builder.Property(x => x.Currency).HasMaxLength(8).IsRequired();
            builder.Property(x => x.ProviderRef).HasMaxLength(256).IsRequired();
            builder.Property(x => x.ProviderCaptureRef).HasMaxLength(256);
            builder.Property(x => x.ExpectedAmount).HasPrecision(18, 2);
        }
    }
}
