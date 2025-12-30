using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class QuestionOptionConfig : IEntityTypeConfiguration<QuestionOption>
    {
        public void Configure(EntityTypeBuilder<QuestionOption> b)
        {
            b.ToTable("question_options");
            b.HasKey(x => x.Id);

            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.OptionId).HasMaxLength(64).IsRequired();
            b.Property(x => x.Text).HasMaxLength(1000).IsRequired();

            b.HasIndex(x => new { x.QuestionId, x.OptionId }).IsUnique();
        }
    }
}
