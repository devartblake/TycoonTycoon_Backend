using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class MissionConfiguration : IEntityTypeConfiguration<Mission>
    {
        public void Configure(EntityTypeBuilder<Mission> builder)
        {
            builder.HasKey(x => x.Id);

            // A mission definition should be uniquely addressable
            builder.HasIndex(x => new { x.Type, x.Key })
                   .IsUnique();

            builder.Property(x => x.Type).IsRequired();
            builder.Property(x => x.Key).IsRequired();
            builder.Property(x => x.Goal).IsRequired();
            builder.Property(x => x.Active).IsRequired();
        }
    }
}
