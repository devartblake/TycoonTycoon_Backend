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
}
