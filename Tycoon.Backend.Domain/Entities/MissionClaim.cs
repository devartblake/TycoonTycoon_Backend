using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Tracks a player's progress and completion state for a single mission definition.
    /// </summary>
    public sealed class MissionClaim : Entity
    {
        private MissionClaim() { } // EF

        public MissionClaim(Guid playerId, Guid missionId)
        {
            PlayerId = playerId;
            MissionId = missionId;
            Progress = 0;
            Completed = false;
            CompletedAtUtc = null;
            Claimed = false;
            ClaimedAtUtc = null;
            CreatedAtUtc = DateTime.UtcNow;
            UpdatedAtUtc = DateTime.UtcNow;
        }

        public Guid PlayerId { get; private set; }
        public Guid MissionId { get; private set; }

        public int Progress { get; private set; }
        public bool Completed { get; private set; }
        public DateTime? CompletedAtUtc { get; private set; }

        public bool Claimed { get; private set; }
        public DateTime? ClaimedAtUtc { get; private set; }

        public DateTime CreatedAtUtc { get; private set; }
        public DateTime UpdatedAtUtc { get; private set; }
        public DateTime? LastResetAtUtc { get; private set; }

        /// <summary>
        /// Adds progress (clamped) and marks completed when goal is reached.
        /// </summary>
        public void AddProgress(int amount, int goal)
        {
            if (amount <= 0) return;

            Progress = Math.Min(goal, Progress + amount);
            UpdatedAtUtc = DateTime.UtcNow;

            if (!Completed && Progress >= goal)
            {
                Completed = true;
                CompletedAtUtc = DateTime.UtcNow;
            }
        }

        public void MarkClaimed()
        {
            if (Claimed) return;
            if (!Completed) throw new InvalidOperationException("Mission must be completed before claiming.");

            Claimed = true;
            ClaimedAtUtc = DateTime.UtcNow;
            Touch(utcNow: DateTime.UtcNow);
        }

        /// <summary>
        /// Resets claim for new season / daily reset scenarios.
        /// </summary>
        public void Reset(DateTime utcNow)
        {
            Progress = 0;
            Completed = false;
            CompletedAtUtc = null;
            Claimed = false;
            ClaimedAtUtc = null;
            UpdatedAtUtc = utcNow;
            LastResetAtUtc = utcNow;
        }

        private void Touch(DateTime utcNow)
        {
            UpdatedAtUtc = utcNow;
        }
    }
}
