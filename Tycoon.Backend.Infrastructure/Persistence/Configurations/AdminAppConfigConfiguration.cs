using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminAppConfigConfiguration : IEntityTypeConfiguration<AdminAppConfig>
{
    public void Configure(EntityTypeBuilder<AdminAppConfig> b)
    {
        b.ToTable("admin_app_config");
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasMaxLength(32);
        b.Property(x => x.ApiBaseUrl).HasMaxLength(500).IsRequired();
        b.Property(x => x.EnableLogging).IsRequired();
        b.Property(x => x.FeatureFlagsJson).HasColumnType("jsonb").IsRequired();
        b.Property(x => x.UpdatedAt).IsRequired();
    }
}
