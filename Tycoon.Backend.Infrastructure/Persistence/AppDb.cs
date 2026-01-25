using Microsoft.EntityFrameworkCore;
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
        // --- Analytics Events ---
        public DbSet<QuestionAnsweredAnalyticsEvent> QuestionAnsweredAnalyticsEvents => Set<QuestionAnsweredAnalyticsEvent>();

        // --- Analytics Rollups ---
        public DbSet<QuestionAnsweredDailyRollup> QuestionAnsweredDailyRollups => Set<QuestionAnsweredDailyRollup>();
        public DbSet<QuestionAnsweredPlayerDailyRollup> QuestionAnsweredPlayerDailyRollups => Set<QuestionAnsweredPlayerDailyRollup>();
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // QuestionAnsweredAnalyticsEvent
            modelBuilder.Entity<QuestionAnsweredAnalyticsEvent>(b =>
            {
                b.HasKey(x => x.Id);

                // Helpful for query patterns; not required, but generally beneficial.
                b.HasIndex(x => new { x.UpdatedAtUtc, x.PlayerId });
            });

            // QuestionAnsweredDailyRollup
            modelBuilder.Entity<QuestionAnsweredDailyRollup>(b =>
            {
                b.HasKey(x => x.Id);

                // Enforce uniqueness for upsert pattern
                b.HasIndex(x => new { x.Day, x.Mode, x.Category, x.Difficulty })
                 .IsUnique(false);
            });

            // QuestionAnsweredPlayerDailyRollup
            modelBuilder.Entity<QuestionAnsweredPlayerDailyRollup>(b =>
            {
                b.HasKey(x => x.Id);

                b.HasIndex(x => new { x.Day, x.PlayerId, x.Mode, x.Category, x.Difficulty })
                 .IsUnique(false);
            });
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