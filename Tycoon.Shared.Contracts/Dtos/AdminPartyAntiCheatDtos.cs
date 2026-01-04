namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record PartyAntiCheatFlagDto(
        Guid Id,
        DateTimeOffset CreatedAtUtc,
        Guid MatchId,
        Guid? PlayerId,
        string RuleKey,
        string Severity,
        string Action,
        string Message,
        Guid? PartyId,
        string? EvidenceJson
    );

    public sealed record PartyAntiCheatFlagsResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<PartyAntiCheatFlagDto> Items
    );

    public sealed record PartyAntiCheatSummaryItemDto(
        Guid? PlayerId,
        Guid? PartyId,
        int Count,
        DateTimeOffset LastSeenUtc,
        IReadOnlyList<Guid> RecentMatchIds
    );

    public sealed record PartyAntiCheatSummaryResponseDto(
        DateTimeOffset SinceUtc,
        int TotalFlags,
        IReadOnlyList<PartyAntiCheatSummaryItemDto> Items
    );

    public sealed record ReviewFlagRequestDto(string? ReviewedBy);

}
