using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class Player : AggregateRoot
    {
        public new Guid Id { get; private set; } = Guid.NewGuid();
        public string Username { get; private set; } = string.Empty;
        public string CountryCode { get; private set; } = "US";

        public int Score { get; private set; }
        public Guid? TierId { get; private set; }
        public int Level { get; private set; }

        public double Xp { get; private set; }
        public int Coins { get; private set; }
        public int Diamonds { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

        private Player() { }
        public Player(string username, string countryCode = "US")
        {
            Username = username;
            CountryCode = countryCode;

            Level = 1;
            Xp = 0;

            Coins = 0;
            Diamonds = 0;
        }

        public void ApplyMatchResult(int scoreDelta, int xpEarned)
        {
            // scoreDelta can be negative; allow it but clamp score to >= 0
            Score = Math.Max(0, Score + scoreDelta);

            // XP earn is non-negative typically; AddXp already clamps
            AddXp(xpEarned);
        }

        public void SetTier(Guid tierId)
        {
            if (TierId == tierId) return;
            TierId = tierId;
        }

        public void AddScore(int amount)
        {
            if (amount < 0) return;
            Score += amount;
        }

        public void AddXp(double amount)
        {
            if (amount < 0) return;

            Xp += amount;

            // Level-up curve: requires Level * 100 XP to level up
            while (Xp >= Level * 100) 
            {
                Xp -= Level * 100;
                Level++; 
            }
        }

        public void AddCoins(int amount)
        {
            if (amount < 0) return;
            Coins += amount;
        }

        public void AddDiamonds(int amount)
        {
            if (amount < 0) return;
            Diamonds += amount;
        }

        public void OnMissionCompleted(Guid missionId, string type, string key, int rewardXp)
        {
            Raise(new MissionCompletedEvent(Id, missionId, type, key, rewardXp));
        }

        public void OnMissionClaimed(Guid missionId, int rewardXp)
        {
            Raise(new MissionClaimedEvent(Id, missionId, rewardXp));
        }
    }
}
