using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.AntiCheat;
// Economy types assumed from your Step 5
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Matches
{
    public sealed record SubmitMatch(SubmitMatchRequest Request) : IRequest<SubmitMatchResponse>;

    public sealed class SubmitMatchHandler(
        IAppDb db, 
        EconomyService econ,
        AntiCheatService antiCheat,
        SeasonService seasons,
        SeasonPointsService seasonsPoints,
        TierAssignmentService tiers)
        : IRequestHandler<SubmitMatch, SubmitMatchResponse>
    {
        public async Task<SubmitMatchResponse> Handle(SubmitMatch r, CancellationToken ct)
        {
            var req = r.Request;

            if (req.MatchId == Guid.Empty || req.EventId == Guid.Empty)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "Invalid", Array.Empty<MatchAwardDto>());

            if (req.Participants is null || req.Participants.Count == 0)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "Invalid", Array.Empty<MatchAwardDto>());

            // Submit idempotency
            var dup = await db.MatchResults.AsNoTracking()
                .AnyAsync(x => x.SubmitEventId == req.EventId, ct);

            if (dup)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "Duplicate", Array.Empty<MatchAwardDto>());

            var match = await db.Matches
                .Include(x => x.Rounds)
                .FirstOrDefaultAsync(x => x.Id == req.MatchId, ct);

            if (match is null)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "NotFound", Array.Empty<MatchAwardDto>());

            // Persist summary snapshot
            var result = new MatchResult(
                matchId: req.MatchId,
                submitEventId: req.EventId,
                mode: req.Mode,
                category: req.Category,
                questionCount: req.QuestionCount,
                endedAtUtc: req.EndedAtUtc,
                status: req.Status
            );

            db.MatchResults.Add(result);
            await db.SaveChangesAsync(ct); // ensure result.Id

            foreach (var p in req.Participants)
            {
                db.MatchParticipantResults.Add(new MatchParticipantResult(
                    matchResultId: result.Id,
                    playerId: p.PlayerId,
                    score: p.Score,
                    correct: p.Correct,
                    wrong: p.Wrong,
                    avgAnswerTimeMs: p.AvgAnswerTimeMs
                ));
            }

            var flags = antiCheat.Evaluate(req);
            if (flags.Count > 0)
            {
                foreach (var f in flags)
                    db.AntiCheatFlags.Add(f);

                await db.SaveChangesAsync(ct);
            }

            var blockRewards = AntiCheatService.ShouldBlockRewards(flags);

            // Reward policy (deterministic, tune later via remote config)
            var awards = await AwardAsync(req, match, econ, ct);

            if (!blockRewards)
            {
                await ApplySeasonPointsAndRanksAsync(req, match, ct); 
            }
            else
            {
                // Still finish host for downstream domain event pipeline, but do not award economy/season points
                // Optional: you can also force req.Status = Aborted here if you want.
            }

            // Finish match for host (preserves your existing domain-event based mission/tier wiring)
            FinishHost(match, req);

            await db.SaveChangesAsync(ct);

            return new SubmitMatchResponse(req.EventId, req.MatchId, "Applied", awards);
        }
        private async Task ApplySeasonPointsAndRanksAsync(SubmitMatchRequest req, Match match, CancellationToken ct)
        {
            // Only award season points for completed matches
            if (req.Status != MatchStatus.Completed)
                return;

            // Get active season
            var active = await seasons.GetActiveAsync(ct);
            if (active is null)
                return;

            var seasonId = active.SeasonId;

            // Determine outcome: highest score wins; ties => draw
            var ordered = req.Participants.OrderByDescending(p => p.Score).ToList();
            var top = ordered[0];
            var second = ordered.Count > 1 ? ordered[1] : null;

            var isDraw = second is not null && second.Score == top.Score;
            var winnerPlayerId = isDraw ? (Guid?)null : top.PlayerId;

            // Apply per-player
            foreach (var p in req.Participants)
            {
                var win = !isDraw && winnerPlayerId.HasValue && p.PlayerId == winnerPlayerId.Value;
                var draw = isDraw;

                // Default policy (tune later):
                // Win: +30, Draw: +15, Loss: +5, plus (correct / 2)
                var delta = (win ? 30 : draw ? 15 : 5) + (Math.Max(0, p.Correct) / 2);

                // Use deterministic per-player event id derived from submit event id
                // This keeps season points idempotent per match submission retry.
                var seasonEventId = DeterministicGuid(req.EventId, p.PlayerId);

                // Apply to season points ledger (idempotent)
                await seasonsPoints.ApplyAsync(new ApplySeasonPointsRequest(
                    seasonEventId,
                    seasonId,
                    p.PlayerId,
                    "match-result",
                    delta,
                    $"match:{req.MatchId}"
                ), ct);

                // Update W/L/D counters on profile (non-ledger state)
                // We do this in a safe way: get profile and update stats.
                var profile = await db.PlayerSeasonProfiles
                    .FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.PlayerId == p.PlayerId, ct);

                // Profile should exist because ApplyAsync creates it, but defensive anyway
                if (profile is null)
                {
                    profile = new PlayerSeasonProfile(seasonId, p.PlayerId, 0);
                    db.PlayerSeasonProfiles.Add(profile);
                }

                profile.ApplyMatchOutcome(win: win, draw: draw);
            }

            // Recompute tier ranks (100 users per tier)
            // For now we recompute every submit in dev; later optimize (queue/batch, debounce, periodic job).
            await tiers.RecomputeAsync(seasonId, usersPerTier: 100, ct: ct);
        }

        private static void FinishHost(Match match, SubmitMatchRequest req)
        {
            // Compute host stats from participant payload
            var host = req.Participants.FirstOrDefault(x => x.PlayerId == match.HostPlayerId);
            if (host is null)
            {
                // If host isn't present in participant list, still mark finished without payload.
                match.Finish();
                return;
            }

            var bestOtherScore = req.Participants.Where(x => x.PlayerId != match.HostPlayerId)
                                                 .Select(x => x.Score)
                                                 .DefaultIfEmpty(host.Score)
                                                 .Max();

            var isWin = host.Score > bestOtherScore;
            var scoreDelta = host.Score - bestOtherScore;

            var duration = (int)Math.Max(0, (req.EndedAtUtc - match.StartedAt).TotalSeconds);

            // XP here should match whatever you award the host in AwardAsync (hostXp)
            // We compute it again for event payload stability.
            var hostXp = Math.Max(0, host.Correct) * 10 + (isWin ? 50 : 0);

            match.Finish(
                isWin: isWin,
                scoreDelta: scoreDelta,
                xpEarned: hostXp,
                correctAnswers: Math.Max(0, host.Correct),
                totalQuestions: Math.Max(0, req.QuestionCount),
                durationSeconds: duration
            );
        }

        private static async Task<IReadOnlyList<MatchAwardDto>> AwardAsync(
            SubmitMatchRequest req,
            Match match,
            EconomyService econ,
            CancellationToken ct)
        {
            // Winner is highest score; ties -> draw
            var ordered = req.Participants.OrderByDescending(p => p.Score).ToList();
            var top = ordered[0];
            var second = ordered.Count > 1 ? ordered[1] : null;
            var isDraw = second is not null && second.Score == top.Score;

            var awards = new List<MatchAwardDto>(req.Participants.Count);

            foreach (var p in req.Participants)
            {
                var baseXp = Math.Max(0, p.Correct) * 10;

                var xp = baseXp;
                var coins = 0;

                if (req.Status != MatchStatus.Completed)
                {
                    xp = 0;
                    coins = 0;
                }
                else
                {
                    // Duel/Ranked get better payouts; otherwise small participation coins
                    var isCompetitive = req.Mode.Equals("duel", StringComparison.OrdinalIgnoreCase) ||
                                        req.Mode.Equals("ranked", StringComparison.OrdinalIgnoreCase);

                    if (isCompetitive)
                    {
                        if (isDraw)
                        {
                            xp += 25;
                            coins += 10;
                        }
                        else if (p.PlayerId == top.PlayerId)
                        {
                            xp += 50;
                            coins += 25;
                        }
                        else
                        {
                            coins += 10;
                        }
                    }
                    else
                    {
                        coins += 5;
                    }
                }

                // Per-player deterministic idempotency under one submit EventId
                var playerEventId = DeterministicGuid(req.EventId, p.PlayerId);

                await econ.ApplyAsync(new CreateEconomyTxnRequest(
                    EventId: playerEventId,
                    PlayerId: p.PlayerId,
                    Kind: "match-complete",
                    Lines: new[]
                    {
                        new EconomyLineDto(CurrencyType.Xp, xp),
                        new EconomyLineDto(CurrencyType.Coins, coins),
                    },
                    Note: $"{match.Mode}:{req.MatchId}"
                ), ct);

                awards.Add(new MatchAwardDto(p.PlayerId, xp, coins));
            }

            return awards;
        }

        private static Guid DeterministicGuid(Guid a, Guid b)
        {
            Span<byte> bytes = stackalloc byte[32];
            a.TryWriteBytes(bytes[..16]);
            b.TryWriteBytes(bytes[16..]);

            Span<byte> folded = stackalloc byte[16];
            for (var i = 0; i < 16; i++)
                folded[i] = (byte)(bytes[i] ^ bytes[i + 16]);

            return new Guid(folded);
        }
    }
}
