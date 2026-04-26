namespace Tycoon.Backend.Domain.Entities
{
    public sealed class RewardClaimRule
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public string RewardId { get; private set; } = string.Empty;
        public int MaxClaimsPerInterval { get; private set; }
        public string ResetInterval { get; private set; } = "daily"; // "daily" | "weekly" | "none"
        public bool IsActive { get; private set; } = true;
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private RewardClaimRule() { }

        public static RewardClaimRule Create(string rewardId, int maxClaimsPerInterval, string resetInterval)
            => new()
            {
                RewardId = rewardId.Trim().ToLowerInvariant(),
                MaxClaimsPerInterval = maxClaimsPerInterval,
                ResetInterval = resetInterval,
            };

        public void Update(int maxClaimsPerInterval, string resetInterval, bool isActive)
        {
            MaxClaimsPerInterval = maxClaimsPerInterval;
            ResetInterval = resetInterval;
            IsActive = isActive;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
