using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PlayerMindProfileService : IPlayerMindProfileService
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

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
                // Sidecar unavailable — retain existing profile scores
            }
        }

        profile.LastCalculatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct);

        return MapToDto(profile);
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
