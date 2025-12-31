using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
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

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Optional: ensures EF picks up IEntityTypeConfiguration<> if you add them later.
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDb).Assembly);
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