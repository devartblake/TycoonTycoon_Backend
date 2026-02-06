using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.UserId)
                .IsRequired();

            builder.Property(rt => rt.Token)
                .IsRequired()
                .HasMaxLength(128);

            builder.Property(rt => rt.DeviceId)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(rt => rt.ExpiresAt)
                .IsRequired();

            builder.Property(rt => rt.CreatedAt)
                .IsRequired();

            builder.Property(rt => rt.IsRevoked)
                .IsRequired();

            // Create indexes for queries
            builder.HasIndex(rt => rt.Token)
                .IsUnique();

            builder.HasIndex(rt => new { rt.UserId, rt.DeviceId, rt.IsRevoked });

            builder.HasIndex(rt => rt.ExpiresAt);
        }
    }
}
