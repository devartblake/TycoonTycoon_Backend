using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerModerationProfileConfiguration : IEntityTypeConfiguration<PlayerModerationProfile>
    {
        public void Configure(EntityTypeBuilder<PlayerModerationProfile> b)
        {
            b.ToTable("player_moderation_profiles");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId).IsUnique();

            b.Property(x => x.Status).IsRequired();

            b.Property(x => x.Reason).HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.Property(x => x.SetByAdmin).HasMaxLength(120);
            b.Property(x => x.SetAtUtc).IsRequired();

            b.Property(x => x.ExpiresAtUtc);

            b.HasIndex(x => x.Status);
            b.HasIndex(x => x.SetAtUtc);
        }
    }
}
