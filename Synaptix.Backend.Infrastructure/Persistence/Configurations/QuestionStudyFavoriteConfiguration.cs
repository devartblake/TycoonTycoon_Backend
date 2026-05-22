using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class QuestionStudyFavoriteConfiguration : IEntityTypeConfiguration<QuestionStudyFavorite>
    {
        public void Configure(EntityTypeBuilder<QuestionStudyFavorite> b)
        {
            b.ToTable("question_study_favorites");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();

            b.HasIndex(x => x.PlayerId);
            b.HasIndex(x => x.QuestionId);
            b.HasIndex(x => new { x.PlayerId, x.QuestionId }).IsUnique();
        }
    }
}
