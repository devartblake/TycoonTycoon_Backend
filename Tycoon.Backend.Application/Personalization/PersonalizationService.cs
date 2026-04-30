using System.Text.Json;
using Microsoft.EntityFrameworkCore;
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

    public PersonalizationService(
        IAppDb db,
        IPlayerMindProfileService profiles,
        IPersonalizationGuardrailService guardrails,
        IPersonalizationSidecarClient sidecar,
        IPersonalizationAuditService audit)
    {
        _db = db;
        _profiles = profiles;
        _guardrails = guardrails;
        _sidecar = sidecar;
        _audit = audit;
    }

    public async Task<PlayerHomePersonalizationDto> GetHomeAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);
        var recommendations = await BuildRecommendationsAsync(playerId, profile, ct);

        var coachBrief = BuildCoachBrief(profile);

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
            });
    }

    public async Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);
        return await BuildRecommendationsAsync(playerId, profile, ct);
    }

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

            var rec = new PersonalizationRecommendation
            {
                Id = recId,
                PlayerId = playerId,
                RecommendationType = candidate.Type,
                Source = "sidecar",
                Priority = priority++,
                Score = candidate.Score,
                PayloadJson = JsonSerializer.Serialize(candidate.Payload, _json),
                GuardrailJson = JsonSerializer.Serialize(guardrailResult.AppliedRules, _json),
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(6)
            };

            _db.PersonalizationRecommendations.Add(rec);

            await _audit.LogDecisionAsync(
                playerId,
                recId,
                guardrailResult.Allowed ? "allowed" : "blocked",
                "sidecar",
                guardrailResult.BlockReason ?? candidate.Reason,
                profile,
                candidate,
                guardrailResult.AppliedRules,
                new { guardrailResult.Allowed },
                ct);

            if (guardrailResult.Allowed)
            {
                result.Add(new PlayerRecommendationDto(
                    rec.Id, candidate.Type, "sidecar", rec.Priority, candidate.Score,
                    candidate.Payload, guardrailResult.AppliedRules, rec.ExpiresAt));
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
}
