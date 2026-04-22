using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StudySetItemConfiguration : IEntityTypeConfiguration<StudySetItem>
    {
        public void Configure(EntityTypeBuilder<StudySetItem> b)
        {
            b.ToTable("study_set_items");
            b.HasKey(x => x.Id);

            b.Property(x => x.StudySetId).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.Order).IsRequired();

            b.HasIndex(x => x.StudySetId);
            b.HasIndex(x => new { x.StudySetId, x.Order }).IsUnique();
        }
    }
}
