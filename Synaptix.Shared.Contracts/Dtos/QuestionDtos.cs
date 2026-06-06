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
        DateTimeOffset UpdatedAtUtc,
        QuestionTaxonomyDto? Taxonomy = null
    );

    // Lightweight list item for grid/list rendering
    public sealed record QuestionListItemDto(
        Guid Id,
        string TextPreview,
        string Category,
        QuestionDifficulty Difficulty,
        string? MediaKey,
        IReadOnlyList<string> Tags,
        QuestionTaxonomyDto? Taxonomy,
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
        string? Status = null,
        QuestionTaxonomyInputDto? Taxonomy = null,
        string? SourceQuestionId = null,
        string? QuestionType = null,
        string? MediaType = null
    );

    public sealed record UpdateQuestionRequest(
        string Text,
        string Category,
        QuestionDifficulty Difficulty,
        IReadOnlyList<QuestionOptionDto> Options,
        string CorrectOptionId,
        IReadOnlyList<string> Tags,
        string? MediaKey,
        string? Status = null,
        QuestionTaxonomyInputDto? Taxonomy = null,
        string? SourceQuestionId = null,
        string? QuestionType = null,
        string? MediaType = null
    );

    public sealed record BulkDeleteQuestionsRequest(IReadOnlyList<Guid> Ids);

    public sealed record BulkDeleteResultDto(int Requested, int Deleted);

    // Import/Export
    public sealed record ImportQuestionsRequest(IReadOnlyList<CreateQuestionRequest> Questions);
    public sealed record ImportQuestionsResultDto(int Received, int Created, int Failed);

    public sealed record TaxonomyImportQuestionsRequest(
        IReadOnlyList<TaxonomyQuestionImportItemDto> Questions,
        bool Strict = false,
        bool EnrichWithSidecar = false,
        bool AutoApplyHighConfidenceSuggestions = false,
        decimal MinimumAutoApplyConfidence = 0.85m
    );

    public sealed record TaxonomyQuestionImportItemDto(
        string? Id,
        string? Question,
        string? Text,
        string? Category,
        object? Difficulty,
        IReadOnlyList<TaxonomyQuestionOptionImportDto>? Answers,
        IReadOnlyList<TaxonomyQuestionOptionImportDto>? Options,
        string? CorrectAnswer,
        string? CorrectOptionId,
        IReadOnlyList<string>? Tags,
        string? MediaKey,
        string? ImageUrl,
        string? VideoUrl,
        string? AudioUrl,
        string? Type,
        QuestionTaxonomyInputDto? Taxonomy
    );

    public sealed record TaxonomyQuestionOptionImportDto(
        string? Id,
        string? OptionId,
        string? Text,
        bool IsCorrect = false
    );

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
        string? MediaKey,
        string? MediaUrl,
        QuestionTaxonomyDto? Taxonomy = null
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
        int MaxCount,
        IReadOnlyList<QuestionTaxonomyFacetDto>? TaxonomyCategories = null,
        IReadOnlyList<FacetCountDto>? Subjects = null,
        IReadOnlyList<FacetCountDto>? Topics = null,
        IReadOnlyList<FacetCountDto>? GradeBands = null,
        IReadOnlyList<FacetCountDto>? AgeGroups = null,
        IReadOnlyList<FacetCountDto>? Audiences = null,
        IReadOnlyList<FacetCountDto>? Datasets = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? Aliases = null
    );

    /// <summary>Preview request for question discovery and future study-set builders.</summary>
    public sealed record PreviewQuestionSetRequest(
        IReadOnlyList<string>? Categories,
        IReadOnlyList<QuestionDifficulty>? Difficulties,
        int Count,
        IReadOnlyList<string>? Subjects = null,
        IReadOnlyList<string>? Topics = null,
        IReadOnlyList<string>? GradeBands = null,
        IReadOnlyList<string>? AgeGroups = null,
        IReadOnlyList<string>? Audiences = null,
        IReadOnlyList<string>? Datasets = null,
        IReadOnlyList<string>? Tags = null
    );

    public sealed record MixedQuestionSetRequest(
        int Count = 10,
        IReadOnlyList<string>? Categories = null,
        IReadOnlyList<string>? Subjects = null,
        IReadOnlyList<string>? Topics = null,
        IReadOnlyList<string>? GradeBands = null,
        IReadOnlyList<string>? AgeGroups = null,
        IReadOnlyList<string>? Audiences = null,
        IReadOnlyList<string>? Datasets = null,
        IReadOnlyList<QuestionDifficulty>? Difficulties = null,
        IReadOnlyList<string>? Tags = null,
        bool BalanceCategories = true,
        bool BalanceDifficulties = true
    );

    public sealed record QuestionTaxonomyInputDto(
        string? CanonicalCategory = null,
        string? DisplayCategory = null,
        string? Subject = null,
        string? Topic = null,
        string? Subtopic = null,
        string? GradeBand = null,
        string? AgeGroup = null,
        string? Audience = null,
        string? SourceDataset = null,
        IReadOnlyList<string>? TaxonomyTags = null
    );

    public sealed record QuestionTaxonomySuggestionRequest(
        string Text,
        string? Category = null,
        QuestionDifficulty? Difficulty = null,
        IReadOnlyList<string>? Options = null,
        IReadOnlyList<string>? Tags = null,
        string? SourceDataset = null,
        string? SourceQuestionId = null,
        QuestionTaxonomyInputDto? CurrentTaxonomy = null
    );

    public sealed record QuestionTaxonomySuggestionResponse(
        string CanonicalCategory,
        string DisplayCategory,
        string? Subject,
        string? Topic,
        string? Subtopic,
        string? GradeBand,
        string? AgeGroup,
        string? Audience,
        string QuestionType,
        string MediaType,
        IReadOnlyList<string> TaxonomyTags,
        IReadOnlyDictionary<string, decimal> FieldConfidences,
        decimal OverallConfidence,
        string ModelVersion,
        IReadOnlyList<string> Warnings
    );

    public sealed record QuestionTaxonomyBatchSuggestionRequest(
        IReadOnlyList<QuestionTaxonomySuggestionRequest> Questions
    );

    public sealed record QuestionTaxonomyBatchSuggestionResponse(
        IReadOnlyList<QuestionTaxonomySuggestionResponse?> Suggestions,
        int Received,
        int Suggested,
        int Failed
    );

    public sealed record QuestionTaxonomyStoredSuggestionDto(
        Guid Id,
        Guid? QuestionId,
        string? SourceDataset,
        string? SourceQuestionId,
        string Status,
        QuestionTaxonomySuggestionResponse Suggestion,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? AppliedAtUtc,
        string? ReviewedBy,
        string? ReviewNote
    );

    public sealed record ApplyQuestionTaxonomySuggestionRequest(
        Guid SuggestionId,
        string? ReviewedBy = null,
        string? ReviewNote = null
    );

    public sealed record QuestionTaxonomyDto(
        string CanonicalCategory,
        string DisplayCategory,
        string? Subject,
        string? Topic,
        string? Subtopic,
        string? GradeBand,
        string? AgeGroup,
        string? Audience,
        string? SourceDataset,
        string? SourceQuestionId,
        string QuestionType,
        string MediaType,
        IReadOnlyList<string> TaxonomyTags
    );

    public sealed record QuestionTaxonomyFacetDto(
        string Key,
        string DisplayName,
        string? Description,
        int Count,
        IReadOnlyList<string> Aliases
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
