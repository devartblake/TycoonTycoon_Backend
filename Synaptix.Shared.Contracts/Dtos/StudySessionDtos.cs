namespace Synaptix.Shared.Contracts.Dtos
{
    public static class StudySessionModes
    {
        public const string SelfTest = "SelfTest";
        public const string Flashcard = "Flashcard";
    }

    public sealed record CreateStudySessionRequest(
        string StudySetId,
        string? Mode = null,
        int Count = 0
    );

    public sealed record UpdateStudySessionProgressRequest(
        Guid? QuestionId,
        string? SelectedOptionId,
        int? CurrentQuestionIndex,
        string? FlashcardAction = null,
        int? Confidence = null,
        bool? AnswerRevealed = null,
        bool IsCompleted = false
    );

    public sealed record StudySessionInteractionDto(
        Guid QuestionId,
        string? FlashcardAction,
        int? Confidence,
        bool AnswerRevealed,
        DateTimeOffset? LastInteractedAtUtc
    );

    public sealed record StudySessionDto(
        Guid Id,
        string StudySetId,
        string Mode,
        string Title,
        string Kind,
        string Category,
        int QuestionCount,
        int AnsweredCount,
        int CorrectCount,
        int CurrentQuestionIndex,
        bool IsCompleted,
        IReadOnlyList<Guid> QuestionIds,
        IReadOnlyList<Guid> AnsweredQuestionIds,
        IReadOnlyList<StudySessionInteractionDto> Interactions,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc,
        DateTimeOffset? CompletedAtUtc
    );
}
