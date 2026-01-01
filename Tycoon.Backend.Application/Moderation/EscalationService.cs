using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Moderation
{
    public sealed class EscalationService(IAppDb db, ModerationService moderation)
    {
        public async Task<RunEscalationResponse> RunAsync(
            RunEscalationRequest req,
            string? setByAdmin,
            CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;
            var windowEnd = now;
            var windowStart = now.AddHours(-Math.Max(1, req.WindowHours));

            // Candidate players = those with any flags in the window
            var candidatePlayerIds = await db.AntiCheatFlags.AsNoTracking()
                .Where(f => f.CreatedAtUtc >= windowStart && f.CreatedAtUtc <= windowEnd && f.PlayerId != null)
                .GroupBy(f => f.PlayerId!.Value)
                .OrderByDescending(g => g.Max(x => x.CreatedAtUtc))
                .Select(g => g.Key)
                .Take(Math.Clamp(req.MaxPlayers, 1, 5000))
                .ToListAsync(ct);

            var decisions = new List<EscalationDecisionDto>();

            foreach (var playerId in candidatePlayerIds)
            {
                // Current moderation profile
                var current = await db.PlayerModerationProfiles.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);

                var currentStatus = current?.Status ?? ModerationStatus.Normal;

                // If already banned, skip
                if (currentStatus == ModerationStatus.Banned)
                    continue;

                // Count flags in window
                var severeCount = await db.AntiCheatFlags.AsNoTracking()
                    .CountAsync(f => f.PlayerId == playerId && f.CreatedAtUtc >= windowStart && f.CreatedAtUtc <= windowEnd &&
                                     (int)f.Severity == (int)AntiCheatSeverity.Severe, ct);

                var warningCount = await db.AntiCheatFlags.AsNoTracking()
                    .CountAsync(f => f.PlayerId == playerId && f.CreatedAtUtc >= windowStart && f.CreatedAtUtc <= windowEnd &&
                                     (int)f.Severity == (int)AntiCheatSeverity.Warning, ct);

                // Also check longer windows for ban thresholds (30d severe)
                var severe30d = await db.AntiCheatFlags.AsNoTracking()
                    .CountAsync(f => f.PlayerId == playerId && f.CreatedAtUtc >= now.AddDays(-30) &&
                                     (int)f.Severity == (int)AntiCheatSeverity.Severe, ct);

                ModerationStatus proposed = currentStatus;
                string? reason = null;

                // Ban threshold
                if (severe30d >= 6)
                {
                    proposed = ModerationStatus.Banned;
                    reason = $"Auto-escalation: {severe30d} severe anti-cheat flags in 30d.";
                }
                else
                {
                    // Restricted threshold
                    var severe7d = await db.AntiCheatFlags.AsNoTracking()
                        .CountAsync(f => f.PlayerId == playerId && f.CreatedAtUtc >= now.AddDays(-7) &&
                                         (int)f.Severity == (int)AntiCheatSeverity.Severe, ct);

                    if (severeCount >= 2 || warningCount >= 6 || severe7d >= 3)
                    {
                        proposed = ModerationStatus.Restricted;
                        reason = $"Auto-escalation: severe24h={severeCount}, warning24h={warningCount}, severe7d={severe7d}.";
                    }
                    else if (warningCount >= 3)
                    {
                        proposed = ModerationStatus.Suspected;
                        reason = $"Auto-escalation: warning24h={warningCount}.";
                    }
                }

                // Never auto-downgrade
                if (proposed <= currentStatus)
                    continue;

                decisions.Add(new EscalationDecisionDto(
                    playerId,
                    (int)currentStatus,
                    (int)proposed,
                    severeCount,
                    warningCount,
                    windowStart,
                    windowEnd,
                    reason ?? "Auto-escalation"));

                if (!req.DryRun)
                {
                    await moderation.SetStatusAsync(
                        playerId,
                        proposed,
                        reason,
                        notes: "Automated escalation (Step 6E).",
                        setByAdmin: setByAdmin,
                        expiresAtUtc: proposed == ModerationStatus.Restricted ? now.AddDays(7) : null, // optional default
                        relatedFlagId: null,
                        ct);
                }
            }

            return new RunEscalationResponse(
                req.DryRun,
                EvaluatedPlayers: candidatePlayerIds.Count,
                ChangedPlayers: decisions.Count,
                Decisions: decisions);
        }
    }
}
