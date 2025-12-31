using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Abstractions
{
    public interface IAppDb
    {
        DbSet<Player> Players { get; }
        DbSet<Match> Matches { get; }
        DbSet<MatchRound> MatchRounds { get; }
        DbSet<MatchResult> MatchResults { get; }
        DbSet<MatchParticipantResult> MatchParticipantResults { get; }

        DbSet<Mission> Missions { get; }
        DbSet<MissionClaim> MissionClaims { get; }
        DbSet<ProcessedGameplayEvent> ProcessedGameplayEvents { get; }
        DbSet<LeaderboardEntry> LeaderboardEntries { get; }
        DbSet<Tier> Tiers { get; }
        DbSet<ReferralCode> ReferralCodes { get; }
        DbSet<ReferralRedemption> ReferralRedemptions { get; }
        DbSet<QrScanEvent> QrScanEvents { get; }
        DbSet<Question> Questions { get; }
        DbSet<QuestionOption> QuestionOptions { get; }
        DbSet<QuestionTag> QuestionTags { get; }
        DbSet<PlayerWallet> PlayerWallets { get; }
        DbSet<EconomyTransaction> EconomyTransactions { get; }
        DbSet<EconomyTransactionLine> EconomyTransactionLines { get; }
        DbSet<PlayerPowerup> PlayerPowerups { get; }
        DbSet<SkillNode> SkillNodes { get; }
        DbSet<PlayerSkillUnlock> PlayerSkillUnlocks { get; }
        DbSet<Season> Seasons { get; }
        DbSet<PlayerSeasonProfile> PlayerSeasonProfiles { get; }
        DbSet<SeasonPointTransaction> SeasonPointTransactions { get; }
        DbSet<AntiCheatFlag> AntiCheatFlags { get; }
        DbSet<PlayerModerationProfile> PlayerModerationProfiles { get; }
        DbSet<ModerationActionLog> ModerationActionLogs { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        EntityEntry Entry(object entity);
    }
}
