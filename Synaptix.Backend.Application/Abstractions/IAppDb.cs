using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Synaptix.Backend.Application.Analytics.Models;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Domain.Experiments;
using Synaptix.Backend.Domain.Personalization;

namespace Synaptix.Backend.Application.Abstractions
{
    public interface IAppDb
    {
        DbSet<Player> Players { get; }
        DbSet<PlayerLookupCode> PlayerLookupCodes { get; }
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
        DbSet<QuestionTaxonomySuggestion> QuestionTaxonomySuggestions { get; }
        DbSet<QuestionStudyFavorite> QuestionStudyFavorites { get; }
        DbSet<StudySet> StudySets { get; }
        DbSet<StudySetItem> StudySetItems { get; }
        DbSet<StudyCardState> StudyCardStates { get; }
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
        DbSet<PlayerNotification> PlayerNotifications { get; }
        DbSet<DirectMessageConversation> DirectMessageConversations { get; }
        DbSet<DirectMessageConversationParticipant> DirectMessageConversationParticipants { get; }
        DbSet<DirectMessage> DirectMessages { get; }
        DbSet<Party> Parties { get; }
        DbSet<PartyMember> PartyMembers { get; }
        DbSet<PartyInvite> PartyInvites { get; }
        DbSet<PartyMatchmakingTicket> PartyMatchmakingTickets { get; }
        DbSet<PartyMatchLink> PartyMatchLinks { get; }
        DbSet<PartyMatchMember> PartyMatchMembers { get; }
        DbSet<SeasonRankSnapshotRow> SeasonRankSnapshots { get; }
        DbSet<Vote> Votes { get; }
        DbSet<User> Users { get; }
        DbSet<RefreshToken> RefreshTokens { get; }
        DbSet<AdminNotificationChannel> AdminNotificationChannels { get; }
        DbSet<AdminNotificationSchedule> AdminNotificationSchedules { get; }
        DbSet<AdminNotificationTemplate> AdminNotificationTemplates { get; }
        DbSet<AdminNotificationHistory> AdminNotificationHistory { get; }
        DbSet<AdminAppConfig> AdminAppConfigs { get; }
        DbSet<GameBalanceConfig> GameBalanceConfigs { get; }
        DbSet<PlayerEconomySafeguardState> PlayerEconomySafeguardStates { get; }
        DbSet<GameEvent> GameEvents { get; }
        DbSet<GameEventParticipant> GameEventParticipants { get; }
        DbSet<GameEventPrizeClaim> GameEventPrizeClaims { get; }
        DbSet<TierGuardian> TierGuardians { get; }
        DbSet<GuardianChallenge> GuardianChallenges { get; }
        DbSet<TerritoryTile> TerritoryTiles { get; }
        DbSet<TerritoryDuel> TerritoryDuels { get; }
        DbSet<PlayerEventStats> PlayerEventStats { get; }
        DbSet<AdminEmailAcl> AdminEmailAcls { get; }
        DbSet<PlayerTransaction> PlayerTransactions { get; }
        DbSet<PlayerTransactionActor> PlayerTransactionActors { get; }
        DbSet<PlayerTransactionItem> PlayerTransactionItems { get; }
        DbSet<PlayerPreferences> PlayerPreferences { get; }
        DbSet<StoreItem> StoreItems { get; }
        DbSet<StoreStockPolicy> StoreStockPolicies { get; }
        DbSet<PlayerStoreStockState> PlayerStoreStockStates { get; }
        DbSet<FlashSale> FlashSales { get; }
        DbSet<RewardClaimRule> RewardClaimRules { get; }
        DbSet<SeasonRewardRule> SeasonRewardRules { get; }
        DbSet<LearningModule> LearningModules { get; }
        DbSet<ModuleLesson> ModuleLessons { get; }
        DbSet<ModuleCompletion> ModuleCompletions { get; }
        DbSet<StudySession> StudySessions { get; }
        DbSet<SetupReport> SetupReports { get; }

        // Reward Reactor
        DbSet<RewardSession> RewardSessions { get; }
        DbSet<RewardClaimLedger> RewardClaimLedger { get; }
        DbSet<RewardChainTicket> RewardChainTickets { get; }

        // Personalization
        DbSet<PlayerMindProfile> PlayerMindProfiles { get; }
        DbSet<PlayerBehaviorEvent> PlayerBehaviorEvents { get; }
        DbSet<PersonalizationRecommendation> PersonalizationRecommendations { get; }
        DbSet<PersonalizationRule> PersonalizationRules { get; }
        DbSet<PersonalizationAuditLog> PersonalizationAuditLogs { get; }

        // A/B Experiments
        DbSet<Experiment> Experiments { get; }
        DbSet<ExperimentVariant> ExperimentVariants { get; }
        DbSet<ExperimentAssignment> ExperimentAssignments { get; }

        Task<int> SaveChangesAsync(CancellationToken ct = default);
        EntityEntry Entry(object entity);
    }
}
