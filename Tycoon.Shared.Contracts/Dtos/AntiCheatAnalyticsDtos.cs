namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record AntiCheatRuleCountDto(
        string RuleKey,
        int Severity,
        int Count
    );

    public sealed record AntiCheatSummaryDto(
        DateTimeOffset WindowStartUtc,
        DateTimeOffset WindowEndUtc,
        int TotalFlags,
        int SevereFlags,
        int WarningFlags,
        int InfoFlags,
        IReadOnlyList<AntiCheatRuleCountDto> ByRule
    );

    public sealed record PlayerRiskRowDto(
        Guid PlayerId,
        int SevereCount,
        int WarningCount,
        int CurrentStatus,
        DateTimeOffset LastFlagAtUtc
    );

    public sealed record PlayerRiskListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<PlayerRiskRowDto> Items
    );
}
