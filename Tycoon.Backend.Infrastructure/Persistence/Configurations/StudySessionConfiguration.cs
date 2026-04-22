using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StudySessionConfiguration : IEntityTypeConfiguration<StudySession>
    {
        public void Configure(EntityTypeBuilder<StudySession> b)
        {
            b.ToTable("study_sessions");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.StudySetId).HasMaxLength(256).IsRequired();
            b.Property(x => x.Mode).HasMaxLength(32).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Kind).HasMaxLength(32).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.QuestionCount).IsRequired();
            b.Property(x => x.QuestionIdsJson).IsRequired();
            b.Property(x => x.AnswerKeyJson).IsRequired();
            b.Property(x => x.AnsweredResultsJson).IsRequired();
            b.Property(x => x.InteractionStatesJson).IsRequired();
            b.Property(x => x.AnsweredCount).IsRequired();
            b.Property(x => x.CorrectCount).IsRequired();
            b.Property(x => x.CurrentQuestionIndex).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.PlayerId);
            b.HasIndex(x => new { x.PlayerId, x.CreatedAtUtc });
            b.HasIndex(x => new { x.PlayerId, x.StudySetId, x.CompletedAtUtc });
        }
    }
}
