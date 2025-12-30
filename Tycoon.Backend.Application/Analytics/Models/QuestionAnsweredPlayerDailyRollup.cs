namespace Tycoon.Backend.Application.Analytics.Models
{
    /// <summary>
    /// Per-player daily rollup keyed by (utcDate, playerId, mode, category, difficulty).
    /// Stored in Mongo; indexed into Elasticsearch.
    /// </summary>
    public sealed record QuestionAnsweredPlayerDailyRollup(
        string Id,                 // deterministic
        DateOnly UtcDate,
        Guid PlayerId,
        string Mode,
        string Category,
        int Difficulty,
        long TotalAnswers,
        long CorrectAnswers,
        long WrongAnswers,
        long SumAnswerTimeMs,
        long MinAnswerTimeMs,
        long MaxAnswerTimeMs,
        DateTime UpdatedAtUtc
    )
    {
        public double Accuracy => TotalAnswers == 0 ? 0 : (double)CorrectAnswers / TotalAnswers;
        public double AvgAnswerTimeMs => TotalAnswers == 0 ? 0 : (double)SumAnswerTimeMs / TotalAnswers;
    }
}
