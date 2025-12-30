using System.Security.Cryptography;
using System.Text;

namespace Tycoon.Backend.Application.Analytics
{
    public static class AnalyticsIds
    {
        /// <summary>
        /// Deterministic ID for an individual question-answered event.
        /// Prefer QuestionId if you have it; otherwise this hash is stable.
        /// </summary>
        public static string QuestionAnsweredEventId(
            Guid matchId,
            Guid playerId,
            DateTime answeredAtUtc,
            string mode,
            string category,
            int difficulty,
            bool isCorrect,
            int answerTimeMs)
        {
            var s = $"{matchId:N}|{playerId:N}|{answeredAtUtc:O}|{mode}|{category}|{difficulty}|{(isCorrect ? 1 : 0)}|{answerTimeMs}";
            return Sha256Base32(s);
        }

        /// <summary>
        /// Deterministic ID for daily rollup doc: YYYYMMDD|mode|category|difficulty
        /// </summary>
        public static string DailyRollupId(DateOnly utcDate, string mode, string category, int difficulty)
            => $"{utcDate:yyyyMMdd}|{mode}|{category}|{difficulty}";

        public static string PlayerDailyRollupId(DateOnly utcDate, Guid playerId, string mode, string category, int difficulty)
            => $"{utcDate:yyyyMMdd}|{playerId:N}|{mode}|{category}|{difficulty}";

        private static string Sha256Base32(string input)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            // URL-safe base64 (short enough and simple)
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }
    }
}
