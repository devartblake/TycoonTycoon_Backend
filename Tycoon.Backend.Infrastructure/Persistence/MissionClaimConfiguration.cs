using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MissionClaimConfiguration : IEntityTypeConfiguration<MissionClaim>
    {
        public void Configure(EntityTypeBuilder<MissionClaim> builder)
        {
            builder.HasKey(x => x.Id);

            // Prevent duplicate claims for same player + mission
            builder.HasIndex(x => new { x.PlayerId, x.MissionId })
                   .IsUnique();

            // Reasonable defaults / constraints
            builder.Property(x => x.Progress).IsRequired();
            builder.Property(x => x.Completed).IsRequired();
            builder.Property(x => x.Claimed).IsRequired();

            builder.Property(x => x.CreatedAtUtc).IsRequired();
            builder.Property(x => x.UpdatedAtUtc).IsRequired();
        }
    }
}
