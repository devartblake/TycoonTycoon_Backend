using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    /// <summary>
    /// Represents a mission definition (Daily / Weekly / Event-based).
    /// Missions are static templates; progress is tracked separately.
    /// </summary>
    public sealed class Mission : Entity
    {
        private Mission() { } // EF Core

        public Mission(
            string type,
            string key,
            string title,
            string description,
            int goal,
            int rewardXp,
            int rewardCoins = 0,
            int rewardDiamonds = 0,
            bool active = true)
        {
            Type = type;
            Key = key;
            Title = title;
            Description = description;
            Goal = goal;

            RewardXp = rewardXp;
            RewardCoins = rewardCoins;
            RewardDiamonds = rewardDiamonds;

            Active = active;
        }

        /// <summary>
        /// Mission category (Daily, Weekly, Seasonal, Event).
        /// </summary>
        public string Type { get; private set; } = string.Empty;

        /// <summary>
        /// Unique mission identifier (e.g. "daily_win_3").
        /// </summary>
        public string Key { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the title associated with the current instance.
        /// </summary>
        public string Title { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the description associated with the current instance.
        /// </summary>
        public string Description { get; private set; } = string.Empty;

        /// <summary>
        /// Required progress to complete the mission.
        /// </summary>
        public int Goal { get; private set; }

        /// <summary>
        /// XP rewarded upon completion.
        /// </summary>
        public int RewardXp { get; private set; } = 0;

        /// <summary>
        /// Coins rewarded upon completion.
        /// </summary>
        public int RewardCoins { get; private set; } = 0;

        /// <summary>
        /// Diamonds rewarded upon completion.
        /// </summary>
        public int RewardDiamonds { get; private set; } = 0;

        /// <summary>
        /// Whether the mission is currently available.
        /// </summary>
        public bool Active { get; private set; }

        public void Activate() => Active = true;
        public void Deactivate() => Active = false;
    }
}
