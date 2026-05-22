using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class EconomyTransactionLineConfiguration : IEntityTypeConfiguration<EconomyTransactionLine>
    {
        public void Configure(EntityTypeBuilder<EconomyTransactionLine> b)
        {
            b.ToTable("economy_transaction_lines");
            b.HasKey(x => x.Id);

            b.Property(x => x.EconomyTransactionId).IsRequired();
            b.Property(x => x.Currency).IsRequired();
            b.Property(x => x.Delta).IsRequired();

            b.HasIndex(x => x.EconomyTransactionId);
        }
    }
}
