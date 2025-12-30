using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
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

            b.Property(x => x.MediaKey).HasMaxLength(256);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.Category);
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
