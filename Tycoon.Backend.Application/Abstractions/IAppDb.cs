using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Tycoon.Backend.Application.Analytics.Models;
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
        DbSet<MatchmakingTicket> MatchmakingTickets { get; }
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
        DbSet<QuestionAnsweredAnalyticsEvent> QuestionAnsweredAnalyticsEvents { get; }
        DbSet<QuestionAnsweredDailyRollup> QuestionAnsweredDailyRollups { get; }
        DbSet<QuestionAnsweredPlayerDailyRollup> QuestionAnsweredPlayerDailyRollups { get; }
        DbSet<PlayerWallet> PlayerWallets { get; }
        DbSet<EconomyTransaction> EconomyTransactions { get; }
        DbSet<EconomyTransactionLine> EconomyTransactionLines { get; }
        DbSet<PlayerPowerup> PlayerPowerups { get; }
        DbSet<SkillNode> SkillNodes { get; }
        DbSet<PlayerSkillUnlock> PlayerSkillUnlocks { get; }
        DbSet<Season> Seasons { get; }
        DbSet<SeasonRewardClaim> SeasonRewardClaims { get; }
        DbSet<PlayerSeasonProfile> PlayerSeasonProfiles { get; }
        DbSet<SeasonPointTransaction> SeasonPointTransactions { get; }
        DbSet<AntiCheatFlag> AntiCheatFlags { get; }
        DbSet<PlayerModerationProfile> PlayerModerationProfiles { get; }
        DbSet<ModerationActionLog> ModerationActionLogs { get; }
        DbSet<FriendRequest> FriendRequests { get; }
        DbSet<FriendEdge> FriendEdges { get; }
        DbSet<Party> Parties { get; }
        DbSet<PartyMember> PartyMembers { get; }
        DbSet<PartyInvite> PartyInvites { get; }
        DbSet<PartyMatchmakingTicket> PartyMatchmakingTickets { get; }
        DbSet<PartyMatchLink> PartyMatchLinks { get; }
        DbSet<PartyMatchMember> PartyMatchMembers { get; }
        DbSet<SeasonRankSnapshotRow> SeasonRankSnapshots { get; }
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<AdminNotificationChannel> AdminNotificationChannels { get; }
        DbSet<AdminNotificationSchedule> AdminNotificationSchedules { get; }
        DbSet<AdminNotificationTemplate> AdminNotificationTemplates { get; }
        DbSet<AdminNotificationHistory> AdminNotificationHistory { get; }
        DbSet<AdminAppConfig> AdminAppConfigs { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        EntityEntry Entry(object entity);
    }
}
