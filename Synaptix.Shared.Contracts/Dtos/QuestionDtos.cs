namespace Synaptix.Shared.Contracts.Dtos
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
        string Status,
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
        string? MediaKey,
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
        string? MediaKey,
        string? Status = null
    );

    public sealed record UpdateQuestionRequest(
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        IReadOnlyList<string> Tags,
        string? MediaKey,
        string? Status = null
    );

    public sealed record BulkDeleteQuestionsRequest(IReadOnlyList<Guid> Ids);

    public sealed record BulkDeleteResultDto(int Requested, int Deleted);

    // Import/Export
    public sealed record ImportQuestionsRequest(IReadOnlyList<CreateQuestionRequest> Questions);
    public sealed record ImportQuestionsResultDto(int Received, int Created, int Failed);

    // Media hooks (presign abstraction)
    public sealed record CreateUploadIntentRequest(string FileName, string ContentType, long SizeBytes);
    public sealed record UploadIntentDto(string AssetKey, string UploadUrl, DateTimeOffset ExpiresAtUtc);

    // JSON upload endpoint
    public sealed record UploadQuestionRequest(string QuestionTitle, string QuestionDetails);
    public sealed record UploadQuestionResponseDto(string Message, string QuestionTitle, string QuestionDetails);

    public sealed record QuestionDifficultyEstimateRequest(string Text);
    public sealed record QuestionDifficultyEstimateResponse(
        QuestionDifficulty Difficulty,
        decimal Confidence,
        string Source
    );

    // ── Gameplay question serving (client-facing, no correct answer exposed) ──

    /// <summary>A question as served to the client during gameplay — correctOptionId is withheld.</summary>
    public sealed record GameplayQuestionDto(
        Guid Id,
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string? MediaKey
    );

    /// <summary>A set of questions served for a match or practice session.</summary>
    public sealed record QuestionSetDto(
        IReadOnlyList<GameplayQuestionDto> Questions,
        int Count
    );

    /// <summary>Canonical category catalog for gameplay, learning, and future study filters.</summary>
    public sealed record QuestionCategoriesResponseDto(
        IReadOnlyList<FacetCountDto> Categories
    );

    /// <summary>High-level question-surface metadata for clients building filters and discovery UIs.</summary>
    public sealed record QuestionMetadataResponseDto(
        IReadOnlyList<FacetCountDto> Categories,
        IReadOnlyList<QuestionDifficulty> Difficulties,
        int DefaultCount,
        int MaxCount
    );

    /// <summary>Preview request for question discovery and future study-set builders.</summary>
    public sealed record PreviewQuestionSetRequest(
        IReadOnlyList<string>? Categories,
        IReadOnlyList<QuestionDifficulty>? Difficulties,
        int Count
    );

    /// <summary>Request to check an answer server-side.</summary>
    public sealed record CheckAnswerRequest(Guid QuestionId, string SelectedOptionId);

    /// <summary>Server response after checking an answer.</summary>
    public sealed record CheckAnswerResponse(
        Guid QuestionId,
        string SelectedOptionId,
        string CorrectOptionId,
        bool IsCorrect
    );

    /// <summary>Batch answer check for a full match round.</summary>
    public sealed record CheckAnswersBatchRequest(IReadOnlyList<CheckAnswerRequest> Answers);

    /// <summary>Batch answer check response.</summary>
    public sealed record CheckAnswersBatchResponse(
        IReadOnlyList<CheckAnswerResponse> Results,
        int Total,
        int Correct
    );
}
