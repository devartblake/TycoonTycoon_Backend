using Synaptix.Backend.Domain.Primitives;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class LearningModule : Entity
    {
        public string Title { get; private set; } = string.Empty;
        public string Description { get; private set; } = string.Empty;
        public string Category { get; private set; } = "General";
        public QuestionDifficulty Difficulty { get; private set; } = QuestionDifficulty.Easy;

        public int RewardXp { get; private set; }
        public int RewardCoins { get; private set; }

        public bool IsPublished { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public List<ModuleLesson> Lessons { get; private set; } = new();

        private LearningModule() { } // EF

        public LearningModule(
            string title,
            string description,
            string category,
            QuestionDifficulty difficulty,
            int rewardXp,
            int rewardCoins)
        {
            SetCore(title, description, category, difficulty, rewardXp, rewardCoins);
        }

        public void Update(
            string title,
            string description,
            string category,
            QuestionDifficulty difficulty,
            int rewardXp,
            int rewardCoins)
        {
            SetCore(title, description, category, difficulty, rewardXp, rewardCoins);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Publish()
        {
            IsPublished = true;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Unpublish()
        {
            IsPublished = false;
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        private void SetCore(
            string title,
            string description,
            string category,
            QuestionDifficulty difficulty,
            int rewardXp,
            int rewardCoins)
        {
            Title = title.Trim();
            Description = description.Trim();
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            Difficulty = difficulty;
            RewardXp = Math.Max(0, rewardXp);
            RewardCoins = Math.Max(0, rewardCoins);
        }
    }

    public sealed class ModuleLesson
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid ModuleId { get; private set; }
        public Guid QuestionId { get; private set; }
        public int Order { get; private set; }
        public string? Explanation { get; private set; }

        private ModuleLesson() { } // EF

        public ModuleLesson(Guid moduleId, Guid questionId, int order, string? explanation)
        {
            ModuleId = moduleId;
            QuestionId = questionId;
            Order = order;
            Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();
        }
    }

    public sealed class ModuleCompletion
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public Guid ModuleId { get; private set; }

        /// <summary>
        /// Stable event id passed to EconomyService.ApplyAsync — stored so re-delivering
        /// the same completion is idempotent even if the completion row already exists.
        /// </summary>
        public Guid EconomyEventId { get; private set; }

        public DateTimeOffset CompletedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private ModuleCompletion() { } // EF

        public ModuleCompletion(Guid playerId, Guid moduleId)
        {
            PlayerId = playerId;
            ModuleId = moduleId;
            EconomyEventId = Guid.NewGuid();
        }
    }
}
