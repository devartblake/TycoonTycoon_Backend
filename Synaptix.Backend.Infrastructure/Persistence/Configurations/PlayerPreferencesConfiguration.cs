using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class PlayerPreferencesConfiguration : IEntityTypeConfiguration<PlayerPreferences>
    {
        public void Configure(EntityTypeBuilder<PlayerPreferences> builder)
        {
            builder.HasKey(x => x.Id);

            builder.HasIndex(x => x.PlayerId).IsUnique();

            builder.Property(x => x.SynaptixMode).HasMaxLength(32).IsRequired();
            builder.Property(x => x.PreferredSurface).HasMaxLength(32).IsRequired();
            builder.Property(x => x.TonePreference).HasMaxLength(32).IsRequired();
        }
    }
}
