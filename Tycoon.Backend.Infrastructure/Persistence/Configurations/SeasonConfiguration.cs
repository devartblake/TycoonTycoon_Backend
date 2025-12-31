using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class SeasonConfiguration : IEntityTypeConfiguration<Season>
    {
        public void Configure(EntityTypeBuilder<Season> b)
        {
            b.ToTable("seasons");
            b.HasKey(x => x.Id);

            b.Property(x => x.SeasonNumber).IsRequired();
            b.HasIndex(x => x.SeasonNumber).IsUnique();

            b.Property(x => x.Name).HasMaxLength(80).IsRequired();

            // If your Season.Status is SeasonStatus enum from contracts
            b.Property(x => x.Status).IsRequired();

            b.Property(x => x.StartsAtUtc).IsRequired();
            b.Property(x => x.EndsAtUtc).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.StartsAtUtc);
            b.HasIndex(x => x.EndsAtUtc);
        }
    }
}
