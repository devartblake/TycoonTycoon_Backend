using System.Text.Json;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.AntiCheat
{
    public sealed class AntiCheatService
    {
        private readonly Func<DateTimeOffset> _utcNow;

        public AntiCheatService(Func<DateTimeOffset>? utcNow = null)
        {
            _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        }

        public IReadOnlyList<AntiCheatFlag> Evaluate(SubmitMatchRequest req)
        {
            var flags = new List<AntiCheatFlag>();
            var now = _utcNow();

            // Defensive guards (you may already validate in pipeline; this keeps service safe)
            if (req is null)
                throw new ArgumentNullException(nameof(req));

            if (req.QuestionCount <= 0)
            {
                flags.Add(new AntiCheatFlag(
                    matchId: req.MatchId,
                    playerId: null,
                    ruleKey: "AC-000",
                    severity: AntiCheatSeverity.Severe,
                    action: AntiCheatAction.BlockRewards,
                    message: "Invalid question count in submit payload.",
                    evidenceJson: JsonSerializer.Serialize(new { req.QuestionCount }),
                    createdAtUtc: now
                ));

                // If question count is invalid, most other rules are noisy; return early.
                return flags;
            }

            var participants = req.Participants ?? new List<MatchParticipantResultDto>();

            // Rule AC-003: duplicate player entries in submit payload
            var dupPlayers = participants
                .GroupBy(x => x.PlayerId)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (dupPlayers.Count > 0)
            {
                flags.Add(new AntiCheatFlag(
                    matchId: req.MatchId,
                    playerId: null,
                    ruleKey: "AC-003",
                    severity: AntiCheatSeverity.Severe,
                    action: AntiCheatAction.BlockRewards,
                    message: "Duplicate player entries in participant list.",
                    evidenceJson: JsonSerializer.Serialize(new { dupPlayers }),
                    createdAtUtc: now
                ));
            }

            const int suspiciousAvgAnswerTimeThresholdMs = 150;

            foreach (var p in participants)
            {
                // Rule AC-004: negative stats
                if (p.Score < 0 || p.Correct < 0 || p.Wrong < 0 || p.AvgAnswerTimeMs < 0)
                {
                    flags.Add(new AntiCheatFlag(
                        req.MatchId,
                        p.PlayerId,
                        "AC-004",
                        AntiCheatSeverity.Severe,
                        AntiCheatAction.BlockRewards,
                        "Negative values in match stats.",
                        JsonSerializer.Serialize(new { p.Score, p.Correct, p.Wrong, p.AvgAnswerTimeMs }),
                        now
                    ));

                    // keep evaluating other rules; negatives can co-occur with other anomalies
                }

                // Rule AC-001: impossible counts
                var totalAnswered = p.Correct + p.Wrong;

                if (p.Correct > req.QuestionCount || p.Wrong > req.QuestionCount || totalAnswered > req.QuestionCount)
                {
                    flags.Add(new AntiCheatFlag(
                        matchId: req.MatchId,
                        playerId: p.PlayerId,
                        ruleKey: "AC-001",
                        severity: AntiCheatSeverity.Severe,
                        action: AntiCheatAction.BlockRewards,
                        message: "Correct/Wrong totals exceed question count.",
                        evidenceJson: JsonSerializer.Serialize(new
                        {
                            p.Correct,
                            p.Wrong,
                            totalAnswered,
                            req.QuestionCount
                        }),
                        createdAtUtc: now
                    ));
                }

                // Rule AC-002: suspicious answer time (too low)
                if (p.AvgAnswerTimeMs > 0 && p.AvgAnswerTimeMs < suspiciousAvgAnswerTimeThresholdMs)
                {
                    flags.Add(new AntiCheatFlag(
                        req.MatchId,
                        p.PlayerId,
                        "AC-002",
                        AntiCheatSeverity.Warning,
                        AntiCheatAction.Warn,
                        "Average answer time is suspiciously low.",
                        JsonSerializer.Serialize(new
                        {
                            p.AvgAnswerTimeMs,
                            thresholdMs = suspiciousAvgAnswerTimeThresholdMs
                        }),
                        now
                    ));
                }
            }

            return flags;
        }

        public static bool ShouldBlockRewards(IEnumerable<AntiCheatFlag> flags)
            => flags.Any(f => f.Action == AntiCheatAction.BlockRewards);
    }
}