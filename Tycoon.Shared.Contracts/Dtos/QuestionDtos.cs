namespace Tycoon.Shared.Contracts.Dtos
{
    public enum QuestionDifficulty
    {
        Easy = 1,
        Medium = 2,
        Hard = 3,
        Expert = 4
    }

    public enum TagFilterMode
    {
        Any = 1, // OR
        All = 2  // AND
    }

    public sealed record QuestionOptionDto(
        string Id,
        string Text
    );

    public sealed record QuestionDto(
        Guid Id,
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        IReadOnlyList<string> Tags,
        string? MediaKey,
        string? MediaUrl,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc
    );

    // Lightweight list item for grid/list rendering
    public sealed record QuestionListItemDto(
        Guid Id,
        string TextPreview,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<string> Tags,
        bool HasMedia,
        DateTimeOffset UpdatedAtUtc
    );

    public sealed record FacetCountDto(string Key, int Count);

    public sealed record QuestionQueryEchoDto(
        string? Search,
        IReadOnlyList<string> Tags,
        TagFilterMode TagMode,
        string? Category,
        QuestionDifficulty? Difficulty,
        string Sort,
        int Page,
        int PageSize
    );

    public sealed record QuestionListResponseDto(
        IReadOnlyList<QuestionListItemDto> Items,
        int Total,
        int Page,
        int PageSize,
        QuestionQueryEchoDto Query,
        IReadOnlyList<FacetCountDto> TagFacets,
        IReadOnlyList<FacetCountDto> CategoryFacets,
        IReadOnlyList<FacetCountDto> DifficultyFacets
    );

    public sealed record CreateQuestionRequest(
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        IReadOnlyList<string> Tags,
        string? MediaKey
    );

    public sealed record UpdateQuestionRequest(
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        IReadOnlyList<string> Tags,
        string? MediaKey
    );

    public sealed record BulkDeleteQuestionsRequest(IReadOnlyList<Guid> Ids);

    public sealed record BulkDeleteResultDto(int Requested, int Deleted);

    // Import/Export
    public sealed record ImportQuestionsRequest(IReadOnlyList<CreateQuestionRequest> Questions);
    public sealed record ImportQuestionsResultDto(int Received, int Created, int Failed);

    // Media hooks (presign abstraction)
    public sealed record CreateUploadIntentRequest(string FileName, string ContentType, long SizeBytes);
    public sealed record UploadIntentDto(string AssetKey, string UploadUrl, DateTimeOffset ExpiresAtUtc);
}
