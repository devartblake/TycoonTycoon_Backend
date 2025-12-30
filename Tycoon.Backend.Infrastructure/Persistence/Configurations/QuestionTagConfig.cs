using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class QuestionTagConfig : IEntityTypeConfiguration<QuestionTag>
    {
        public void Configure(EntityTypeBuilder<QuestionTag> b)
        {
            b.ToTable("question_tags");
            b.HasKey(x => x.Id);

            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.Tag).HasMaxLength(64).IsRequired();

            b.HasIndex(x => x.Tag);
            b.HasIndex(x => new { x.QuestionId, x.Tag }).IsUnique();
        }
    }
}
