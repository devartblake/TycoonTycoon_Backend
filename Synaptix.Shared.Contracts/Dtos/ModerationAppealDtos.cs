namespace Synaptix.Shared.Contracts.Dtos
{
    public sealed record SubmitAppealRequest(string Reason);

    public sealed record ReviewAppealRequest(
        string Verdict,             // "approve" | "reject"
        string? ReviewerNotes
    );

    public sealed record ModerationAppealDto(
        Guid Id,
        Guid PlayerId,
        string Reason,
        int Status,                 // matches ModerationAppealStatus enum int values
        string? ReviewerNotes,
        string? ReviewedBy,
        DateTimeOffset SubmittedAtUtc,
        DateTimeOffset? ReviewedAtUtc
    );

    public sealed record ModerationAppealListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<ModerationAppealDto> Items
    );
}
