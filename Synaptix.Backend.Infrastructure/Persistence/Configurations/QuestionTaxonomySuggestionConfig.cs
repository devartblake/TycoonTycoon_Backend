using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class QuestionTaxonomySuggestionConfig : IEntityTypeConfiguration<QuestionTaxonomySuggestion>
{
    public void Configure(EntityTypeBuilder<QuestionTaxonomySuggestion> b)
    {
        b.ToTable("question_taxonomy_suggestions");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.QuestionId).HasColumnName("question_id");
        b.Property(x => x.SourceDataset).HasColumnName("source_dataset").HasMaxLength(256);
        b.Property(x => x.SourceQuestionId).HasColumnName("source_question_id").HasMaxLength(128);
        b.Property(x => x.SuggestedTaxonomyJson).HasColumnName("suggested_taxonomy_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.ConfidenceJson).HasColumnName("confidence_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.WarningsJson).HasColumnName("warnings_json").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.OverallConfidence).HasColumnName("overall_confidence").HasPrecision(5, 4);
        b.Property(x => x.ModelVersion).HasColumnName("model_version").HasMaxLength(128).IsRequired();
        b.Property(x => x.Status).HasColumnName("status").HasMaxLength(32).IsRequired();
        b.Property(x => x.CreatedAtUtc).HasColumnName("created_at_utc");
        b.Property(x => x.AppliedAtUtc).HasColumnName("applied_at_utc");
        b.Property(x => x.ReviewedBy).HasColumnName("reviewed_by").HasMaxLength(256);
        b.Property(x => x.ReviewNote).HasColumnName("review_note").HasMaxLength(1000);

        b.HasIndex(x => x.QuestionId).HasDatabaseName("ix_question_taxonomy_suggestions_question_id");
        b.HasIndex(x => new { x.SourceDataset, x.SourceQuestionId }).HasDatabaseName("ix_question_taxonomy_suggestions_source");
        b.HasIndex(x => new { x.Status, x.CreatedAtUtc }).HasDatabaseName("ix_question_taxonomy_suggestions_status_created_at_utc");
    }
}
