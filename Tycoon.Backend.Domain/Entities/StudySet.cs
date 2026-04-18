using Tycoon.Backend.Domain.Primitives;

namespace Tycoon.Backend.Domain.Entities
{
    public sealed class StudySet : Entity
    {
        public Guid PlayerId { get; private set; }
        public string Title { get; private set; } = string.Empty;
        public string? Description { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset UpdatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;
        public List<StudySetItem> Items { get; private set; } = new();

        private StudySet()
        {
        }

        public StudySet(Guid playerId, string title, string? description, IReadOnlyList<Guid> questionIds)
        {
            PlayerId = playerId;
            Update(title, description, questionIds);
            CreatedAtUtc = DateTimeOffset.UtcNow;
        }

        public void Update(string title, string? description, IReadOnlyList<Guid> questionIds)
        {
            UpdateMetadata(title, description);
            Items = questionIds
                .Distinct()
                .Select((questionId, index) => new StudySetItem(Id, questionId, index))
                .ToList();
        }

        public void UpdateMetadata(string title, string? description)
        {
            Title = title.Trim();
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
            UpdatedAtUtc = DateTimeOffset.UtcNow;
        }
    }

    public sealed class StudySetItem
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid StudySetId { get; private set; }
        public Guid QuestionId { get; private set; }
        public int Order { get; private set; }

        private StudySetItem()
        {
        }

        public StudySetItem(Guid studySetId, Guid questionId, int order)
        {
            StudySetId = studySetId;
            QuestionId = questionId;
            Order = order;
        }
    }
}
