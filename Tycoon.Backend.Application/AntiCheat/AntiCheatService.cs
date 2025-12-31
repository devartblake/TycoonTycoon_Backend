using System.Text.Json;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.AntiCheat
{
    public sealed class AntiCheatService
    {
        public IReadOnlyList<AntiCheatFlag> Evaluate(SubmitMatchRequest req)
        {
            var flags = new List<AntiCheatFlag>();

            // Rule AC-001: impossible correct count
            foreach (var p in req.Participants)
            {
                if (p.Correct > req.QuestionCount || p.Wrong > req.QuestionCount)
                {
                    flags.Add(new AntiCheatFlag(
                        matchId: req.MatchId,
                        playerId: p.PlayerId,
                        ruleKey: "AC-001",
                        severity: AntiCheatSeverity.Severe,
                        action: AntiCheatAction.BlockRewards,
                        message: "Correct/Wrong exceeds question count.",
                        evidenceJson: JsonSerializer.Serialize(new { p.Correct, p.Wrong, req.QuestionCount })
                    ));
                }
            }

            // Rule AC-002: suspicious answer time (too low)
            foreach (var p in req.Participants)
            {
                if (p.AvgAnswerTimeMs > 0 && p.AvgAnswerTimeMs < 150) // tune threshold later
                {
                    flags.Add(new AntiCheatFlag(
                        req.MatchId,
                        p.PlayerId,
                        "AC-002",
                        AntiCheatSeverity.Warning,
                        AntiCheatAction.Warn,
                        "Average answer time is suspiciously low.",
                        JsonSerializer.Serialize(new { p.AvgAnswerTimeMs })
                    ));
                }
            }

            // Rule AC-003: duplicate player entries in submit payload
            var dupPlayers = req.Participants.GroupBy(x => x.PlayerId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dupPlayers.Count > 0)
            {
                flags.Add(new AntiCheatFlag(
                    req.MatchId,
                    null,
                    "AC-003",
                    AntiCheatSeverity.Severe,
                    AntiCheatAction.BlockRewards,
                    "Duplicate player entries in participant list.",
                    JsonSerializer.Serialize(new { dupPlayers })
                ));
            }

            // Rule AC-004: negative stats
            foreach (var p in req.Participants)
            {
                if (p.Score < 0 || p.Correct < 0 || p.Wrong < 0 || p.AvgAnswerTimeMs < 0)
                {
                    flags.Add(new AntiCheatFlag(
                        req.MatchId,
                        p.PlayerId,
                        "AC-004",
                        AntiCheatSeverity.Severe,
                        AntiCheatAction.BlockRewards,
                        "Negative values in match stats.",
                        JsonSerializer.Serialize(new { p.Score, p.Correct, p.Wrong, p.AvgAnswerTimeMs })
                    ));
                }
            }

            return flags;
        }

        public static bool ShouldBlockRewards(IEnumerable<AntiCheatFlag> flags)
            => flags.Any(f => f.Action == AntiCheatAction.BlockRewards);
    }
}
