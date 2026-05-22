namespace Synaptix.Shared.Contracts.Dtos
{
    public static class StudySetKinds
    {
        public const string Category = "Category";
        public const string WeakArea = "WeakArea";
        public const string Favorites = "Favorites";
        public const string Custom = "Custom";
        public const string DueReview = "DueReview";
    }

    public sealed record CreateStudySetRequest(
        string Title,
        string? Description,
        IReadOnlyList<Guid> QuestionIds
    );

    public sealed record UpdateStudySetRequest(
        string Title,
        string? Description,
        IReadOnlyList<Guid> QuestionIds
    );

    public sealed record StudySetListItemDto(
        string Id,
        string Title,
        string Description,
        string Kind,
        string Category,
        int QuestionCount
    );

    public sealed record StudySetQuestionDto(
        Guid Id,
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        string? MediaKey
    );

    public sealed record StudySetDetailDto(
        string Id,
        string Title,
        string Description,
        string Kind,
        string Category,
        int QuestionCount,
        IReadOnlyList<StudySetQuestionDto> Questions
    );

    public sealed record StudySetsResponseDto(
        IReadOnlyList<StudySetListItemDto> Items
    );

    public sealed record RecommendedStudySetsResponseDto(
        IReadOnlyList<StudySetListItemDto> Items
    );
}
