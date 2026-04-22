using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StudyCardStateConfiguration : IEntityTypeConfiguration<StudyCardState>
    {
        public void Configure(EntityTypeBuilder<StudyCardState> b)
        {
            b.ToTable("study_card_states");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.ReviewCount).IsRequired();
            b.Property(x => x.SuccessStreak).IsRequired();
            b.Property(x => x.EaseFactor).HasPrecision(4, 2).IsRequired();
            b.Property(x => x.LastOutcome).HasMaxLength(32);
            b.Property(x => x.LastMode).HasMaxLength(32);

            b.HasIndex(x => x.PlayerId);
            b.HasIndex(x => x.QuestionId);
            b.HasIndex(x => new { x.PlayerId, x.QuestionId }).IsUnique();
            b.HasIndex(x => new { x.PlayerId, x.NextReviewAtUtc });
        }
    }
}
