using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Records a single player vote (e.g. audience !A / !B / !C during a live match).
    /// One vote per player per topic — enforce uniqueness at the application layer.
    /// </summary>
    public sealed class Vote : AggregateRoot
    {
        public Guid PlayerId { get; private set; }

        /// <summary>
        /// The voted option, e.g. "!A", "!B", "!C".
        /// </summary>
        public string Option { get; private set; } = string.Empty;

        /// <summary>
        /// Arbitrary grouping key (e.g. match ID or poll slug) so results can be
        /// aggregated per context.
        /// </summary>
        public string Topic { get; private set; } = string.Empty;

        public DateTimeOffset TimestampUtc { get; private set; }

        private Vote() { } // EF Core

        public Vote(Guid playerId, string option, string topic)
        {
            PlayerId = playerId;
            Option = option.Trim();
            Topic = topic.Trim();
            TimestampUtc = DateTimeOffset.UtcNow;

            Raise(new VoteCastEvent(
                VoteId: Id,
                PlayerId: playerId,
                Option: Option,
                Topic: Topic,
                CastAtUtc: TimestampUtc.UtcDateTime
            ));
        }
    }
}
