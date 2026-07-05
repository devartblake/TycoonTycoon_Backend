using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Personalization;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Personalization;

public sealed class PlayerMindProfileService : IPlayerMindProfileService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    // Local scoring rule constants
    private const decimal MaxActivityEvents = 20m;
    private const decimal ConfidenceMin = 0.30m;
    private const decimal ConfidenceMax = 0.90m;
    private const decimal ConfidenceRange = ConfidenceMax - ConfidenceMin;
    private const decimal BaseChurnRisk = 0.80m;
    private const decimal MinChurnRisk = 0.10m;
    private const decimal ChurnReductionPerEvent = 0.03m;
    private const int FrustrationHighThreshold = 10;
    private const decimal FrustrationHighScore = 0.70m;
    private const decimal FrustrationDivisor = 20m;

    private readonly IAppDb _db;
    private readonly IPersonalizationSidecarClient _sidecar;

    public PlayerMindProfileService(IAppDb db, IPersonalizationSidecarClient sidecar)
    {
        _db = db;
        _sidecar = sidecar;
    }

    public async Task<PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _db.PlayerMindProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);

        if (profile is null)
        {
            profile = new PlayerMindProfile { Id = Guid.NewGuid(), PlayerId = playerId };
            _db.PlayerMindProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        return MapToDto(profile);
    }

    public async Task<PlayerMindProfileDto> SetPersonalizationEnabledAsync(Guid playerId, bool enabled, CancellationToken ct = default)
    {
        var profile = await _db.PlayerMindProfiles
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);

        if (profile is null)
        {
            profile = new PlayerMindProfile { Id = Guid.NewGuid(), PlayerId = playerId };
            _db.PlayerMindProfiles.Add(profile);
        }

        profile.PersonalizationEnabled = enabled;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToDto(profile);
    }

    public async Task RecordEventAsync(Guid playerId, PlayerBehaviorEventDto dto, CancellationToken ct = default)
    {
        var evt = new PlayerBehaviorEvent
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EventType = dto.EventType,
            EventSource = dto.EventSource,
            Category = dto.Category,
            Difficulty = dto.Difficulty,
            Mode = dto.Mode,
            MetadataJson = dto.Metadata is not null
                ? JsonSerializer.Serialize(dto.Metadata, _json)
                : "{}",
            OccurredAt = dto.OccurredAt ?? DateTimeOffset.UtcNow,
            IngestedAt = DateTimeOffset.UtcNow
        };

        _db.PlayerBehaviorEvents.Add(evt);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _db.PlayerMindProfiles
            .FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);

        if (profile is null)
        {
            profile = new PlayerMindProfile { Id = Guid.NewGuid(), PlayerId = playerId };
            _db.PlayerMindProfiles.Add(profile);
        }

        // Load recent events (last 50) for sidecar scoring
        var recentEvents = await _db.PlayerBehaviorEvents
            .AsNoTracking()
            .Where(e => e.PlayerId == playerId)
            .OrderByDescending(e => e.OccurredAt)
            .Take(50)
            .Select(e => new PlayerBehaviorEventDto(
                e.EventType, e.EventSource, e.Category, e.Difficulty, e.Mode, null, e.OccurredAt))
            .ToListAsync(ct);

        // Apply local rules first as a baseline
        ApplyLocalRules(profile, recentEvents);

        if (profile.SidecarScoringEnabled)
        {
            try
            {
                var scores = await _sidecar.ScorePlayerAsync(new SidecarPlayerScoringRequest(
                    playerId.ToString(),
                    recentEvents,
                    new SidecarPlayerSnapshotDto(
                        profile.ConfidenceLevel,
                        profile.ChurnRiskScore,
                        profile.FrustrationRiskScore,
                        profile.NotificationFatigueScore,
                        profile.Archetype)), ct);

                profile.ChurnRiskScore = scores.ChurnRiskScore;
                profile.FrustrationRiskScore = scores.FrustrationRiskScore;
                profile.ConfidenceLevel = scores.ConfidenceLevel;
                profile.Archetype = scores.RecommendedArchetype;
                profile.CategoryStrengthsJson = JsonSerializer.Serialize(scores.CategoryStrengths, _json);
                profile.CategoryWeaknessesJson = JsonSerializer.Serialize(scores.CategoryWeaknesses, _json);
                profile.SidecarScoresJson = JsonSerializer.Serialize(scores.Signals, _json);
            }
            catch
            {
                // Sidecar unavailable — local rules already applied
            }
        }

        profile.LastCalculatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToDto(profile);
    }

    private static void ApplyLocalRules(PlayerMindProfile profile, IReadOnlyList<PlayerBehaviorEventDto> recentEvents)
    {
        if (recentEvents.Count == 0)
            return;

        // Confidence rises with activity (clamp to [ConfidenceMin, ConfidenceMax])
        var activityRatio = Math.Min(recentEvents.Count / MaxActivityEvents, 1m);
        profile.ConfidenceLevel = Math.Clamp(ConfidenceMin + activityRatio * ConfidenceRange, ConfidenceMin, ConfidenceMax);

        // Churn risk decreases with more recent activity
        profile.ChurnRiskScore = Math.Max(MinChurnRisk, BaseChurnRisk - recentEvents.Count * ChurnReductionPerEvent);

        // Frustration risk: many events in the last hour signals possible frustration
        var oneHourAgo = DateTimeOffset.UtcNow.AddHours(-1);
        var recentHourCount = recentEvents.Count(e => e.OccurredAt.HasValue && e.OccurredAt >= oneHourAgo);
        profile.FrustrationRiskScore = recentHourCount >= FrustrationHighThreshold
            ? FrustrationHighScore
            : recentHourCount / FrustrationDivisor;

        // Category strengths: proportion of events per category
        var categoryGroups = recentEvents
            .Where(e => e.Category is not null)
            .GroupBy(e => e.Category!)
            .ToDictionary(g => g.Key, g => Math.Round((decimal)g.Count() / recentEvents.Count, 4));

        if (categoryGroups.Count > 0)
            profile.CategoryStrengthsJson = JsonSerializer.Serialize(categoryGroups, _json);

        // Archetype heuristic based on dominant event source
        var topSource = recentEvents
            .GroupBy(e => e.EventSource)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        profile.Archetype = topSource switch
        {
            "ranked"   => "competitor",
            "practice" => "learner",
            "casual"   => "casual_player",
            _          => profile.Archetype
        };
    }

    private static PlayerMindProfileDto MapToDto(PlayerMindProfile p)
    {
        static Dictionary<string, decimal> ParseDecimalDict(string json)
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json, _json) ?? []; }
            catch { return []; }
        }

        static Dictionary<string, object> ParseObjectDict(string json)
        {
            try { return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _json) ?? []; }
            catch { return []; }
        }

        return new PlayerMindProfileDto(
            p.PlayerId, p.ConfidenceLevel, p.RiskTolerance,
            p.PreferredPace, p.LearningStyle, p.CompetitivePreference, p.SocialPreference,
            p.ChurnRiskScore, p.FrustrationRiskScore, p.RewardSensitivityScore,
            p.StoreAffinityScore, p.NotificationFatigueScore, p.Archetype,
            ParseDecimalDict(p.CategoryStrengthsJson),
            ParseDecimalDict(p.CategoryWeaknessesJson),
            ParseObjectDict(p.PreferenceJson),
            ParseObjectDict(p.GuardrailJson),
            p.PersonalizationEnabled,
            p.SidecarScoringEnabled,
            p.LastCalculatedAt);
    }
}
