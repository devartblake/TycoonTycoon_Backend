namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record AntiCheatFlagDto(
        Guid Id,
        Guid MatchId,
        Guid? PlayerId,
        string RuleKey,
        int Severity,
        int Action,
        string Message,
        DateTimeOffset CreatedAtUtc,
        DateTimeOffset? ReviewedAtUtc,
        string? ReviewedBy,
        string? ReviewNote
    );

    public sealed record AntiCheatFlagListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<AntiCheatFlagDto> Items
    );
}
