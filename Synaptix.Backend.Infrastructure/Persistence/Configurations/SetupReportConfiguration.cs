using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations;

public sealed class SetupReportConfiguration : IEntityTypeConfiguration<SetupReport>
{
    public void Configure(EntityTypeBuilder<SetupReport> builder)
    {
        builder.ToTable("setup_reports");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.Source)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.WarningCount).IsRequired();
        builder.Property(x => x.GeneratedAtUtc).IsRequired();
        builder.Property(x => x.CreatedAtUtc).IsRequired();

        builder.Property(x => x.ReportJson)
            .IsRequired()
            .HasColumnType("jsonb");

        builder.HasIndex(x => x.CreatedAtUtc);
        builder.HasIndex(x => new { x.Status, x.CreatedAtUtc });
    }
}
