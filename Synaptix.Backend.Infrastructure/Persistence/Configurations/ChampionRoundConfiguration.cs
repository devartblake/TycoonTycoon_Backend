using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence.Configurations
{
    public sealed class ChampionRoundConfiguration : IEntityTypeConfiguration<ChampionRound>
    {
        public void Configure(EntityTypeBuilder<ChampionRound> b)
        {
            b.ToTable("champion_rounds");
            b.HasKey(x => x.Id);

            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.RoundNumber).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.CorrectOptionId).HasMaxLength(64).IsRequired();
            b.Property(x => x.StartedAtUtc).IsRequired();
            b.Property(x => x.DeadlineUtc).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();

            b.HasIndex(x => new { x.GameEventId, x.RoundNumber }).IsUnique();
            b.HasIndex(x => new { x.GameEventId, x.Status });
        }
    }

    public sealed class ChampionRoundAnswerConfiguration : IEntityTypeConfiguration<ChampionRoundAnswer>
    {
        public void Configure(EntityTypeBuilder<ChampionRoundAnswer> b)
        {
            b.ToTable("champion_round_answers");
            b.HasKey(x => x.Id);

            b.Property(x => x.RoundId).IsRequired();
            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.PlayerId).IsRequired();
            b.Property(x => x.SelectedOptionId).HasMaxLength(64).IsRequired();
            b.Property(x => x.SubmittedAtUtc).IsRequired();

            b.HasIndex(x => new { x.RoundId, x.PlayerId }).IsUnique();
        }
    }

    public sealed class ChampionDuelConfiguration : IEntityTypeConfiguration<ChampionDuel>
    {
        public void Configure(EntityTypeBuilder<ChampionDuel> b)
        {
            b.ToTable("champion_duels");
            b.HasKey(x => x.Id);

            b.Property(x => x.GameEventId).IsRequired();
            b.Property(x => x.ChampionPlayerId).IsRequired();
            b.Property(x => x.ChallengerPlayerId).IsRequired();
            b.Property(x => x.QuestionId).IsRequired();
            b.Property(x => x.CorrectOptionId).HasMaxLength(64).IsRequired();
            b.Property(x => x.StartedAtUtc).IsRequired();
            b.Property(x => x.DeadlineUtc).IsRequired();
            b.Property(x => x.Status).HasMaxLength(16).IsRequired();
            b.Property(x => x.ChampionOptionId).HasMaxLength(64);
            b.Property(x => x.ChallengerOptionId).HasMaxLength(64);

            b.HasIndex(x => new { x.GameEventId, x.Status });
        }
    }
}
