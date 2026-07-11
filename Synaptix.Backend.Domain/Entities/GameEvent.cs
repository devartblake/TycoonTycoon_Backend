using Synaptix.Backend.Domain.Events;
using Synaptix.Backend.Domain.Primitives;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class GameEvent : AggregateRoot
    {
        /// <summary>Symmetric battle-royale (rank-1 = last survivor).</summary>
        public const string ChampionBattleKind = "champion_battle";

        /// <summary>Asymmetric 1-vs-99: the tier's #1 defends the crown against challengers.</summary>
        public const string ChampionVsTierKind = "champion_vs_tier";

        public string Kind { get; private set; } = string.Empty;
        public int TierId { get; private set; }
        public GameEventStatus Status { get; private set; }
        public DateTimeOffset ScheduledAtUtc { get; private set; }
        public DateTimeOffset? OpenAtUtc { get; private set; }
        public int EntryFeeCoins { get; private set; }
        public int ReviveCostGems { get; private set; }
        public int JackpotPool { get; private set; }
        public int MaxParticipants { get; private set; }

        /// <summary>The seeded champion (tier #1) for a champion_vs_tier event; null otherwise/until seeded.</summary>
        public Guid? ChampionPlayerId { get; private set; }

        /// <summary>Sponsor-backed multiplier applied to the jackpot at close (default 1.0 = no change).</summary>
        public decimal JackpotMultiplier { get; private set; } = 1.0m;

        /// <summary>Name of the sponsor funding the jackpot multiplier, if any. Null = house-funded / no sponsor.</summary>
        public string? SponsorName { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; }

        /// <summary>Kinds whose jackpot accrues from entry fees and eliminations.</summary>
        public bool FeedsJackpot => Kind is ChampionBattleKind or ChampionVsTierKind;

        private GameEvent() { }

        public GameEvent(
            string kind,
            int tierId,
            DateTimeOffset scheduledAtUtc,
            DateTimeOffset? openAtUtc,
            int entryFeeCoins,
            int reviveCostGems,
            int maxParticipants)
        {
            Kind = kind;
            TierId = tierId;
            Status = GameEventStatus.Scheduled;
            ScheduledAtUtc = scheduledAtUtc;
            OpenAtUtc = openAtUtc;
            EntryFeeCoins = entryFeeCoins;
            ReviveCostGems = reviveCostGems;
            JackpotPool = 0;
            MaxParticipants = maxParticipants;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Open(DateTimeOffset now)
        {
            Status = GameEventStatus.Open;
            Raise(new GameEventOpenedEvent(Id, Kind, TierId, ScheduledAtUtc));
        }

        public void Start(DateTimeOffset now)
        {
            Status = GameEventStatus.Live;
            Raise(new GameEventStartedEvent(Id, Kind));
        }

        public void Close(DateTimeOffset now, int totalParticipants)
        {
            Status = GameEventStatus.Closed;
            Raise(new GameEventClosedEvent(Id, Kind, totalParticipants, JackpotPool));
        }

        public void AddToJackpot(int amount)
        {
            if (amount > 0)
                JackpotPool += amount;
        }

        /// <summary>Seed the champion (tier #1) once, at Open. No-op if already seeded.</summary>
        public void SeedChampion(Guid playerId)
        {
            if (ChampionPlayerId is null && playerId != Guid.Empty)
                ChampionPlayerId = playerId;
        }

        /// <summary>Set the sponsor jackpot multiplier (clamped to a sane 1.0–10.0 range).</summary>
        public void SetJackpotMultiplier(decimal multiplier)
        {
            JackpotMultiplier = Math.Clamp(multiplier, 1.0m, 10.0m);
        }

        /// <summary>
        /// Attribute the jackpot boost to a sponsor: sets the multiplier (clamped
        /// 1.0–10.0) and the sponsor's name. A blank/whitespace name clears the
        /// attribution (house-funded). No-op once the event has closed.
        /// </summary>
        public void ApplySponsor(string? sponsorName, decimal multiplier)
        {
            if (Status == GameEventStatus.Closed) return;
            SetJackpotMultiplier(multiplier);
            SponsorName = string.IsNullOrWhiteSpace(sponsorName) ? null : sponsorName.Trim();
        }

        /// <summary>The jackpot after the sponsor multiplier is applied.</summary>
        public int EffectiveJackpot =>
            (int)Math.Round(JackpotPool * JackpotMultiplier, MidpointRounding.AwayFromZero);

        /// <summary>The coins the sponsor's multiplier added on top of the base jackpot.</summary>
        public int SponsorBoostAmount => EffectiveJackpot - JackpotPool;
    }
}
