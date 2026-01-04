using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Social
{
    /// <summary>
    /// Party-aware integrity checks for match submissions (6J).
    /// Produces AntiCheatFlags (Warn by default) that can be escalated later.
    /// </summary>
    public sealed class PartyIntegrityService(IAppDb db)
    {
        public async Task<IReadOnlyList<AntiCheatFlag>> EvaluateMatchSubmissionAsync(
            Guid matchId,
            IReadOnlyList<Guid> participantPlayerIds,
            CancellationToken ct)
        {
            if (matchId == Guid.Empty || participantPlayerIds.Count == 0)
                return Array.Empty<AntiCheatFlag>();

            // Find parties linked to this match
            var partyIds = await db.PartyMatchLinks.AsNoTracking()
                .Where(x => x.MatchId == matchId)
                .Select(x => x.PartyId)
                .Distinct()
                .ToListAsync(ct);

            if (partyIds.Count == 0)
                return Array.Empty<AntiCheatFlag>();

            var flags = new List<AntiCheatFlag>();

            // Prefer snapshot membership if available (strict correctness)
            var snapshot = await db.PartyMatchMembers.AsNoTracking()
                .Where(x => x.MatchId == matchId)
                .Select(x => new { x.PartyId, x.PlayerId })
                .ToListAsync(ct);

            var members = snapshot.Count > 0
                ? snapshot
                : await db.PartyMembers.AsNoTracking()
                    .Where(m => partyIds.Contains(m.PartyId))
                    .Select(m => new { m.PartyId, m.PlayerId })
                    .ToListAsync(ct);

            var participantSet = participantPlayerIds.Distinct().ToHashSet();

            foreach (var pid in partyIds)
            {
                var partyMemberIds = members.Where(m => m.PartyId == pid)
                    .Select(m => m.PlayerId)
                    .Distinct()
                    .ToList();

                if (partyMemberIds.Count == 0)
                    continue;

                // Members missing from submission payload
                var missing = partyMemberIds.Where(m => !participantSet.Contains(m)).ToList();

                if (missing.Count > 0)
                {
                    var evidence = JsonSerializer.Serialize(new
                    {
                        partyId = pid,
                        kind = "party-member-missing-from-submit",
                        missingPlayerIds = missing
                    });

                    flags.Add(new AntiCheatFlag(
                        matchId: matchId,
                        playerId: null, // party-level anomaly
                        ruleKey: "party-member-missing-from-submit",
                        severity: AntiCheatSeverity.Warning,
                        action: AntiCheatAction.Warn,
                        message: $"Match submit missing {missing.Count} party member(s) for party {pid}.",
                        evidenceJson: evidence,
                        createdAtUtc: DateTimeOffset.UtcNow
                    ));
                }

                // Participants that appear to be "extra" relative to this party (informational)
                // This is only meaningful if your matchmaking expects party members ⊆ participants for each linked party.
                // We do not flag "extra" here by default because opponents exist.
            }

            return flags;
        }
    }
}
