using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ModerationActionLogConfiguration : IEntityTypeConfiguration<ModerationActionLog>
    {
        public void Configure(EntityTypeBuilder<ModerationActionLog> b)
        {
            b.ToTable("moderation_action_logs");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.NewStatus).IsRequired();

            b.Property(x => x.Reason).HasMaxLength(200);
            b.Property(x => x.Notes).HasMaxLength(1000);

            b.Property(x => x.SetByAdmin).HasMaxLength(120);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.HasIndex(x => x.CreatedAtUtc);

            b.Property(x => x.ExpiresAtUtc);

            b.Property(x => x.RelatedFlagId);
            b.HasIndex(x => x.RelatedFlagId);

            b.HasIndex(x => new { x.NewStatus, x.CreatedAtUtc });
        }
    }
}
