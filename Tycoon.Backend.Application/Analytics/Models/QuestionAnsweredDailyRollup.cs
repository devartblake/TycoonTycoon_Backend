namespace Tycoon.Backend.Application.Analytics.Models
{
    /// <summary>
    /// Daily rollup keyed by (date, category, difficulty, mode).
    /// Stored in Mongo; also indexed into Elasticsearch.
    /// </summary>
    public sealed record QuestionAnsweredDailyRollup(
        string Id,                 // deterministic
        DateOnly UtcDate,
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
