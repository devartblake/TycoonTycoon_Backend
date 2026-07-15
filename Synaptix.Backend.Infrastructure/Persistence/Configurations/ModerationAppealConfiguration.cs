using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ModerationAppealConfiguration : IEntityTypeConfiguration<ModerationAppeal>
    {
        public void Configure(EntityTypeBuilder<ModerationAppeal> b)
        {
            b.ToTable("moderation_appeals");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.Reason).IsRequired().HasMaxLength(2000);

            b.Property(x => x.Status).IsRequired();

            b.Property(x => x.ReviewerNotes).HasMaxLength(2000);
            b.Property(x => x.ReviewedBy).HasMaxLength(120);

            b.Property(x => x.SubmittedAtUtc).IsRequired();
            b.Property(x => x.ReviewedAtUtc);

            // Pending-queue listing and the one-pending-appeal-per-player guard.
            b.HasIndex(x => new { x.Status, x.SubmittedAtUtc });
            b.HasIndex(x => new { x.PlayerId, x.Status });
        }
    }
}
