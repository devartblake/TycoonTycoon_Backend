using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ModuleCompletionConfig : IEntityTypeConfiguration<ModuleCompletion>
    {
        public void Configure(EntityTypeBuilder<ModuleCompletion> b)
        {
            b.ToTable("module_completions");
            b.HasKey(x => x.Id);

            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.ModuleId).IsRequired();
            b.Property(x => x.EconomyEventId).IsRequired();
            b.Property(x => x.CompletedAtUtc).IsRequired();

            // Primary idempotency guard: one completion record per player per module
            b.HasIndex(x => new { x.PlayerId, x.ModuleId }).IsUnique();

            // Secondary guard: ensures EconomyService EventId is never reused across completions
            b.HasIndex(x => x.EconomyEventId).IsUnique();

            b.HasIndex(x => x.PlayerId);
            b.HasIndex(x => x.ModuleId);
        }
    }
}
