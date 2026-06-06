using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class QuestionConfig : IEntityTypeConfiguration<Question>
    {
        public void Configure(EntityTypeBuilder<Question> b)
        {
            b.ToTable("questions");
            b.HasKey(x => x.Id);

            b.Property(x => x.Text).HasMaxLength(2000).IsRequired();
            b.Property(x => x.Category).HasMaxLength(64).IsRequired();
            b.Property(x => x.CorrectOptionId).HasMaxLength(64).IsRequired();
            b.Property(x => x.CanonicalCategory).HasColumnName("canonical_category").HasMaxLength(128).IsRequired();
            b.Property(x => x.DisplayCategory).HasColumnName("display_category").HasMaxLength(128).IsRequired();
            b.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(128);
            b.Property(x => x.Topic).HasColumnName("topic").HasMaxLength(128);
            b.Property(x => x.Subtopic).HasColumnName("subtopic").HasMaxLength(128);
            b.Property(x => x.GradeBand).HasColumnName("grade_band").HasMaxLength(64);
            b.Property(x => x.AgeGroup).HasColumnName("age_group").HasMaxLength(64);
            b.Property(x => x.Audience).HasColumnName("audience").HasMaxLength(64);
            b.Property(x => x.SourceDataset).HasColumnName("source_dataset").HasMaxLength(256);
            b.Property(x => x.SourceQuestionId).HasColumnName("source_question_id").HasMaxLength(128);
            b.Property(x => x.QuestionType).HasColumnName("question_type").HasMaxLength(64).IsRequired();
            b.Property(x => x.MediaType).HasColumnName("media_type").HasMaxLength(64).IsRequired();
            b.Property(x => x.TaxonomyTagsJson).HasColumnName("taxonomy_tags_json").HasColumnType("jsonb").IsRequired();

            b.Property(x => x.MediaKey).HasMaxLength(256);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.Category);
            b.HasIndex(x => x.CanonicalCategory);
            b.HasIndex(x => x.Subject);
            b.HasIndex(x => x.GradeBand);
            b.HasIndex(x => x.AgeGroup);
            b.HasIndex(x => x.SourceDataset);
            b.HasIndex(x => new { x.SourceDataset, x.SourceQuestionId }).IsUnique()
                .HasFilter("source_dataset IS NOT NULL AND source_question_id IS NOT NULL");
            b.HasIndex(x => new { x.Status, x.CanonicalCategory, x.Difficulty });
            b.HasIndex(x => x.Difficulty);
            b.HasIndex(x => x.UpdatedAtUtc);

            b.HasMany(x => x.Options)
                .WithOne()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasMany(x => x.Tags)
                .WithOne()
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
