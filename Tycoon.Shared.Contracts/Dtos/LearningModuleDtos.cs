namespace Tycoon.Shared.Contracts.Dtos
{
    // ── Browse / list ────────────────────────────────────────────────────────────

    public sealed record LearningModuleListItemDto(
        Guid Id,
        string Title,
        string Description,
        string Category,
        QuestionDifficulty Difficulty,
        int LessonCount,
        int RewardXp,
        int RewardCoins,
        bool IsCompleted        // false when no playerId is supplied to the endpoint
    );

    // ── Module overview (single) ─────────────────────────────────────────────────

    public sealed record LearningModuleDetailDto(
        Guid Id,
        string Title,
        string Description,
        string Category,
        QuestionDifficulty Difficulty,
        int LessonCount,
        int RewardXp,
        int RewardCoins
    );

    // Recommended modules for a player or anonymous learner.
    public sealed record RecommendedLearningModulesResponseDto(
        IReadOnlyList<LearningModuleListItemDto> Items
    );

    // Progress summary across the published learning catalog for one player.
    public sealed record LearningModuleProgressDto(
        Guid PlayerId,
        int TotalPublishedModules,
        int CompletedModules,
        int RemainingModules,
        decimal CompletionRate,
        IReadOnlyList<Guid> CompletedModuleIds
    );

    // ── Lesson (learning screen) ─────────────────────────────────────────────────
    // CorrectOptionId is intentionally exposed — this is a learning context,
    // not a competitive gameplay context.

    public sealed record ModuleLessonDto(
        Guid LessonId,
        int Order,
        Guid QuestionId,
        string QuestionText,
        string QuestionCategory,
        IReadOnlyList<QuestionOptionDto> Options,   // reuses existing QuestionOptionDto
        string CorrectOptionId,
        string? Explanation
    );

    // ── Complete response ────────────────────────────────────────────────────────

    public sealed record CompleteModuleResultDto(
        Guid ModuleId,
        Guid PlayerId,
        string Status,          // "Completed" | "AlreadyCompleted" | "ModuleNotFound"
        int RewardXp,
        int RewardCoins,
        int BalanceXp,
        int BalanceCoins
    );

    // ── Admin requests ───────────────────────────────────────────────────────────

    public sealed record CreateLearningModuleRequest(
        string Title,
        string Description,
        string Category,
        QuestionDifficulty Difficulty,
        int RewardXp,
        int RewardCoins
    );

    public sealed record UpdateLearningModuleRequest(
        string Title,
        string Description,
        string Category,
        QuestionDifficulty Difficulty,
        int RewardXp,
        int RewardCoins
    );

    public sealed record AddModuleLessonRequest(
        Guid QuestionId,
        int Order,
        string? Explanation
    );

    // ── Admin list item ──────────────────────────────────────────────────────────

    public sealed record AdminLearningModuleListItemDto(
        Guid Id,
        string Title,
        string Category,
        QuestionDifficulty Difficulty,
        int LessonCount,
        int RewardXp,
        int RewardCoins,
        bool IsPublished,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset UpdatedAtUtc
    );
}
