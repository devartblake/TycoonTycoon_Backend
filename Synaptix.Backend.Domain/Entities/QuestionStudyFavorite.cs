namespace Synaptix.Backend.Domain.Entities
{
    public sealed class QuestionStudyFavorite
    {
        public Guid Id { get; private set; } = Guid.NewGuid();
        public Guid PlayerId { get; private set; }
        public Guid QuestionId { get; private set; }
        public DateTimeOffset CreatedAtUtc { get; private set; } = DateTimeOffset.UtcNow;

        private QuestionStudyFavorite()
        {
        }

        public QuestionStudyFavorite(Guid playerId, Guid questionId)
        {
            PlayerId = playerId;
            QuestionId = questionId;
        }
    }
}
