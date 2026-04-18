using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class StudySetConfiguration : IEntityTypeConfiguration<StudySet>
    {
        public void Configure(EntityTypeBuilder<StudySet> b)
        {
            b.ToTable("study_sets");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.UpdatedAtUtc).IsRequired();

            b.HasIndex(x => x.PlayerId);
            b.HasIndex(x => new { x.PlayerId, x.UpdatedAtUtc });

            b.HasMany(x => x.Items)
                .WithOne()
                .HasForeignKey(x => x.StudySetId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
