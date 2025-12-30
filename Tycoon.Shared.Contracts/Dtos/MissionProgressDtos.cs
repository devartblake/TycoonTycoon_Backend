namespace Tycoon.Shared.Contracts.Dtos
{
    public sealed record MatchCompletedProgressDto(
        Guid EventId,
        Guid PlayerId,
        bool IsWin,
        int CorrectAnswers,
        int TotalQuestions,
        int DurationSeconds
    );

    public sealed record RoundCompletedProgressDto(
        Guid EventId,
        Guid PlayerId,
        bool PerfectRound,
        int AvgAnswerTimeMs
    );

    public sealed record ProgressAppliedDto(
        Guid EventId,
        Guid PlayerId,
        string Status,             // "Applied" | "Duplicate"
        DateTimeOffset AppliedAtUtc
    );
}
