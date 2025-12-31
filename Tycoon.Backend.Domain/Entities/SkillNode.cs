using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class SkillNode
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Key { get; private set; } = string.Empty; // stable
        public SkillBranch Branch { get; private set; }
        public int Tier { get; private set; }

        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;

        // Stored as JSON for flexibility
        public string PrereqKeysJson { get; private set; } = "[]";
        public string CostsJson { get; private set; } = "[]";
        public string EffectsJson { get; private set; } = "{}";

        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private SkillNode() { }

        public SkillNode(
            string key,
            SkillBranch branch,
            int tier,
            string title,
            string description,
            string prereqKeysJson,
            string costsJson,
            string effectsJson)
        {
            Key = key.Trim();
            Branch = branch;
            Tier = tier;
            Title = title.Trim();
            Description = description.Trim();
            PrereqKeysJson = prereqKeysJson;
            CostsJson = costsJson;
            EffectsJson = effectsJson;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Touch() => UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public sealed class PlayerSkillUnlock
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public string NodeKey { get; private set; } = string.Empty;
        public DateTimeOffset UnlockedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private PlayerSkillUnlock() { }

        public PlayerSkillUnlock(Guid playerId, string nodeKey)
        {
            PlayerId = playerId;
            NodeKey = nodeKey.Trim();
            UnlockedAtUtc = DateTimeOffset.UtcNow;
        }
    }
}
