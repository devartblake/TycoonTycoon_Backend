using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class AdminEmailAclConfiguration : IEntityTypeConfiguration<AdminEmailAcl>
{
    public void Configure(EntityTypeBuilder<AdminEmailAcl> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Email)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.ListType)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(16);

        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.AddedBy)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(e => e.CreatedAtUtc)
            .IsRequired();

        builder.Property(e => e.UpdatedAtUtc)
            .IsRequired();

        // One entry per email — an email cannot be on both lists simultaneously
        builder.HasIndex(e => e.NormalizedEmail)
            .IsUnique();

        // Fast filtering by list type
        builder.HasIndex(e => e.ListType);
    }
}
