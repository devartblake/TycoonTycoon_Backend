using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class AntiCheatFlagConfiguration : IEntityTypeConfiguration<AntiCheatFlag>
    {
        public void Configure(EntityTypeBuilder<AntiCheatFlag> b)
        {
            b.ToTable("anti_cheat_flags");
            b.HasKey(x => x.Id);

            b.Property(x => x.MatchId).IsRequired();
            b.HasIndex(x => x.MatchId);

            b.Property(x => x.PlayerId);
            b.HasIndex(x => x.PlayerId);

            b.Property(x => x.RuleKey).HasMaxLength(64).IsRequired();
            b.Property(x => x.Severity).IsRequired();
            b.Property(x => x.Action).IsRequired();

            b.Property(x => x.Message).HasMaxLength(300).IsRequired();
            b.Property(x => x.EvidenceJson);

            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.HasIndex(x => x.CreatedAtUtc);
            b.HasIndex(x => new { x.Severity, x.CreatedAtUtc });

            b.Property(x => x.ReviewedAtUtc);
            b.HasIndex(x => x.ReviewedAtUtc);

            b.Property(x => x.ReviewedBy).HasMaxLength(64);
            b.Property(x => x.ReviewNote).HasMaxLength(400);

            // Optional: speeds up “unreviewed severe flags”
            b.HasIndex(x => new { x.Severity, x.ReviewedAtUtc, x.CreatedAtUtc });
        }
    }
}
