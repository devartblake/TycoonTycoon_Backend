using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class SkillNodeConfig : IEntityTypeConfiguration<SkillNode>
    {
        public void Configure(EntityTypeBuilder<SkillNode> b)
        {
            b.ToTable("skill_nodes");
            b.HasKey(x => x.Id);

            b.Property(x => x.Key).HasMaxLength(80).IsRequired();
            b.HasIndex(x => x.Key).IsUnique();

            b.Property(x => x.Branch).IsRequired();
            b.Property(x => x.Tier).IsRequired();

            b.Property(x => x.Title).HasMaxLength(120).IsRequired();
            b.Property(x => x.Description).HasMaxLength(600).IsRequired();

            b.Property(x => x.PrereqKeysJson).IsRequired();
            b.Property(x => x.CostsJson).IsRequired();
            b.Property(x => x.EffectsJson).IsRequired();

            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.Branch);
            b.HasIndex(x => x.Tier);
        }
    }
}
