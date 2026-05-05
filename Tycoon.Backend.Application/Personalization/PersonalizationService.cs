using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationService : IPersonalizationService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly IAppDb _db;
    private readonly IPlayerMindProfileService _profiles;
    private readonly IPersonalizationGuardrailService _guardrails;
    private readonly IPersonalizationSidecarClient _sidecar;
    private readonly IPersonalizationAuditService _audit;
    private readonly PersonalizationOptions _options;

    public PersonalizationService(
        IAppDb db,
        IPlayerMindProfileService profiles,
        IPersonalizationGuardrailService guardrails,
        IPersonalizationSidecarClient sidecar,
        IPersonalizationAuditService audit,
        IOptions<PersonalizationOptions> options)
    {
        _db = db;
        _profiles = profiles;
        _guardrails = guardrails;
        _sidecar = sidecar;
        _audit = audit;
        _options = options.Value;
    }

    public async Task<PlayerHomePersonalizationDto> GetHomeAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);
        var recommendations = await BuildRecommendationsAsync(playerId, profile, ct);

        var coachBrief = BuildCoachBrief(profile);
        var recommendedMissions = BuildMissionRecommendations(profile);

        return new PlayerHomePersonalizationDto(
            playerId,
            RecommendedMode: RecommendMode(profile),
            RecommendedCategory: RecommendCategory(profile),
            RecommendedDifficulty: RecommendDifficulty(profile),
            Recommendations: recommendations,
            CoachBrief: coachBrief,
            Guardrails: new Dictionary<string, object>
            {
                ["personalizationEnabled"] = profile.PersonalizationEnabled,
                ["sidecarEnabled"] = profile.SidecarScoringEnabled
            },
            RecommendedMissions: recommendedMissions);
    }

    public async Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);
        return await BuildRecommendationsAsync(playerId, profile, ct);
    }

    public async Task<NotificationPersonalizationDto> GetNotificationRecommendationAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);

        // Track local fatigue suppression separately so clients can see the block reason
        var localFatigueSuppressed = profile.NotificationFatigueScore >= _options.NotificationFatigueThreshold;

        var appliedGuardrails = new Dictionary<string, object>
        {
            ["adaptiveNotificationsEnabled"] = _options.AdaptiveNotifications,
            ["personalizationEnabled"] = profile.PersonalizationEnabled,
            ["localFatigueSuppressed"] = localFatigueSuppressed
        };

        if (!_options.AdaptiveNotifications)
            return new NotificationPersonalizationDto(playerId, null, profile.NotificationFatigueScore, false, 24, appliedGuardrails);

        // Load recent notification events so the sidecar can refine the fatigue estimate
        var recentNotifEvents = await LoadRecentEventsAsync(playerId, 30, ct);

        // Get notification score from sidecar (falls back to local computation when sidecar unavailable)
        var notifScore = new SidecarNotificationScoreDto(
            profile.NotificationFatigueScore,
            !localFatigueSuppressed,
            RecommendedFrequencyHours(profile.NotificationFatigueScore));

        if (profile.PersonalizationEnabled && profile.SidecarScoringEnabled)
        {
            try
            {
                notifScore = await _sidecar.GetNotificationScoreAsync(
                    new SidecarNotificationScoreRequest(
                        playerId.ToString(),
                        new SidecarPlayerSnapshotDto(
                            profile.ConfidenceLevel,
                            profile.ChurnRiskScore,
                            profile.FrustrationRiskScore,
                            profile.NotificationFatigueScore,
                            profile.Archetype),
                        recentNotifEvents), ct);
            }
            catch
            {
                // Sidecar unavailable — use local fallback
            }
        }

        // Apply local fatigue guardrail and track each source separately
        var sidecarFatigueSuppressed = !notifScore.CanReceiveNotification;
        var canReceive = !localFatigueSuppressed && !sidecarFatigueSuppressed;

        appliedGuardrails["sidecarFatigueSuppressed"] = sidecarFatigueSuppressed;
        appliedGuardrails["recommendedFrequencyHours"] = notifScore.RecommendedFrequencyHours;

        if (!canReceive)
            return new NotificationPersonalizationDto(playerId, null, notifScore.NotificationFatigueScore, false, notifScore.RecommendedFrequencyHours, appliedGuardrails);

        // Find the best notification candidate
        PlayerRecommendationDto? recommendation = null;

        if (profile.PersonalizationEnabled && profile.SidecarScoringEnabled)
        {
            try
            {
                var candidates = await _sidecar.GetRecommendationCandidatesAsync(
                    new SidecarRecommendationRequest(
                        playerId.ToString(),
                        new SidecarPlayerSnapshotDto(
                            profile.ConfidenceLevel,
                            profile.ChurnRiskScore,
                            profile.FrustrationRiskScore,
                            profile.NotificationFatigueScore,
                            profile.Archetype),
                        recentNotifEvents), ct);

                var notifCandidate = candidates.FirstOrDefault(c => c.Type == "notification");

                if (notifCandidate is not null)
                {
                    var guardCandidate = new PersonalizationCandidateDto(notifCandidate.Type, notifCandidate.Score, notifCandidate.Payload);
                    var guardrailResult = _guardrails.Apply(profile, guardCandidate);

                    var recId = Guid.NewGuid();
                    var reason = guardrailResult.BlockReason ?? notifCandidate.Reason;

                    await _audit.LogDecisionAsync(
                        playerId,
                        recId,
                        guardrailResult.Allowed ? "allowed" : "blocked",
                        "notification",
                        reason,
                        profile,
                        notifCandidate,
                        guardrailResult.AppliedRules,
                        new { guardrailResult.Allowed },
                        ct);

                    if (guardrailResult.Allowed)
                    {
                        var rec = new PersonalizationRecommendation
                        {
                            Id = recId,
                            PlayerId = playerId,
                            RecommendationType = notifCandidate.Type,
                            Source = "notification",
                            Priority = 0,
                            Score = notifCandidate.Score,
                            Reason = notifCandidate.Reason,
                            PayloadJson = JsonSerializer.Serialize(notifCandidate.Payload, _json),
                            GuardrailJson = JsonSerializer.Serialize(guardrailResult.AppliedRules, _json),
                            ExpiresAt = DateTimeOffset.UtcNow.AddHours(notifScore.RecommendedFrequencyHours)
                        };

                        _db.PersonalizationRecommendations.Add(rec);
                        await _db.SaveChangesAsync(ct);

                        recommendation = new PlayerRecommendationDto(
                            rec.Id, notifCandidate.Type, "notification", rec.Priority, notifCandidate.Score,
                            notifCandidate.Reason, notifCandidate.Payload, guardrailResult.AppliedRules, rec.ExpiresAt);
                    }
                }
            }
            catch
            {
                // Sidecar unavailable — use local fallback
            }
        }

        // Local fallback: build a notification recommendation from the player profile
        recommendation ??= BuildLocalNotificationRecommendation(profile);

        return new NotificationPersonalizationDto(playerId, recommendation, notifScore.NotificationFatigueScore, true, notifScore.RecommendedFrequencyHours, appliedGuardrails);
    }

    private static int RecommendedFrequencyHours(decimal fatigueScore) => fatigueScore switch
    {
        >= 0.75m => 48,
        >= 0.50m => 24,
        >= 0.25m => 12,
        _ => 6
    };

    private async Task<IReadOnlyList<PlayerBehaviorEventDto>> LoadRecentEventsAsync(Guid playerId, int count, CancellationToken ct) =>
        await _db.PlayerBehaviorEvents
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(count)
            .Select(e => new PlayerBehaviorEventDto(
                e.EventType, e.EventSource, e.Category, e.Difficulty, e.Mode, null, e.OccurredAt))
            .ToListAsync(ct);

    private static PlayerRecommendationDto BuildLocalNotificationRecommendation(PlayerMindProfileDto profile)
    {
        string tone, intent, reason;

        if (profile.FrustrationRiskScore >= 0.65m)
        {
            tone   = "supportive";
            intent = "support";
            reason = "You've had some tough sessions — take it easy and try a low-pressure round.";
        }
        else if (profile.ChurnRiskScore >= 0.60m)
        {
            tone   = "encouraging";
            intent = "re_engage";
            reason = "We missed you! Jump back in — your favourite categories are waiting.";
        }
        else
        {
            tone   = "motivating";
            intent = "daily_check_in";
            reason = "Stay sharp — a quick round today keeps your streak going.";
        }

        return new PlayerRecommendationDto(
            Guid.NewGuid(),
            "notification",
            "local",
            0,
            0.65m,
            reason,
            new Dictionary<string, object> { ["tone"] = tone, ["intent"] = intent },
            new Dictionary<string, object> { ["allowed"] = true },
            DateTimeOffset.UtcNow.AddHours(RecommendedFrequencyHours(profile.NotificationFatigueScore)));
    }

    public async Task<StorePersonalizationDto> GetStoreRecommendationsAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);

        var appliedGuardrails = new Dictionary<string, object>
        {
            ["adaptiveStoreEnabled"] = _options.AdaptiveStore,
            ["personalizationEnabled"] = profile.PersonalizationEnabled,
            ["frustrationPaidOfferSuppressed"] = IsFrustrated(profile)
        };

        if (!_options.AdaptiveStore)
            return new StorePersonalizationDto(playerId, [], appliedGuardrails);

        var candidates = await BuildStoreCandidatesAsync(playerId, profile, ct);

        var offers = new List<PlayerRecommendationDto>();
        var priority = 0;

        foreach (var candidate in candidates)
        {
            var guardCandidate = new PersonalizationCandidateDto(candidate.Type, candidate.Score, candidate.Payload);
            var guardrailResult = _guardrails.Apply(profile, guardCandidate);

            var recId = Guid.NewGuid();
            var reason = guardrailResult.BlockReason ?? candidate.Reason;

            await _audit.LogDecisionAsync(
                playerId,
                recId,
                guardrailResult.Allowed ? "allowed" : "blocked",
                "store",
                reason,
                profile,
                candidate,
                guardrailResult.AppliedRules,
                new { guardrailResult.Allowed },
                ct);

            if (guardrailResult.Allowed)
            {
                var rec = new PersonalizationRecommendation
                {
                    Id = recId,
                    PlayerId = playerId,
                    RecommendationType = candidate.Type,
                    Source = "store",
                    Priority = priority++,
                    Score = candidate.Score,
                    Reason = candidate.Reason,
                    PayloadJson = JsonSerializer.Serialize(candidate.Payload, _json),
                    GuardrailJson = JsonSerializer.Serialize(guardrailResult.AppliedRules, _json),
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(6)
                };

                _db.PersonalizationRecommendations.Add(rec);

                offers.Add(new PlayerRecommendationDto(
                    rec.Id, candidate.Type, "store", rec.Priority, candidate.Score,
                    candidate.Reason, candidate.Payload, guardrailResult.AppliedRules, rec.ExpiresAt));
            }
        }

        if (offers.Count > 0)
            await _db.SaveChangesAsync(ct);

        return new StorePersonalizationDto(playerId, offers, appliedGuardrails);
    }

    private async Task<List<SidecarRecommendationCandidateDto>> BuildStoreCandidatesAsync(
        Guid playerId, PlayerMindProfileDto profile, CancellationToken ct)
    {
        var candidates = new List<SidecarRecommendationCandidateDto>();

        if (profile.PersonalizationEnabled && profile.SidecarScoringEnabled)
        {
            try
            {
                var sidecarCandidates = await _sidecar.GetRecommendationCandidatesAsync(
                    new SidecarRecommendationRequest(
                        playerId.ToString(),
                        new SidecarPlayerSnapshotDto(
                            profile.ConfidenceLevel,
                            profile.ChurnRiskScore,
                            profile.FrustrationRiskScore,
                            profile.NotificationFatigueScore,
                            profile.Archetype),
                        []), ct);

                candidates.AddRange(sidecarCandidates.Where(c =>
                    c.Type is "store_offer" or "store_free_offer"));
            }
            catch
            {
                // Sidecar unavailable — fall back to local rules
            }
        }

        // Local fallback: ensure struggling players always receive a free support offer
        if (IsFrustrated(profile) && !candidates.Any(c => c.Type == "store_free_offer"))
        {
            candidates.Add(new SidecarRecommendationCandidateDto(
                Type: "store_free_offer",
                TargetId: null,
                Score: 0.80m,
                Reason: "You seem to be having a tough time — here's a free support item to help you get back on track.",
                Payload: new Dictionary<string, object> { ["tone"] = "supportive", ["isFree"] = true }));
        }

        return candidates;
    }

    private bool IsFrustrated(PlayerMindProfileDto profile) =>
        profile.FrustrationRiskScore >= _options.FrustrationPaidOfferSuppressionThreshold;

    public async Task AcceptRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default)
    {
        var rec = await _db.PersonalizationRecommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId && r.PlayerId == playerId, ct);

        if (rec is not null)
        {
            rec.AcceptedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    public async Task DismissRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default)
    {
        var rec = await _db.PersonalizationRecommendations
            .FirstOrDefaultAsync(r => r.Id == recommendationId && r.PlayerId == playerId, ct);

        if (rec is not null)
        {
            rec.DismissedAt = DateTimeOffset.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<IReadOnlyList<PlayerRecommendationDto>> BuildRecommendationsAsync(
        Guid playerId, PlayerMindProfileDto profile, CancellationToken ct)
    {
        List<SidecarRecommendationCandidateDto> candidates = [];

        if (profile.PersonalizationEnabled && profile.SidecarScoringEnabled)
        {
            try
            {
                candidates = (await _sidecar.GetRecommendationCandidatesAsync(
                    new SidecarRecommendationRequest(
                        playerId.ToString(),
                        new SidecarPlayerSnapshotDto(
                            profile.ConfidenceLevel,
                            profile.ChurnRiskScore,
                            profile.FrustrationRiskScore,
                            profile.NotificationFatigueScore,
                            profile.Archetype),
                        []), ct)).ToList();
            }
            catch
            {
                // Sidecar unavailable — serve empty candidates; local rules still apply
            }
        }

        var result = new List<PlayerRecommendationDto>();
        var priority = 0;

        foreach (var candidate in candidates)
        {
            var guardCandidate = new PersonalizationCandidateDto(candidate.Type, candidate.Score, candidate.Payload);
            var guardrailResult = _guardrails.Apply(profile, guardCandidate);

            var recId = Guid.NewGuid();
            var reason = guardrailResult.BlockReason ?? candidate.Reason;

            await _audit.LogDecisionAsync(
                playerId,
                recId,
                guardrailResult.Allowed ? "allowed" : "blocked",
                "sidecar",
                reason,
                profile,
                candidate,
                guardrailResult.AppliedRules,
                new { guardrailResult.Allowed },
                ct);

            if (guardrailResult.Allowed)
            {
                var rec = new PersonalizationRecommendation
                {
                    Id = recId,
                    PlayerId = playerId,
                    RecommendationType = candidate.Type,
                    Source = "sidecar",
                    Priority = priority++,
                    Score = candidate.Score,
                    Reason = candidate.Reason,
                    PayloadJson = JsonSerializer.Serialize(candidate.Payload, _json),
                    GuardrailJson = JsonSerializer.Serialize(guardrailResult.AppliedRules, _json),
                    ExpiresAt = DateTimeOffset.UtcNow.AddHours(6)
                };

                _db.PersonalizationRecommendations.Add(rec);

                result.Add(new PlayerRecommendationDto(
                    rec.Id, candidate.Type, "sidecar", rec.Priority, candidate.Score,
                    candidate.Reason, candidate.Payload, guardrailResult.AppliedRules, rec.ExpiresAt));
            }
        }

        if (result.Count > 0)
            await _db.SaveChangesAsync(ct);

        return result;
    }

    private static string RecommendMode(PlayerMindProfileDto profile) => profile.Archetype switch
    {
        "risk_taker" or "social_challenger" => "ranked",
        "low_pressure_learner" or "confidence_builder" => "practice",
        "mastery_path" => "study",
        _ => "casual"
    };

    private static string? RecommendCategory(PlayerMindProfileDto profile) =>
        profile.CategoryWeaknesses.OrderByDescending(kv => kv.Value).FirstOrDefault().Key;

    private static string? RecommendDifficulty(PlayerMindProfileDto profile) =>
        profile.ConfidenceLevel switch
        {
            < 0.30m => "easy",
            < 0.60m => "medium",
            < 0.85m => "hard",
            _ => "expert"
        };

    private static CoachBriefDto BuildCoachBrief(PlayerMindProfileDto profile)
    {
        if (profile.FrustrationRiskScore >= 0.70m)
            return new CoachBriefDto(
                "Take a breather",
                "Your recent sessions have been tough. Try a low-pressure practice round to rebuild confidence.",
                "start_practice",
                "/play/practice",
                "supportive");

        if (profile.ChurnRiskScore >= 0.65m)
            return new CoachBriefDto(
                "We missed you!",
                "Jump back in — your favourite categories are waiting. A quick round keeps your streak alive.",
                "start_casual",
                "/play/casual",
                "encouraging");

        if (profile.CategoryWeaknesses.Count > 0)
        {
            var weakest = profile.CategoryWeaknesses.OrderByDescending(kv => kv.Value).First().Key;
            return new CoachBriefDto(
                $"Level up your {weakest} knowledge",
                $"You've been missing some {weakest} questions. A focused study set can help.",
                "start_study",
                "/study",
                "motivating");
        }

        return new CoachBriefDto(
            "Ready for a challenge?",
            "Your skills are looking strong. Consider a ranked match to test yourself.",
            "start_ranked",
            "/play/ranked",
            "motivating");
    }

    // Maps player archetype → ordered list of (missionArchetype, reason, isLowPressure)
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<(string Archetype, string Reason, bool IsLowPressure)>>
        _archetypeMissionMap = new Dictionary<string, IReadOnlyList<(string, string, bool)>>
    {
        ["confidence_builder"]  = [("confidence_builder", "Low-pressure missions help you rebuild confidence step by step.", true),
                                   ("comeback_player",    "A short comeback mission is a great way to ease back in.", true)],
        ["streak_seeker"]       = [("streak_seeker",      "Keep the momentum — daily streak missions are your strength.", false),
                                   ("mastery_path",       "Master a skill category to push your streak even further.", false)],
        ["explorer"]            = [("explorer",           "Explore new categories and broaden your knowledge.", false),
                                   ("collector",          "Collect achievements across different topics.", false)],
        ["comeback_player"]     = [("comeback_player",    "A quick comeback mission gets you back in the game fast.", true),
                                   ("confidence_builder", "Rebuild confidence before aiming higher.", true)],
        ["collector"]           = [("collector",          "Collect badges and milestones across every topic.", false),
                                   ("explorer",           "Explore new categories to add to your collection.", false)],
        ["risk_taker"]          = [("risk_taker",         "High-stakes challenge missions are built for you.", false),
                                   ("social_challenger",  "Take on social challenges to prove your skills.", false)],
        ["social_challenger"]   = [("social_challenger",  "Challenge friends and climb the leaderboard.", false),
                                   ("risk_taker",         "Test your limits with high-risk challenge missions.", false)],
        ["mastery_path"]        = [("mastery_path",       "Deep-dive mastery missions will push your expertise to the max.", false),
                                   ("streak_seeker",      "A streak mission keeps your mastery progress consistent.", false)],
        ["new_player"]          = [("confidence_builder", "Start with confidence-building missions designed for new players.", true)],
        ["low_pressure_learner"]= [("confidence_builder", "Low-pressure missions let you learn at your own pace.", true)],
    };

    private static IReadOnlyList<MissionRecommendationDto> BuildMissionRecommendations(PlayerMindProfileDto profile)
    {
        // High-frustration players receive only low-pressure missions
        if (profile.FrustrationRiskScore >= 0.65m)
        {
            return [new MissionRecommendationDto(
                "confidence_builder",
                "You seem frustrated — a low-pressure confidence-building mission will help you recover.",
                IsLowPressure: true)];
        }

        if (_archetypeMissionMap.TryGetValue(profile.Archetype, out var mapped))
            return mapped.Select(m => new MissionRecommendationDto(m.Archetype, m.Reason, m.IsLowPressure)).ToList();

        // Fallback for unknown archetypes
        return [new MissionRecommendationDto(
            "explorer",
            "Explore a variety of missions to discover what suits you best.",
            IsLowPressure: false)];
    }
}
