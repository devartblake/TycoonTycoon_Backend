using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerSkillUnlockConfig : IEntityTypeConfiguration<PlayerSkillUnlock>
    {
        public void Configure(EntityTypeBuilder<PlayerSkillUnlock> b)
        {
            b.ToTable("player_skill_unlocks");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.NodeKey).HasMaxLength(80).IsRequired();
            b.Property(x => x.UnlockedAtUtc).IsRequired();

            b.HasIndex(x => new { x.PlayerId, x.NodeKey }).IsUnique();
            b.HasIndex(x => x.PlayerId);
        }
    }
}
