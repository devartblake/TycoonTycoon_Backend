using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.HasKey(u => u.Id);

            builder.Property(u => u.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(u => u.Handle)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(u => u.PasswordHash)
                .IsRequired();

            builder.Property(u => u.Country)
                .HasMaxLength(2);

            builder.Property(u => u.Tier)
                .HasMaxLength(10);

            builder.Property(u => u.Mmr)
                .IsRequired();

            builder.Property(u => u.CreatedAt)
                .IsRequired();

            builder.Property(u => u.IsActive)
                .IsRequired();

            // Ignore the Flags dictionary as it's not mapped to database
            builder.Ignore(u => u.Flags);

            // Create indexes for performance
            builder.HasIndex(u => u.Email)
                .IsUnique();

            builder.HasIndex(u => u.Handle)
                .IsUnique();

            builder.HasIndex(u => u.CreatedAt);
        }
    }
}
