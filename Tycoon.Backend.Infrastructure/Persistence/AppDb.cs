using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Domain.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Events;
using Tycoon.Backend.Infrastructure.Persistence.Configurations;

namespace Tycoon.Backend.Infrastructure.Persistence
{
    /// <summary>
    /// Primary EF Core DbContext for Trivia Tycoon (PostgreSQL source of truth).
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
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<MatchRound> MatchRounds => Set<MatchRound>();
        public DbSet<MatchResult> MatchResults => Set<MatchResult>();
        public DbSet<MatchParticipantResult> MatchParticipantResults => Set<MatchParticipantResult>();
        public DbSet<MatchmakingTicket> MatchmakingTickets => Set<MatchmakingTicket>();
        public DbSet<Mission> Missions => Set<Mission>();
        public DbSet<MissionClaim> MissionClaims => Set<MissionClaim>();
        public DbSet<ProcessedGameplayEvent> ProcessedGameplayEvents => Set<ProcessedGameplayEvent>();
        public DbSet<LeaderboardEntry> LeaderboardEntries => Set<LeaderboardEntry>();
        public DbSet<Tier> Tiers => Set<Tier>();
        public DbSet<ReferralCode> ReferralCodes => Set<ReferralCode>();
        public DbSet<ReferralRedemption> ReferralRedemptions => Set<ReferralRedemption>();
        public DbSet<QrScanEvent> QrScanEvents => Set<QrScanEvent>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
        public DbSet<QuestionTag> QuestionTags => Set<QuestionTag>();
        public DbSet<PlayerWallet> PlayerWallets => Set<PlayerWallet>();
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
        public DbSet<Party> Parties => Set<Party>();
        public DbSet<PartyMember> PartyMembers => Set<PartyMember>();
        public DbSet<PartyInvite> PartyInvites => Set<PartyInvite>();
        public DbSet<PartyMatchmakingTicket> PartyMatchmakingTickets => Set<PartyMatchmakingTicket>();
        public DbSet<PartyMatchLink> PartyMatchLinks => Set<PartyMatchLink>();
        public DbSet<PartyMatchMember> PartyMatchMembers => Set<PartyMatchMember>();
        public DbSet<SeasonRewardClaim> SeasonRewardClaims => Set<SeasonRewardClaim>();
        public DbSet<SeasonRankSnapshotRow> SeasonRankSnapshots => Set<SeasonRankSnapshotRow>();

        public DbSet<QuestionAnsweredAnalyticsEvent> QuestionAnsweredAnalyticsEvents => throw new NotImplementedException();

        public DbSet<QuestionAnsweredDailyRollup> QuestionAnsweredDailyRollups => throw new NotImplementedException();

        public DbSet<QuestionAnsweredPlayerDailyRollup> QuestionAnsweredPlayerDailyRollups => throw new NotImplementedException();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new SeasonRewardClaimConfiguration());
            modelBuilder.ApplyConfiguration(new SeasonRankSnapshotRowConfiguration());

            // Ensure we pick up all IEntityTypeConfiguration<> classes.
            //
            // IMPORTANT: We intentionally scan BOTH the Infrastructure assembly (this DbContext)
            // and the Domain assembly (where entity CLR types live). This makes the system more
            // robust to accidental misplacement of configuration classes and prevents silent
            // model drift (e.g., join entities being discovered without keys).
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDb).Assembly);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(Player).Assembly);

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
    }
}
