using Serilog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Analytics.Models;
using Synaptix.Backend.Domain.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Domain.Experiments;
using Synaptix.Backend.Domain.Personalization;
using Synaptix.Backend.Infrastructure.Events;
using Synaptix.Backend.Infrastructure.Persistence.Configurations;
using Synaptix.Entitlements.Entities;

namespace Synaptix.Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Primary EF Core DbContext for Synaptix (PostgreSQL source of truth).
    /// Treat this as the transactional boundary (no UnitOfWork abstraction).
    /// </summary>
    public sealed class AppDb : DbContext, IAppDb
    {
        private readonly IDomainEventDispatcher? _dispatcher;

        public AppDb(DbContextOptions<AppDb> options, IDomainEventDispatcher? dispatcher = null)
            : base(options)
        {
            _dispatcher = dispatcher;
        }

        public DbSet<Player> Players => Set<Player>();
        public DbSet<PlayerLookupCode> PlayerLookupCodes => Set<PlayerLookupCode>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<MatchRound> MatchRounds => Set<MatchRound>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();
        public DbSet<MatchParticipantResult> MatchParticipantResults => Set<MatchParticipantResult>();
        public DbSet<MatchmakingTicket> MatchmakingTickets => Set<MatchmakingTicket>();
        public DbSet<Mission> Missions => Set<Mission>();
        public DbSet<MissionClaim> MissionClaims => Set<MissionClaim>();
        public DbSet<ProcessedGameplayEvent> ProcessedGameplayEvents => Set<ProcessedGameplayEvent>();
        public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
        public DbSet<ReferralCode> ReferralCodes => Set<ReferralCode>();
        public DbSet<ReferralRedemption> ReferralRedemptions => Set<ReferralRedemption>();
        public DbSet<QrScanEvent> QrScanEvents => Set<QrScanEvent>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
        public DbSet<QuestionTaxonomySuggestion> QuestionTaxonomySuggestions => Set<QuestionTaxonomySuggestion>();
        public DbSet<QuestionStudyFavorite> QuestionStudyFavorites => Set<QuestionStudyFavorite>();
        public DbSet<StudySet> StudySets => Set<StudySet>();
        public DbSet<StudySetItem> StudySetItems => Set<StudySetItem>();
        public DbSet<StudyCardState> StudyCardStates => Set<StudyCardState>();
        public DbSet<PlayerWallet> PlayerWallets => Set<PlayerWallet>();
        public DbSet<SpinClaim> SpinClaims => Set<SpinClaim>();
        public DbSet<DailyRewardClaim> DailyRewardClaims => Set<DailyRewardClaim>();
        public DbSet<WeeklyStreakState> WeeklyStreakStates => Set<WeeklyStreakState>();
        public DbSet<EconomyTransaction> EconomyTransactions => Set<EconomyTransaction>();
        public DbSet<EconomyTransactionLine> EconomyTransactionLines => Set<EconomyTransactionLine>();
        public DbSet<PlayerPowerup> PlayerPowerups => Set<PlayerPowerup>();
        public DbSet<SkillNode> SkillNodes => Set<SkillNode>();
        public DbSet<PlayerSkillUnlock> PlayerSkillUnlocks => Set<PlayerSkillUnlock>();
        public DbSet<Season> Seasons => Set<Season>();
        public DbSet<PlayerSeasonProfile> PlayerSeasonProfiles => Set<PlayerSeasonProfile>();
        public DbSet<SeasonPointTransaction> SeasonPointTransactions => Set<SeasonPointTransaction>();
        public DbSet<AntiCheatFlag> AntiCheatFlags => Set<AntiCheatFlag>();
        public DbSet<PlayerModerationProfile> PlayerModerationProfiles => Set<PlayerModerationProfile>();
        public DbSet<ModerationActionLog> ModerationActionLogs => Set<ModerationActionLog>();
        public DbSet<FriendRequest> FriendRequests => Set<FriendRequest>();
        public DbSet<FriendEdge> FriendEdges => Set<FriendEdge>();
        public DbSet<PlayerNotification> PlayerNotifications => Set<PlayerNotification>();
        public DbSet<DirectMessageConversation> DirectMessageConversations => Set<DirectMessageConversation>();
        public DbSet<DirectMessageConversationParticipant> DirectMessageConversationParticipants => Set<DirectMessageConversationParticipant>();
        public DbSet<DirectMessage> DirectMessages => Set<DirectMessage>();
        public DbSet<Party> Parties => Set<Party>();
        public DbSet<PartyMember> PartyMembers => Set<PartyMember>();
        public DbSet<PartyInvite> PartyInvites => Set<PartyInvite>();
        public DbSet<PartyMatchmakingTicket> PartyMatchmakingTickets => Set<PartyMatchmakingTicket>();
        public DbSet<PartyMatchLink> PartyMatchLinks => Set<PartyMatchLink>();
        public DbSet<PartyMatchMember> PartyMatchMembers => Set<PartyMatchMember>();
        public DbSet<SeasonRewardClaim> SeasonRewardClaims => Set<SeasonRewardClaim>();
        public DbSet<SeasonRankSnapshotRow> SeasonRankSnapshots => Set<SeasonRankSnapshotRow>();
        public DbSet<Vote> Votes => Set<Vote>();
        public DbSet<User> Users => Set<User>();
        public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
        public DbSet<OtpToken> OtpTokens => Set<OtpToken>();
        public DbSet<AdminNotificationChannel> AdminNotificationChannels => Set<AdminNotificationChannel>();
        public DbSet<AdminNotificationSchedule> AdminNotificationSchedules => Set<AdminNotificationSchedule>();
        public DbSet<AdminNotificationTemplate> AdminNotificationTemplates => Set<AdminNotificationTemplate>();
        public DbSet<AdminNotificationHistory> AdminNotificationHistory => Set<AdminNotificationHistory>();
        public DbSet<AdminAppConfig> AdminAppConfigs => Set<AdminAppConfig>();
        public DbSet<GameBalanceConfig> GameBalanceConfigs => Set<GameBalanceConfig>();
        public DbSet<PlayerEconomySafeguardState> PlayerEconomySafeguardStates => Set<PlayerEconomySafeguardState>();
        public DbSet<Tier> Tiers => Set<Tier>();
        public DbSet<QuestionAnsweredAnalyticsEvent> QuestionAnsweredAnalyticsEvents => Set<QuestionAnsweredAnalyticsEvent>();
        public DbSet<QuestionAnsweredDailyRollup> QuestionAnsweredDailyRollups => Set<QuestionAnsweredDailyRollup>();
        public DbSet<QuestionAnsweredPlayerDailyRollup> QuestionAnsweredPlayerDailyRollups => Set<QuestionAnsweredPlayerDailyRollup>();
        public DbSet<GameEvent> GameEvents => Set<GameEvent>();
        public DbSet<GameEventParticipant> GameEventParticipants => Set<GameEventParticipant>();
        public DbSet<GameEventPrizeClaim> GameEventPrizeClaims => Set<GameEventPrizeClaim>();
        public DbSet<TierGuardian> TierGuardians => Set<TierGuardian>();
        public DbSet<GuardianChallenge> GuardianChallenges => Set<GuardianChallenge>();
        public DbSet<TerritoryTile> TerritoryTiles => Set<TerritoryTile>();
        public DbSet<TerritoryDuel> TerritoryDuels => Set<TerritoryDuel>();
        public DbSet<PlayerEventStats> PlayerEventStats => Set<PlayerEventStats>();
        public DbSet<AdminEmailAcl> AdminEmailAcls => Set<AdminEmailAcl>();
        public DbSet<PlayerTransaction> PlayerTransactions => Set<PlayerTransaction>();
        public DbSet<PlayerTransactionActor> PlayerTransactionActors => Set<PlayerTransactionActor>();
        public DbSet<PlayerTransactionItem> PlayerTransactionItems => Set<PlayerTransactionItem>();
        public DbSet<PlayerPreferences> PlayerPreferences => Set<PlayerPreferences>();
        public DbSet<StoreItem> StoreItems => Set<StoreItem>();
        public DbSet<StoreStockPolicy> StoreStockPolicies => Set<StoreStockPolicy>();
        public DbSet<PlayerStoreStockState> PlayerStoreStockStates => Set<PlayerStoreStockState>();
        public DbSet<ParentalPurchaseControl> ParentalPurchaseControls => Set<ParentalPurchaseControl>();
        public DbSet<FlashSale> FlashSales => Set<FlashSale>();
        public DbSet<RewardClaimRule> RewardClaimRules => Set<RewardClaimRule>();
        public DbSet<SeasonRewardRule> SeasonRewardRules => Set<SeasonRewardRule>();
        public DbSet<LearningModule> LearningModules => Set<LearningModule>();
        public DbSet<ModuleLesson> ModuleLessons => Set<ModuleLesson>();
        public DbSet<ModuleCompletion> ModuleCompletions => Set<ModuleCompletion>();
        public DbSet<StudySession> StudySessions => Set<StudySession>();
        public DbSet<SetupReport> SetupReports => Set<SetupReport>();

        // Reward Reactor
        public DbSet<RewardSession> RewardSessions => Set<RewardSession>();
        public DbSet<RewardClaimLedger> RewardClaimLedger => Set<RewardClaimLedger>();
        public DbSet<RewardChainTicket> RewardChainTickets => Set<RewardChainTicket>();

        // Personalization
        public DbSet<PlayerMindProfile> PlayerMindProfiles => Set<PlayerMindProfile>();
        public DbSet<PlayerBehaviorEvent> PlayerBehaviorEvents => Set<PlayerBehaviorEvent>();
        public DbSet<PersonalizationRecommendation> PersonalizationRecommendations => Set<PersonalizationRecommendation>();
        public DbSet<PersonalizationRule> PersonalizationRules => Set<PersonalizationRule>();
        public DbSet<PersonalizationAuditLog> PersonalizationAuditLogs => Set<PersonalizationAuditLog>();

        // A/B Experiments
        public DbSet<Experiment> Experiments => Set<Experiment>();
        public DbSet<ExperimentVariant> ExperimentVariants => Set<ExperimentVariant>();
        public DbSet<ExperimentAssignment> ExperimentAssignments => Set<ExperimentAssignment>();

        // Entitlements
        public DbSet<PlayerEntitlement> PlayerEntitlements => Set<PlayerEntitlement>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Applies all IEntityTypeConfiguration<> classes in the Infrastructure assembly,
            // including SeasonRewardClaimConfiguration and SeasonRankSnapshotRowConfiguration.
            // Do NOT manually call ApplyConfiguration for types already in this assembly —
            // doing so causes them to be applied twice, which can produce duplicate constraints.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDb).Assembly);

            // Hard guardrail: fail fast with an actionable error if any entity type is discovered
            // without a primary key. This prevents opaque "requires a primary key" failures later
            // in migration/runtime and makes it obvious which types are misconfigured.
            ValidateAllEntityTypesHaveKeys((IModel)modelBuilder.Model);
        }

        private static void ValidateAllEntityTypesHaveKeys(IModel model)
        {
            // EF will sometimes create shared-type entity types (e.g., many-to-many dictionaries).
            // Those are typically keyless by design. We only guard *real* entities.
            var offenders = model
            .GetEntityTypes()
            .Where(et =>
                !et.IsOwned() &&
                et.FindPrimaryKey() is null &&
                et.GetAnnotations().All(a => a.Name != "Relational:ViewName"))
            .Select(et => et.DisplayName())
            .OrderBy(n => n)
            .ToList();

            if (offenders.Count == 0)
            {
                return;
            }

            throw new InvalidOperationException(
                "EF model validation failed: the following entity types have no primary key configured. " +
                "Add HasKey(...) in an IEntityTypeConfiguration<> (preferred) or mark the type as keyless via HasNoKey(). " +
                "Offenders: " + string.Join(", ", offenders));
        }

        public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            // Collect events BEFORE commit
            var events = DomainEventCollector.CollectAndClear(this);

            // Commit
            var result = await base.SaveChangesAsync(ct);

            // Dispatch AFTER commit (only if dispatcher is available; MigrationService can run without it)
            if (_dispatcher is not null && events.Count > 0)
            {
                await _dispatcher.DispatchAsync(events, ct);
            }

            return result;
        }

        /// <summary>
        /// Migration-only diagnostic: logs entity types that would break migrations because they have no PK.
        /// Do NOT call during normal runtime; call from MigrationService before Database.MigrateAsync().
        /// </summary>
        public void LogEntitiesMissingPrimaryKeysForMigrations(ILogger log)
        {
            var model = Model;

            var offenders = model
                .GetEntityTypes()
                .Where(et =>
                {
                    var ro = (IReadOnlyEntityType)et;
                    return !ro.IsOwned()
                           && ro.FindPrimaryKey() is null;
                })
                .Select(et => et.DisplayName())
                .OrderBy(n => n)
                .ToList();

            if (offenders.Count == 0)
            {
                log.Information("EF Model Guard: all entity types have primary keys (or are owned/keyless).");
                return;
            }

            log.Warning("EF Model Guard: {Count} entity types have NO primary key (will break migrations): {Offenders}",
                offenders.Count, offenders);
        }
    }
}
