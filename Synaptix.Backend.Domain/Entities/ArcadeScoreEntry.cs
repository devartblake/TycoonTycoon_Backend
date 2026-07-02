namespace Synaptix.Backend.Domain.Entities
{
    public class ArcadeScoreEntry
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string GameId { get; private set; } = string.Empty;
        public string Difficulty { get; private set; } = string.Empty;
        public int Score { get; private set; }
        public int DurationMs { get; private set; }
        public DateTimeOffset AchievedAtUtc { get; private set; }

        private ArcadeScoreEntry() { }

        public ArcadeScoreEntry(
            Guid playerId,
            string gameId,
            string difficulty,
            int score,
            int durationMs,
            DateTimeOffset achievedAtUtc)
        {
            PlayerId = playerId;
            GameId = gameId;
            Difficulty = difficulty;
            Score = score;
            DurationMs = durationMs;
            AchievedAtUtc = achievedAtUtc;
        }

        public bool IsNewBest(int otherScore, int otherDurationMs)
        {
            // Score desc, then duration asc
            if (otherScore > Score) return true;
            if (otherScore < Score) return false;
            return otherDurationMs < DurationMs;
        }

        public void UpdateScore(int score, int durationMs, DateTimeOffset achievedAt)
        {
            Score = score;
            DurationMs = durationMs;
            AchievedAtUtc = achievedAt;
        }
    }
}
