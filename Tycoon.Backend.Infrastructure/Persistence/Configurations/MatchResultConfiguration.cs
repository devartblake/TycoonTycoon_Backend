using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MatchResultConfiguration : IEntityTypeConfiguration<MatchResult>
    {
        public void Configure(EntityTypeBuilder<MatchResult> b)
        {
            b.ToTable("match_results");
            b.HasKey(x => x.Id);

            b.Property(x => x.MatchId).IsRequired();
            b.HasIndex(x => x.MatchId).IsUnique(); // 1 result snapshot per match

            b.Property(x => x.SubmitEventId).IsRequired();
            b.HasIndex(x => x.SubmitEventId).IsUnique(); // idempotent submit
            b.Property(x => x.Mode).HasMaxLength(32).IsRequired();

            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.QuestionCount).IsRequired();
            b.Property(x => x.EndedAtUtc).IsRequired();
            b.Property(x => x.Status).IsRequired();

            b.HasMany(x => x.Participants)
                .WithOne()
                .HasForeignKey(x => x.MatchResultId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.EndedAtUtc);
        }
    }
}
