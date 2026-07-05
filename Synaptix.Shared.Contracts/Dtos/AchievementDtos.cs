namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record AchievementDto(
        string Key,
        string Title,
        string Description,
        string Category,
        int Points,
        string? IconUrl,
        bool IsSecret
    );

    public sealed record AchievementCatalogDto(
        IReadOnlyList<AchievementDto> Achievements
    );

    public sealed record PlayerAchievementDto(
        string Key,
        DateTimeOffset UnlockedAtUtc
    );

    public sealed record PlayerAchievementsDto(
        Guid PlayerId,
        IReadOnlyList<PlayerAchievementDto> Unlocked
    );

    public sealed record UnlockAchievementRequest(
        Guid PlayerId,
        string AchievementKey
    );

    public sealed record UnlockAchievementResultDto(
        Guid PlayerId,
        string AchievementKey,
        string Status, // "Unlocked" | "Duplicate" | "NotFound"
        DateTimeOffset? UnlockedAtUtc
    );
}
