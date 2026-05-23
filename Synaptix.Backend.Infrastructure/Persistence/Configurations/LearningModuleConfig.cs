using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class LearningModuleConfig : IEntityTypeConfiguration<LearningModule>
    {
        public void Configure(EntityTypeBuilder<LearningModule> b)
        {
            b.ToTable("learning_modules");
            b.HasKey(x => x.Id);

            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();

            b.Property(x => x.RewardXp).IsRequired();
            b.Property(x => x.RewardCoins).IsRequired();
            b.Property(x => x.IsPublished).IsRequired();

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.IsPublished);
            b.HasIndex(x => x.Difficulty);

            b.HasMany(x => x.Lessons)
                .WithOne()
                .HasForeignKey(x => x.ModuleId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
