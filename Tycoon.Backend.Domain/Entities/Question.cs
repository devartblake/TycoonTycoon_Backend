using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class Question
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Text { get; private set; } = string.Empty;
        public string Category { get; private set; } = "General";
        public QuestionDifficulty Difficulty { get; private set; } = QuestionDifficulty.Easy;

        public string CorrectOptionId { get; private set; } = string.Empty;

        public string? MediaKey { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public List<QuestionOption> Options { get; private set; } = new();
        public List<QuestionTag> Tags { get; private set; } = new();

        private Question() { } // EF

        public Question(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string? mediaKey)
        {
            SetCore(text, category, difficulty, correctOptionId, mediaKey);
        }

        public void Update(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string? mediaKey)
        {
            SetCore(text, category, difficulty, correctOptionId, mediaKey);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ReplaceOptions(IEnumerable<QuestionOption> options)
        {
            Options = options.ToList();
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void ReplaceTags(IEnumerable<string> tags)
        {
            Tags = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(t => new QuestionTag(Id, t))
                .ToList();

            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        private void SetCore(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string? mediaKey)
        {
            Text = text.Trim();
            Category = string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();
            Difficulty = difficulty;
            CorrectOptionId = correctOptionId.Trim();
            MediaKey = string.IsNullOrWhiteSpace(mediaKey) ? null : mediaKey.Trim();
        }
    }

    public sealed class QuestionOption
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid QuestionId { get; private set; }

        public string OptionId { get; private set; } = string.Empty; // stable string id (e.g., "A","B","C" or GUID string)
        public string Text { get; private set; } = string.Empty;

        private QuestionOption() { } // EF

        public QuestionOption(Guid questionId, string optionId, string text)
        {
            QuestionId = questionId;
            OptionId = optionId.Trim();
            Text = text.Trim();
        }
    }

    public sealed class QuestionTag
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid QuestionId { get; private set; }
        public string Tag { get; private set; } = string.Empty;

        private QuestionTag() { } // EF

        public QuestionTag(Guid questionId, string tag)
        {
            QuestionId = questionId;
            Tag = tag.Trim();
        }
    }
}
