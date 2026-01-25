namespace Tycoon.Backend.Application.Analytics.Models
{
    /// <summary>
    /// Aggregated daily rollup for question answers across all players.
    /// </summary>
    public sealed class QuestionAnsweredDailyRollup
    {
        public string Id { get; set; } = string.Empty;

        /// <summary>UTC Date (date-only) this rollup is aggregated for.</summary>
        public DateOnly Day { get; set; } // Aligned with Store filter logic

        public string Mode { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Difficulty { get; set; }

        public int TotalAnswers { get; set; }
        public int CorrectAnswers { get; set; }
        public int WrongAnswers { get; set; }

        /// <summary>
        /// Total answer time across all answers in milliseconds.
        /// </summary>
        public long SumAnswerTimeMs { get; set; }

        public int MinAnswerTimeMs { get; set; }
        public int MaxAnswerTimeMs { get; set; }

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

        // Helper methods for analytics display
        public double Accuracy => TotalAnswers <= 0 ? 0d : (double)CorrectAnswers / TotalAnswers;
        public int AvgAnswerTimeMs => TotalAnswers <= 0 ? 0 : (int)(SumAnswerTimeMs / TotalAnswers);
    }
}