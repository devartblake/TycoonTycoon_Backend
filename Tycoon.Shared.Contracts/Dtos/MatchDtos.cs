namespace Tycoon.Shared.Contracts.Dtos
{
    public enum MatchStatus
    {
        Completed = 1,
        Aborted = 2
    }

    public enum MatchOutcome
    {
        Win = 1,
        Loss = 2,
        Draw = 3
    }

    public record StartMatchRequest(
        Guid HostPlayerId,
        string Mode
     );

    public record StartMatchResponse(
        Guid MatchId,
        DateTimeOffset StartedAt
    );

    public sealed record MatchParticipantResultDto(
        Guid PlayerId,
        int Score,
        int Correct,
        int Wrong,
        double AvgAnswerTimeMs
    );

    public sealed record SubmitMatchRequest(
        Guid EventId,                  // idempotency key for match submission + payouts
        Guid MatchId,                  // stable match identifier (server or client minted)
        string Mode,                   // e.g. "duel", "ranked", "practice"
        string Category,               // e.g. "sports", "general"
        int QuestionCount,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset EndedAtUtc,
        MatchStatus Status,
        IReadOnlyList<MatchParticipantResultDto> Participants
    );

    public sealed record MatchAwardDto(Guid PlayerId, int AwardedXp, int AwardedCoins);

    public sealed record SubmitMatchResponse(
        Guid EventId,
        Guid MatchId,
        string Status,                 // "Applied" | "Duplicate"
        IReadOnlyList<MatchAwardDto> Awards
    );

    public sealed record MatchDetailDto(
        Guid MatchId,
        Guid HostPlayerId,
        string Mode,
        string Category,
        int QuestionCount,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset EndedAtUtc,
        MatchStatus Status,
        IReadOnlyList<MatchParticipantResultDto> Participants
    );

    public sealed record MatchListItemDto(
        Guid MatchId,
        string Mode,
        string Category,
        int QuestionCount,
        DateTimeOffset EndedAtUtc,
        MatchStatus Status
    );

    public sealed record MatchListResponseDto(
        int Page,
        int PageSize,
        int Total,
        IReadOnlyList<MatchListItemDto> Items
    );
}
