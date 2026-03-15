using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class VoteConfiguration : IEntityTypeConfiguration<Vote>
    {
        public void Configure(EntityTypeBuilder<Vote> b)
        {
            b.ToTable("votes");
            b.HasKey(x => x.Id);
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.Option).HasMaxLength(8).IsRequired();
            b.Property(x => x.Topic).HasMaxLength(128).IsRequired();
            b.Property(x => x.TimestampUtc).IsRequired();

            // One vote per player per topic.
            b.HasIndex(x => new { x.PlayerId, x.Topic }).IsUnique();

            // Fast results aggregation by topic.
            b.HasIndex(x => x.Topic);
        }
    }
}
