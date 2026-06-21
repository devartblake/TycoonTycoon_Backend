using Mediator;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.AntiCheat;
using Synaptix.Backend.Application.Enforcement;
using Synaptix.Backend.Application.Moderation;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Application.Guardians;
using Synaptix.Backend.Application.Social;
using Synaptix.Backend.Application.Territory;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Matches
{
    public sealed record SubmitMatch(SubmitMatchRequest Request) : IRequest<SubmitMatchResponse>;

    public sealed class SubmitMatchHandler(
        IAppDb db,
        IEconomyService econ,
        IPlayerTransactionService ptxnSvc,
        AntiCheatService antiCheat,
        SeasonService seasons,
        SeasonPointsService seasonsPoints,
        ModerationService moderation,
        EnforcementService enforcement,
        PartyIntegrityService partyIntegrity,
        PartyLifecycleService partLifecycle,
        IOptions<RankedSeasonOptions> rankedOptions,
        IMediator mediator,
        ILogger<SubmitMatchHandler> logger)
        : IRequestHandler<SubmitMatch, SubmitMatchResponse>
    {
        private readonly RankedSeasonOptions _ranked = rankedOptions.Value;
        private readonly ILogger<SubmitMatchHandler> _logger = logger;
        public async ValueTask<SubmitMatchResponse> Handle(SubmitMatch r, CancellationToken ct)
        {
            var req = r.Request;

            if (req.MatchId == Guid.Empty || req.EventId == Guid.Empty)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "Invalid", Array.Empty<MatchAwardDto>());

            if (req.Participants is null || req.Participants.Count == 0)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "Invalid", Array.Empty<MatchAwardDto>());

            if (req.Answers is { Count: > 0 })
            {
                var authoritativeParticipants = await BuildAuthoritativeParticipantsAsync(req.Participants, req.Answers, ct);
                var authoritativeQuestionCount = req.Answers.Select(a => a.QuestionId).Distinct().Count();

                req = req with
                {
                    Participants = authoritativeParticipants,
                    QuestionCount = authoritativeQuestionCount > 0 ? authoritativeQuestionCount : req.QuestionCount
                };
            }

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

            // If match is already finished, treat as already submitted.
            if (match.FinishedAt is not null)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "AlreadySubmitted", Array.Empty<MatchAwardDto>());

            // Block multiple results for the same match (double-submit protection).
            var alreadyHasResult = await db.MatchResults.AsNoTracking()
                .AnyAsync(x => x.MatchId == req.MatchId, ct);

            if (alreadyHasResult)
                return new SubmitMatchResponse(req.EventId, req.MatchId, "AlreadySubmitted", Array.Empty<MatchAwardDto>());

            // Moderation enforcement (Policy A)
            //var restricted = false;

            foreach (var p in req.Participants)
            {
                var status = await moderation.GetEffectiveStatusAsync(p.PlayerId, ct);

                if (status == ModerationStatus.Banned)
                {
                    // Do not mint economy or season points; do not leak ban details.
                    FinishHost(match, req);
                    await db.SaveChangesAsync(ct);
                    return new SubmitMatchResponse(req.EventId, req.MatchId, "Rejected", Array.Empty<MatchAwardDto>());
                }

            }

            var allowRewards = true;
            var allowSeasonPoints = true;

            foreach (var p in req.Participants)
            {
                var decision = await enforcement.EvaluateAsync(p.PlayerId, ct);

                if (!decision.CanSubmitMatch)
                {
                    FinishHost(match, req);
                    await db.SaveChangesAsync(ct);
                    return new SubmitMatchResponse(req.EventId, req.MatchId, "Rejected", Array.Empty<MatchAwardDto>());
                }

                if (!decision.AllowRewards) allowRewards = false;
                if (!decision.AllowSeasonPoints) allowSeasonPoints = false;
            }

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

            try
            {
                await db.SaveChangesAsync(ct); // ensure result.Id
            }
            catch (DbUpdateException)
            {
                // Another submit likely won the race and inserted a result first.
                return new SubmitMatchResponse(req.EventId, req.MatchId, "AlreadySubmitted", Array.Empty<MatchAwardDto>());
            }

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

            var flags = antiCheat.Evaluate(req).ToList();

            // Party integrity flags
            var partyFlags = await partyIntegrity.EvaluateMatchSubmissionAsync(
                matchId: req.MatchId,
                participantPlayerIds: req.Participants.Select(p => p.PlayerId).ToList(),
                ct: ct);

            if (partyFlags.Count > 0)
                flags.AddRange(partyFlags);

            // Persist any flags (conre anti-cheat + party integrity)
            if (flags.Count > 0)
            {
                foreach (var f in flags)
                    db.AntiCheatFlags.Add(f);

                await db.SaveChangesAsync(ct);
            }

            var antiCheatBlocksRewards = AntiCheatService.ShouldBlockRewards(flags);

            var blockRewards = antiCheatBlocksRewards || !allowRewards;
            // IMPORTANT: season blocking should use allowSeasonPoints (not allowRewards)
            var blockSeason = antiCheatBlocksRewards || !allowSeasonPoints;

            IReadOnlyList<MatchAwardDto> awards = Array.Empty<MatchAwardDto>();

            // Mint economy rewards only when allowed — now via PlayerTransaction
            if (!blockRewards)
            {
                awards = await AwardAsync(req, match, ptxnSvc, ct);

                // Apply territory XP multiplier bonus for territory_duel mode
                if (req.Mode.Equals("territory_duel", StringComparison.OrdinalIgnoreCase)
                    && req.Status == MatchStatus.Completed)
                {
                    try
                    {
                        var duel = await db.TerritoryDuels.AsNoTracking()
                            .FirstOrDefaultAsync(x => x.MatchId == req.MatchId, ct);

                        if (duel is not null)
                        {
                            foreach (var p in req.Participants)
                            {
                                var multiplierBps = await mediator.Send(
                                    new GetPlayerTileMultiplier(duel.SeasonId, duel.TierNumber, p.PlayerId), ct);

                                if (multiplierBps > 0)
                                {
                                    var bonusXp = (Math.Max(0, p.Correct) * 10 * multiplierBps) / 10000;
                                    if (bonusXp > 0)
                                    {
                                        var bonusEventId = DeterministicGuid(
                                            DeterministicGuid(req.EventId, p.PlayerId),
                                            new Guid("00000000-0000-0000-0000-000000000001"));

                                        await econ.ApplyAsync(new CreateEconomyTxnRequest(
                                            EventId: bonusEventId,
                                            PlayerId: p.PlayerId,
                                            Kind: "territory-xp-bonus",
                                            Lines: new[] { new EconomyLineDto(CurrencyType.Xp, bonusXp) },
                                            Note: $"territory-duel:{req.MatchId}"
                                        ), ct);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Territory XP multiplier bonus failed for match {MatchId}", req.MatchId);
                    }
                }
            }

            // Apply season points only when allowed
            if (!blockSeason)
            {
                await ApplySeasonPointsAndRanksAsync(req, match, ct);
            }

            // Finish match for host (preserves your existing domain-event based wiring)
            FinishHost(match, req);

            await db.SaveChangesAsync(ct);

            // 6I.6: Auto-close parties linked to this match when final
            if (req.Status == MatchStatus.Completed || req.Status == MatchStatus.Aborted)
            {
                try
                {
                    await partLifecycle.ClosePartiesForMatchAsync(
                        matchId: req.MatchId,
                        reason: $"Match {req.Status}",
                        ct: ct);
                }
                catch (Exception ex)
                {
                    // Party closure failure must not fail match submission, but must be visible.
                    _logger.LogWarning(ex,
                        "Party closure failed for match {MatchId} — match result was still applied.",
                        req.MatchId);
                }
            }

            // Resolve guardian challenges and territory duels linked to this match
            if (req.Mode.Equals("guardian_duel", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await mediator.Send(new ResolveGuardianChallenge(req.MatchId), ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Guardian challenge resolution failed for match {MatchId}", req.MatchId);
                }
            }

            if (req.Mode.Equals("territory_duel", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    await mediator.Send(new ResolveTerritoryDuel(req.MatchId), ct);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Territory duel resolution failed for match {MatchId}", req.MatchId);
                }
            }

            var finalStatus = antiCheatBlocksRewards ? "Rejected" : "Applied";
            return new SubmitMatchResponse(req.EventId, req.MatchId, finalStatus, awards);
        }

        private async Task<IReadOnlyList<MatchParticipantResultDto>> BuildAuthoritativeParticipantsAsync(
            IReadOnlyList<MatchParticipantResultDto> requestedParticipants,
            IReadOnlyList<MatchAnswerSubmissionDto> answers,
            CancellationToken ct)
        {
            var questionIds = answers.Select(a => a.QuestionId).Distinct().ToArray();
            var answerKey = await db.Questions.AsNoTracking()
                .Where(q => questionIds.Contains(q.Id))
                .Select(q => new { q.Id, q.CorrectOptionId })
                .ToDictionaryAsync(x => x.Id, x => x.CorrectOptionId, ct);

            var byPlayer = answers
                .GroupBy(a => a.PlayerId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var recomputed = new List<MatchParticipantResultDto>(requestedParticipants.Count);

            foreach (var p in requestedParticipants)
            {
                if (!byPlayer.TryGetValue(p.PlayerId, out var playerAnswers) || playerAnswers.Count == 0)
                {
                    recomputed.Add(p);
                    continue;
                }

                var correct = 0;
                foreach (var answer in playerAnswers)
                {
                    if (answerKey.TryGetValue(answer.QuestionId, out var correctOptionId)
                        && string.Equals(correctOptionId, answer.SelectedOptionId, StringComparison.OrdinalIgnoreCase))
                    {
                        correct++;
                    }
                }

                var total = playerAnswers.Count;
                var wrong = Math.Max(0, total - correct);
                var avgAnswerTime = total == 0 ? 0 : playerAnswers.Average(x => x.AnswerTimeMs);
                var score = correct * 10;

                recomputed.Add(new MatchParticipantResultDto(
                    p.PlayerId,
                    score,
                    correct,
                    wrong,
                    avgAnswerTime));
            }

            return recomputed;
        }

        private async Task ApplySeasonPointsAndRanksAsync(SubmitMatchRequest req, Match match, CancellationToken ct)
        {
            // Only award season points for completed matches
            if (req.Status != MatchStatus.Completed)
                return;

            // Only ranked contributes to season ladder
            if (!req.Mode.Equals("ranked", StringComparison.OrdinalIgnoreCase))
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

                // Load or create profile ONCE — save immediately so SeasonPointsService.GetOrCreateProfileAsync
                // finds it and doesn't create a duplicate (unique index on SeasonId+PlayerId).
                var profile = await db.PlayerSeasonProfiles
                    .FirstOrDefaultAsync(x => x.SeasonId == seasonId && x.PlayerId == p.PlayerId, ct);

                if (profile is null)
                {
                    profile = new PlayerSeasonProfile(seasonId, p.PlayerId, 0);
                    db.PlayerSeasonProfiles.Add(profile);
                    await db.SaveChangesAsync(ct);
                }

                // Placement-aware delta
                var isPlacement = profile.PlacementMatchesCompleted < _ranked.PlacementMatchesRequired;

                int basePts = isPlacement
                    ? (win ? _ranked.PlacementWinPoints : draw ? _ranked.PlacementDrawPoints : _ranked.PlacementLossPoints)
                    : (win ? _ranked.RankedWinBase : draw ? _ranked.RankedDrawBase : _ranked.RankedLossBase);

                var perfPts = Math.Max(0, p.Correct) / _ranked.CorrectDivisor;
                var delta = basePts + perfPts;

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

                // Update profile counters ONCE
                profile.ApplyMatchOutcome(win: win, draw: draw);
                profile.RecordRankedMatchCompleted();
            }

            // Tier recomputation is intentionally NOT called inline here.
            // Use the LeaderboardRecalculationJob scheduled via Hangfire (e.g. every 5 minutes)
            // to avoid a full rank recalculation in the hot path on every match submission.
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

        private static async Task<IReadOnlyList<MatchAwardDto>> AwardAsync(SubmitMatchRequest req, Match match, IPlayerTransactionService ptxnSvc, CancellationToken ct)
        {
            // Winner is highest score; ties -> draw
            var ordered = req.Participants.OrderByDescending(p => p.Score).ToList();
            var top = ordered[0];
            var second = ordered.Count > 1 ? ordered[1] : null;
            var isDraw = second is not null && second.Score == top.Score;

            var awards = new List<MatchAwardDto>(req.Participants.Count);
            var currencyChanges = new List<PlayerTransactionCurrencyDto>();
            var actors = new List<PlayerTransactionActorDto>();

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

                actors.Add(new PlayerTransactionActorDto(p.PlayerId, "recipient"));
                currencyChanges.Add(new PlayerTransactionCurrencyDto(
                    p.PlayerId,
                    new[]
                    {
                        new EconomyLineDto(CurrencyType.Xp, xp),
                        new EconomyLineDto(CurrencyType.Coins, coins),
                    }
                ));

                awards.Add(new MatchAwardDto(p.PlayerId, xp, coins));
            }

            // Single PlayerTransaction wrapping all per-player economy ledger entries
            await ptxnSvc.ExecuteAsync(new CreatePlayerTransactionRequest(
                EventId: req.EventId,
                Kind: "match-complete",
                CorrelatedEventId: req.MatchId,
                Actors: actors,
                CurrencyChanges: currencyChanges,
                Note: $"{match.Mode}:{req.MatchId}"
            ), ct);

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
