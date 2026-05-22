using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class GuardianChallengeConfiguration : IEntityTypeConfiguration<GuardianChallenge>
    {
        public void Configure(EntityTypeBuilder<GuardianChallenge> b)
        {
            b.ToTable("guardian_challenges");
            b.HasKey(x => x.Id);
            b.Property(x => x.SeasonId).IsRequired();
            b.Property(x => x.TierNumber).IsRequired();
            b.Property(x => x.ChallengerId).IsRequired();
            b.Property(x => x.GuardianId).IsRequired();
            b.Property(x => x.MatchId).IsRequired();
            b.Property(x => x.Status).IsRequired();
            b.Property(x => x.CreatedAtUtc).IsRequired();
            b.Property(x => x.ResolvedAtUtc);
            b.HasIndex(x => x.MatchId).IsUnique();
            b.HasIndex(x => new { x.SeasonId, x.TierNumber, x.ChallengerId });
            b.Ignore(x => x.DomainEvents);
        }
    }
}
