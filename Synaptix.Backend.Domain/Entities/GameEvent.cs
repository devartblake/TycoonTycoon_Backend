using Synaptix.Backend.Domain.Events;
using Synaptix.Backend.Domain.Primitives;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class GameEvent : AggregateRoot
    {
        public string Kind { get; private set; } = string.Empty;
        public int TierId { get; private set; }
        public GameEventStatus Status { get; private set; }
        public DateTimeOffset ScheduledAtUtc { get; private set; }
        public DateTimeOffset? OpenAtUtc { get; private set; }
        public int EntryFeeCoins { get; private set; }
        public int ReviveCostGems { get; private set; }
        public int JackpotPool { get; private set; }
        public int MaxParticipants { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; }

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
    }
}
