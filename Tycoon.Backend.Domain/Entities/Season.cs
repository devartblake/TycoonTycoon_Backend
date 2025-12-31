using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class Season
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public int SeasonNumber { get; private set; }
        public string Name { get; private set; } = string.Empty;
        public SeasonStatus Status { get; private set; }

        public DateTimeOffset StartsAtUtc { get; private set; }
        public DateTimeOffset EndsAtUtc { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private Season() { }

        public Season(int seasonNumber, string name, DateTimeOffset startsAtUtc, DateTimeOffset endsAtUtc)
        {
            SeasonNumber = seasonNumber;
            Name = (name ?? "").Trim();
            StartsAtUtc = startsAtUtc;
            EndsAtUtc = endsAtUtc;
            Status = SeasonStatus.Scheduled;
        }

        public void Activate()
        {
            Status = SeasonStatus.Active;
        }

        public void Close()
        {
            Status = SeasonStatus.Closed;
        }
    }

    public sealed class PlayerSeasonProfile
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid SeasonId { get; private set; }
        public Guid PlayerId { get; private set; }

        public int RankPoints { get; private set; }
        public int Wins { get; private set; }
        public int Losses { get; private set; }
        public int Draws { get; private set; }
        public int MatchesPlayed { get; private set; }

        public int Tier { get; private set; } = 1;       // 1..N
        public int TierRank { get; private set; } = 0;    // 1..100
        public int SeasonRank { get; private set; } = 0;  // 1..N

        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerSeasonProfile() { }

        public PlayerSeasonProfile(Guid seasonId, Guid playerId, int initialPoints)
        {
            SeasonId = seasonId;
            PlayerId = playerId;
            RankPoints = initialPoints;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyPoints(int delta)
        {
            RankPoints = Math.Max(0, RankPoints + delta);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ApplyMatchOutcome(bool win, bool draw)
        {
            MatchesPlayed++;
            if (draw) Draws++;
            else if (win) Wins++;
            else Losses++;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void SetRanks(int tier, int tierRank, int seasonRank)
        {
            Tier = tier;
            TierRank = tierRank;
            SeasonRank = seasonRank;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public sealed class SeasonPointTransaction
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public Guid EventId { get; private set; }          // idempotency key
        public Guid SeasonId { get; private set; }
        public Guid PlayerId { get; private set; }

        public string Kind { get; private set; } = string.Empty;
        public int Delta { get; private set; }
        public string? Note { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private SeasonPointTransaction() { }

        public SeasonPointTransaction(Guid eventId, Guid seasonId, Guid playerId, string kind, int delta, string? note)
        {
            EventId = eventId;
            SeasonId = seasonId;
            PlayerId = playerId;
            Kind = (kind ?? "").Trim();
            Delta = delta;
            Note = note;
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
