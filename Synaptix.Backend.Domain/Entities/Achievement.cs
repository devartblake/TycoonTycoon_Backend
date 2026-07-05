namespace Synaptix.Backend.Domain.Entities
{
    /// <summary>
    /// Achievement catalog entry (admin-seeded, keyed by stable string key).
    /// </summary>
    public sealed class Achievement
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Key { get; private set; } = string.Empty; // stable
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public int Points { get; private set; }
        public string? IconUrl { get; private set; }
        public bool IsSecret { get; private set; }

        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private Achievement() { }

        public Achievement(
            string key,
            string title,
            string description,
            string category,
            int points,
            string? iconUrl,
            bool isSecret)
        {
            Key = key.Trim();
            Update(title, description, category, points, iconUrl, isSecret);
        }

        public void Update(
            string title,
            string description,
            string category,
            int points,
            string? iconUrl,
            bool isSecret)
        {
            Title = title.Trim();
            Description = description.Trim();
            Category = category.Trim();
            Points = points;
            IconUrl = iconUrl?.Trim();
            IsSecret = isSecret;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public sealed class PlayerAchievement
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string AchievementKey { get; private set; } = string.Empty;
        public DateTimeOffset UnlockedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerAchievement() { }

        public PlayerAchievement(Guid playerId, string achievementKey)
        {
            PlayerId = playerId;
            AchievementKey = achievementKey.Trim();
            UnlockedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
