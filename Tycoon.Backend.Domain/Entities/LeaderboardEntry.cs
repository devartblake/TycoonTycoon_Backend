namespace Tycoon.Backend.Domain.Entities
{
    public class LeaderboardEntry
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public int GlobalRank { get; private set; }
        public int TierRank { get; private set; }
        public int TierId { get; private set; }
        public int Score { get; private set; }
        public double XpProgress { get; private set; } // 0..1

        private LeaderboardEntry() { }
        public LeaderboardEntry(Guid playerId, int tierId, int score, double xpProgress)
        { 
            PlayerId = playerId;
            TierId = tierId;
            Score = score;
            XpProgress = xpProgress;
        }

        public void UpdateScore(int delta) => Score += delta;
        public void SetRanks(int global, int tierRank) => (GlobalRank, TierRank) = (global, tierRank);

        public void SetTierSnapshot(int tierId, int score, double xpProgress)
        {
            TierId = tierId;
            Score = score;
            XpProgress = xpProgress;
        }
    }
}
