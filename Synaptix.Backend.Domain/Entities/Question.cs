using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Domain.Entities
{
    public sealed class Question
    {
        public Guid Id { get; private set; } = Guid.NewGuid();

        public string Text { get; private set; } = string.Empty;
        public string Category { get; private set; } = "General";
        public QuestionDifficulty Difficulty { get; private set; } = QuestionDifficulty.Easy;
        public string CanonicalCategory { get; private set; } = "general";
        public string DisplayCategory { get; private set; } = "General";
        public string? Subject { get; private set; }
        public string? Topic { get; private set; }
        public string? Subtopic { get; private set; }
        public string? GradeBand { get; private set; }
        public string? AgeGroup { get; private set; }
        public string? Audience { get; private set; }
        public string? SourceDataset { get; private set; }
        public string? SourceQuestionId { get; private set; }
        public string QuestionType { get; private set; } = "multiple_choice";
        public string MediaType { get; private set; } = "text";
        public string TaxonomyTagsJson { get; private set; } = "[]";

        public string CorrectOptionId { get; private set; } = string.Empty;
        public string Status { get; private set; } = "Draft";
        public DateTimeOffset? StatusChangedAtUtc { get; private set; }

        public string? MediaKey { get; private set; }

        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        public List<QuestionOption> Options { get; private set; } = new();
        public List<QuestionTag> Tags { get; private set; } = new();

        private Question() { } // EF

        public Question(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string? mediaKey)
        {
            SetCore(text, category, difficulty, correctOptionId, mediaKey);
            SetTaxonomy(null, null, null, null, null, null, null, null, null, null, null, null);
        }

        public void Update(string text, string category, QuestionDifficulty difficulty, string correctOptionId, string? mediaKey)
        {
            SetCore(text, category, difficulty, correctOptionId, mediaKey);
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void SetTaxonomy(
            string? canonicalCategory,
            string? displayCategory,
            string? subject,
            string? topic,
            string? subtopic,
            string? gradeBand,
            string? ageGroup,
            string? audience,
            string? sourceDataset,
            string? sourceQuestionId,
            string? questionType,
            string? mediaType,
            string? taxonomyTagsJson = null)
        {
            CanonicalCategory = string.IsNullOrWhiteSpace(canonicalCategory)
                ? NormalizeKey(Category)
                : NormalizeKey(canonicalCategory);
            DisplayCategory = string.IsNullOrWhiteSpace(displayCategory)
                ? Category
                : displayCategory.Trim();
            Subject = NormalizeNullable(subject);
            Topic = NormalizeNullable(topic);
            Subtopic = NormalizeNullable(subtopic);
            GradeBand = NormalizeNullable(gradeBand);
            AgeGroup = NormalizeNullable(ageGroup);
            Audience = NormalizeNullable(audience);
            SourceDataset = NormalizeNullable(sourceDataset);
            SourceQuestionId = NormalizeNullable(sourceQuestionId);
            QuestionType = string.IsNullOrWhiteSpace(questionType) ? "multiple_choice" : NormalizeKey(questionType);
            MediaType = string.IsNullOrWhiteSpace(mediaType) ? InferMediaType(MediaKey) : NormalizeKey(mediaType);
            TaxonomyTagsJson = string.IsNullOrWhiteSpace(taxonomyTagsJson) ? "[]" : taxonomyTagsJson.Trim();
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

        public void SetStatus(string status)
        {
            var normalized = string.IsNullOrWhiteSpace(status) ? "Draft" : status.Trim();
            if (normalized is not ("Draft" or "Approved" or "Rejected" or "Archived"))
                throw new ArgumentException("Status must be one of: Draft, Approved, Rejected, Archived.");

            Status = normalized;
            StatusChangedAtUtc = DateTimeOffset.UtcNow;
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

        private static string? NormalizeNullable(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        private static string NormalizeKey(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "general"
                : value.Trim().ToLowerInvariant()
                    .Replace("&", "and")
                    .Replace(" ", "_")
                    .Replace("-", "_");

        private static string InferMediaType(string? mediaKey)
        {
            if (string.IsNullOrWhiteSpace(mediaKey)) return "text";
            var lower = mediaKey.Trim().ToLowerInvariant();
            if (lower.EndsWith(".mp3") || lower.EndsWith(".wav") || lower.EndsWith(".ogg")) return "audio";
            if (lower.EndsWith(".mp4") || lower.EndsWith(".webm") || lower.EndsWith(".mov")) return "video";
            return "image";
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

        public void UpdateText(string text)
        {
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
