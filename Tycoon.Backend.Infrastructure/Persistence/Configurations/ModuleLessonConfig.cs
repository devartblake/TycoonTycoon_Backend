using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ModuleLessonConfig : IEntityTypeConfiguration<ModuleLesson>
    {
        public void Configure(EntityTypeBuilder<ModuleLesson> b)
        {
            b.ToTable("module_lessons");
            b.HasKey(x => x.Id);

            b.Property(x => x.ModuleId).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.Order).IsRequired();
            b.Property(x => x.Explanation).HasMaxLength(2000);

            b.HasIndex(x => x.ModuleId);
            b.HasIndex(x => x.QuestionId);

            // Prevent duplicate ordering within a module
            b.HasIndex(x => new { x.ModuleId, x.Order }).IsUnique();
        }
    }
}
