# Synaptix Unified Personalization — Backend Code Scaffolding

## 1. Purpose

This document provides code-ready scaffolding for the backend implementation of the Unified Personalization Layer.

It covers:

- .NET 10 backend domain models
- EF Core configurations
- DTO contracts
- Application service interfaces
- Application service skeletons
- Minimal API endpoint scaffolding
- Admin endpoint scaffolding
- FastAPI Sidecar personalization routes
- Program.cs registration
- Configuration examples
- Guardrail service structure
- Observability and audit trail hooks

---

## 2. Backend Authority Model

```text
.NET Backend = authoritative decision maker
FastAPI Sidecar = scoring / intelligence assistant
Flutter Frontend = renderer and interaction collector
```

The Sidecar must never directly:

- grant rewards
- mutate wallets
- assign ranked difficulty
- bypass anti-cheat
- unlock purchases
- force player progression

---

# Part A — .NET Backend Scaffolding

---

## 3. Suggested Folder Structure

```text
Tycoon.Backend.Domain/
  Personalization/
    PlayerMindProfile.cs
    PlayerBehaviorEvent.cs
    PersonalizationRecommendation.cs
    PersonalizationAuditLog.cs
    PersonalizationRule.cs

Tycoon.Backend.Application/
  Personalization/
    DTOs/
      PlayerMindProfileDto.cs
      PlayerBehaviorEventDto.cs
      PlayerRecommendationDto.cs
      PlayerHomePersonalizationDto.cs
      CoachBriefDto.cs
      PersonalizationCandidateDto.cs
      PersonalizationGuardrailResult.cs
      SidecarDtos.cs
    IPlayerMindProfileService.cs
    IPersonalizationService.cs
    IPersonalizationGuardrailService.cs
    IPersonalizationSidecarClient.cs
    IPersonalizationAuditService.cs
    PlayerMindProfileService.cs
    PersonalizationService.cs
    PersonalizationGuardrailService.cs
    PersonalizationSidecarClient.cs
    PersonalizationAuditService.cs
    PersonalizationOptions.cs

Tycoon.Backend.Infrastructure/
  Persistence/
    Configurations/
      PlayerMindProfileConfiguration.cs
      PlayerBehaviorEventConfiguration.cs
      PersonalizationRecommendationConfiguration.cs
      PersonalizationAuditLogConfiguration.cs
      PersonalizationRuleConfiguration.cs
    Migrations/
      AddPersonalizationTables.cs

Tycoon.Backend.Api/
  Features/
    Personalization/
      PersonalizationEndpoints.cs
    Coach/
      CoachEndpoints.cs
    AdminPersonalization/
      AdminPersonalizationEndpoints.cs
```

---

## 4. Domain Models

### 4.1 `PlayerMindProfile.cs`

```csharp
namespace Tycoon.Backend.Domain.Personalization;

/// <summary>
/// Gameplay personalization profile.
/// This is not a clinical or psychological profile.
/// It stores inferred gameplay adaptation signals only.
/// </summary>
public sealed class PlayerMindProfile
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public decimal ConfidenceLevel { get; set; } = 0.50m;
    public decimal RiskTolerance { get; set; } = 0.50m;

    public string PreferredPace { get; set; } = "balanced";
    public string LearningStyle { get; set; } = "mixed";
    public string CompetitivePreference { get; set; } = "balanced";
    public string SocialPreference { get; set; } = "solo";

    public decimal ChurnRiskScore { get; set; } = 0.00m;
    public decimal FrustrationRiskScore { get; set; } = 0.00m;
    public decimal RewardSensitivityScore { get; set; } = 0.50m;
    public decimal StoreAffinityScore { get; set; } = 0.50m;
    public decimal NotificationFatigueScore { get; set; } = 0.00m;

    public string Archetype { get; set; } = "new_player";

    public string CategoryStrengthsJson { get; set; } = "{}";
    public string CategoryWeaknessesJson { get; set; } = "{}";
    public string PreferenceJson { get; set; } = "{}";
    public string GuardrailJson { get; set; } = "{}";
    public string SidecarScoresJson { get; set; } = "{}";

    public bool PersonalizationEnabled { get; set; } = true;
    public bool SidecarScoringEnabled { get; set; } = true;

    public DateTimeOffset? LastCalculatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### 4.2 `PlayerBehaviorEvent.cs`

```csharp
namespace Tycoon.Backend.Domain.Personalization;

/// <summary>
/// Raw gameplay, store, learning, notification, and recommendation event used for profile recalculation.
/// </summary>
public sealed class PlayerBehaviorEvent
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public string EventType { get; set; } = "";
    public string EventSource { get; set; } = "";

    public string? Category { get; set; }
    public string? Difficulty { get; set; }
    public string? Mode { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset IngestedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### 4.3 `PersonalizationRecommendation.cs`

```csharp
namespace Tycoon.Backend.Domain.Personalization;

/// <summary>
/// Backend-approved recommendation shown to the player.
/// </summary>
public sealed class PersonalizationRecommendation
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public string RecommendationType { get; set; } = "";
    public string Source { get; set; } = "backend";

    public int Priority { get; set; }
    public decimal Score { get; set; } = 0.50m;

    public string Reason { get; set; } = "";
    public string PayloadJson { get; set; } = "{}";
    public string GuardrailJson { get; set; } = "{}";

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### 4.4 `PersonalizationAuditLog.cs`

```csharp
namespace Tycoon.Backend.Domain.Personalization;

/// <summary>
/// Audit trail for explainability, admin diagnostics, and ToM safety.
/// </summary>
public sealed class PersonalizationAuditLog
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public Guid? RecommendationId { get; set; }

    public string DecisionType { get; set; } = "";
    public string Source { get; set; } = "backend";
    public string Reason { get; set; } = "";

    public string InputSignalsJson { get; set; } = "{}";
    public string CandidateJson { get; set; } = "{}";
    public string GuardrailsAppliedJson { get; set; } = "{}";
    public string FinalDecisionJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

### 4.5 `PersonalizationRule.cs`

```csharp
namespace Tycoon.Backend.Domain.Personalization;

/// <summary>
/// Admin-tunable personalization safety/configuration rule.
/// </summary>
public sealed class PersonalizationRule
{
    public Guid Id { get; set; }
    public string RuleKey { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
    public string RuleJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

## 5. EF Core Configuration Example

### `PlayerMindProfileConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Tycoon.Backend.Domain.Personalization;

namespace Tycoon.Backend.Infrastructure.Persistence.Configurations;

public sealed class PlayerMindProfileConfiguration : IEntityTypeConfiguration<PlayerMindProfile>
{
    public void Configure(EntityTypeBuilder<PlayerMindProfile> b)
    {
        b.ToTable("player_mind_profiles");

        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("id");
        b.Property(x => x.PlayerId).HasColumnName("player_id");

        b.Property(x => x.ConfidenceLevel).HasColumnName("confidence_level").HasPrecision(5, 2);
        b.Property(x => x.RiskTolerance).HasColumnName("risk_tolerance").HasPrecision(5, 2);
        b.Property(x => x.ChurnRiskScore).HasColumnName("churn_risk_score").HasPrecision(5, 2);
        b.Property(x => x.FrustrationRiskScore).HasColumnName("frustration_risk_score").HasPrecision(5, 2);
        b.Property(x => x.RewardSensitivityScore).HasColumnName("reward_sensitivity_score").HasPrecision(5, 2);
        b.Property(x => x.StoreAffinityScore).HasColumnName("store_affinity_score").HasPrecision(5, 2);
        b.Property(x => x.NotificationFatigueScore).HasColumnName("notification_fatigue_score").HasPrecision(5, 2);

        b.Property(x => x.PreferredPace).HasColumnName("preferred_pace").HasMaxLength(64);
        b.Property(x => x.LearningStyle).HasColumnName("learning_style").HasMaxLength(64);
        b.Property(x => x.CompetitivePreference).HasColumnName("competitive_preference").HasMaxLength(64);
        b.Property(x => x.SocialPreference).HasColumnName("social_preference").HasMaxLength(64);
        b.Property(x => x.Archetype).HasColumnName("archetype").HasMaxLength(96);

        b.Property(x => x.CategoryStrengthsJson).HasColumnName("category_strengths_json").HasColumnType("jsonb");
        b.Property(x => x.CategoryWeaknessesJson).HasColumnName("category_weaknesses_json").HasColumnType("jsonb");
        b.Property(x => x.PreferenceJson).HasColumnName("preference_json").HasColumnType("jsonb");
        b.Property(x => x.GuardrailJson).HasColumnName("guardrail_json").HasColumnType("jsonb");
        b.Property(x => x.SidecarScoresJson).HasColumnName("sidecar_scores_json").HasColumnType("jsonb");

        b.Property(x => x.PersonalizationEnabled).HasColumnName("personalization_enabled");
        b.Property(x => x.SidecarScoringEnabled).HasColumnName("sidecar_scoring_enabled");

        b.Property(x => x.LastCalculatedAt).HasColumnName("last_calculated_at");
        b.Property(x => x.CreatedAt).HasColumnName("created_at");
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at");

        b.HasIndex(x => x.PlayerId).IsUnique();
        b.HasIndex(x => x.Archetype);
        b.HasIndex(x => x.ChurnRiskScore);
        b.HasIndex(x => x.FrustrationRiskScore);
    }
}
```

---

## 6. Add DbSet Properties

In your EF AppDb implementation:

```csharp
public DbSet<PlayerMindProfile> PlayerMindProfiles => Set<PlayerMindProfile>();
public DbSet<PlayerBehaviorEvent> PlayerBehaviorEvents => Set<PlayerBehaviorEvent>();
public DbSet<PersonalizationRecommendation> PersonalizationRecommendations => Set<PersonalizationRecommendation>();
public DbSet<PersonalizationAuditLog> PersonalizationAuditLogs => Set<PersonalizationAuditLog>();
public DbSet<PersonalizationRule> PersonalizationRules => Set<PersonalizationRule>();
```

If you use an `IAppDb` abstraction, add matching sets there too.

---

## 7. DTOs

### `PlayerBehaviorEventDto.cs`

```csharp
namespace Tycoon.Backend.Application.Personalization.DTOs;

public sealed record PlayerBehaviorEventDto(
    string EventType,
    string EventSource,
    string? Category,
    string? Difficulty,
    string? Mode,
    Dictionary<string, object>? Metadata,
    DateTimeOffset? OccurredAt
);
```

### `PlayerMindProfileDto.cs`

```csharp
namespace Tycoon.Backend.Application.Personalization.DTOs;

public sealed record PlayerMindProfileDto(
    Guid PlayerId,
    decimal ConfidenceLevel,
    decimal RiskTolerance,
    string PreferredPace,
    string LearningStyle,
    string CompetitivePreference,
    string SocialPreference,
    decimal ChurnRiskScore,
    decimal FrustrationRiskScore,
    decimal RewardSensitivityScore,
    decimal StoreAffinityScore,
    decimal NotificationFatigueScore,
    string Archetype,
    Dictionary<string, decimal> CategoryStrengths,
    Dictionary<string, decimal> CategoryWeaknesses,
    Dictionary<string, object> Preferences,
    Dictionary<string, object> Guardrails,
    DateTimeOffset? LastCalculatedAt
);
```

### `PlayerRecommendationDto.cs`

```csharp
namespace Tycoon.Backend.Application.Personalization.DTOs;

public sealed record PlayerRecommendationDto(
    Guid Id,
    string Type,
    int Priority,
    decimal Score,
    string Reason,
    Dictionary<string, object> Payload,
    Dictionary<string, object> Guardrails,
    DateTimeOffset? ExpiresAt
);
```

### `PlayerHomePersonalizationDto.cs`

```csharp
namespace Tycoon.Backend.Application.Personalization.DTOs;

public sealed record PlayerHomePersonalizationDto(
    Guid PlayerId,
    string RecommendedMode,
    string? RecommendedCategory,
    string? RecommendedDifficulty,
    IReadOnlyList<PlayerRecommendationDto> Recommendations,
    CoachBriefDto? CoachBrief,
    Dictionary<string, object> Guardrails
);

public sealed record CoachBriefDto(
    string Title,
    string Message,
    string RecommendedAction,
    string? TargetRoute,
    string Tone
);
```

---

## 8. Options / Feature Flags

### `PersonalizationOptions.cs`

```csharp
namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationOptions
{
    public bool Enabled { get; set; } = true;
    public bool UseSidecar { get; set; } = true;
    public bool AdaptiveQuestions { get; set; } = false;
    public bool AdaptiveMissions { get; set; } = true;
    public bool AdaptiveStore { get; set; } = true;
    public bool AdaptiveNotifications { get; set; } = true;
    public bool CoachEnabled { get; set; } = true;

    public decimal FrustrationPaidOfferSuppressionThreshold { get; set; } = 0.75m;
    public decimal NotificationFatigueThreshold { get; set; } = 0.70m;
}
```

### `appsettings.Development.json`

```json
{
  "Personalization": {
    "Enabled": true,
    "UseSidecar": true,
    "AdaptiveQuestions": false,
    "AdaptiveMissions": true,
    "AdaptiveStore": true,
    "AdaptiveNotifications": true,
    "CoachEnabled": true,
    "FrustrationPaidOfferSuppressionThreshold": 0.75,
    "NotificationFatigueThreshold": 0.70
  },
  "SidecarPersonalization": {
    "BaseUrl": "http://sidecar:8100",
    "TimeoutSeconds": 3,
    "Enabled": true
  }
}
```

---

## 9. Application Interfaces

```csharp
using Tycoon.Backend.Application.Personalization.DTOs;

namespace Tycoon.Backend.Application.Personalization;

public interface IPlayerMindProfileService
{
    Task<PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default);

    Task RecordEventAsync(Guid playerId, PlayerBehaviorEventDto behaviorEvent, CancellationToken ct = default);

    Task<PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default);
}

public interface IPersonalizationService
{
    Task<PlayerHomePersonalizationDto> GetHomeAsync(Guid playerId, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(Guid playerId, CancellationToken ct = default);

    Task AcceptRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);

    Task DismissRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);
}

public interface IPersonalizationGuardrailService
{
    PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate);
}

public interface IPersonalizationAuditService
{
    Task LogDecisionAsync(
        Guid playerId,
        Guid? recommendationId,
        string decisionType,
        string source,
        string reason,
        object inputSignals,
        object candidate,
        object guardrails,
        object finalDecision,
        CancellationToken ct = default);
}
```

---

## 10. Candidate and Guardrail DTOs

```csharp
namespace Tycoon.Backend.Application.Personalization.DTOs;

public sealed record PersonalizationCandidateDto(
    string Type,
    string? TargetId,
    decimal Score,
    string Reason,
    Dictionary<string, object> Payload
);

public sealed record PersonalizationGuardrailResult(
    bool Allowed,
    string? BlockReason,
    Dictionary<string, object> AppliedRules
);
```

---

## 11. Guardrail Service Skeleton

```csharp
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Personalization.DTOs;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationGuardrailService : IPersonalizationGuardrailService
{
    private readonly PersonalizationOptions _options;

    public PersonalizationGuardrailService(IOptions<PersonalizationOptions> options)
    {
        _options = options.Value;
    }

    public PersonalizationGuardrailResult Apply(
        PlayerMindProfileDto profile,
        PersonalizationCandidateDto candidate)
    {
        var applied = new Dictionary<string, object>();

        if (!_options.Enabled)
        {
            applied["personalization_disabled"] = true;
            return new(false, "Personalization is disabled.", applied);
        }

        if (candidate.Type == "store_offer" &&
            profile.FrustrationRiskScore >= _options.FrustrationPaidOfferSuppressionThreshold)
        {
            applied["suppress_paid_offer_when_frustrated"] = true;
            return new(false, "Paid offer suppressed due to high frustration risk.", applied);
        }

        if (candidate.Type == "notification" &&
            profile.NotificationFatigueScore >= _options.NotificationFatigueThreshold)
        {
            applied["notification_fatigue_limit"] = true;
            return new(false, "Notification suppressed due to fatigue risk.", applied);
        }

        if (candidate.Type == "ranked_difficulty_modifier")
        {
            applied["ranked_fairness_lock"] = true;
            return new(false, "Ranked difficulty cannot be modified by personalization.", applied);
        }

        applied["allowed"] = true;
        return new(true, null, applied);
    }
}
```

---

## 12. PlayerMindProfileService Skeleton

```csharp
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Personalization.DTOs;
using Tycoon.Backend.Domain.Personalization;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PlayerMindProfileService : IPlayerMindProfileService
{
    private readonly IAppDb _db;

    public PlayerMindProfileService(IAppDb db)
    {
        _db = db;
    }

    public async Task<PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _db.PlayerMindProfiles
            .FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);

        if (profile is null)
        {
            profile = new PlayerMindProfile
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _db.PlayerMindProfiles.Add(profile);
            await _db.SaveChangesAsync(ct);
        }

        return ToDto(profile);
    }

    public async Task RecordEventAsync(
        Guid playerId,
        PlayerBehaviorEventDto behaviorEvent,
        CancellationToken ct = default)
    {
        var entity = new PlayerBehaviorEvent
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            EventType = behaviorEvent.EventType,
            EventSource = behaviorEvent.EventSource,
            Category = behaviorEvent.Category,
            Difficulty = behaviorEvent.Difficulty,
            Mode = behaviorEvent.Mode,
            MetadataJson = JsonSerializer.Serialize(behaviorEvent.Metadata ?? new Dictionary<string, object>()),
            OccurredAt = behaviorEvent.OccurredAt ?? DateTimeOffset.UtcNow,
            IngestedAt = DateTimeOffset.UtcNow
        };

        _db.PlayerBehaviorEvents.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default)
    {
        var profile = await _db.PlayerMindProfiles
            .FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);

        if (profile is null)
        {
            return await GetOrCreateAsync(playerId, ct);
        }

        var recentEvents = await _db.PlayerBehaviorEvents
            .Where(x => x.PlayerId == playerId)
            .OrderByDescending(x => x.OccurredAt)
            .Take(100)
            .ToListAsync(ct);

        var answerEvents = recentEvents
            .Where(x => x.EventType == "question_answered")
            .ToList();

        var incorrect = 0;
        foreach (var e in answerEvents)
        {
            try
            {
                using var doc = JsonDocument.Parse(e.MetadataJson);
                if (doc.RootElement.TryGetProperty("correct", out var correct) &&
                    correct.ValueKind == JsonValueKind.False)
                {
                    incorrect++;
                }
            }
            catch
            {
                // Ignore malformed metadata.
            }
        }

        var missRate = answerEvents.Count == 0
            ? 0m
            : (decimal)incorrect / answerEvents.Count;

        profile.FrustrationRiskScore = Math.Clamp(missRate, 0m, 1m);
        profile.ConfidenceLevel = Math.Clamp(1m - missRate, 0m, 1m);
        profile.Archetype = profile.FrustrationRiskScore >= 0.70m
            ? "confidence_builder"
            : "balanced_player";
        profile.LastCalculatedAt = DateTimeOffset.UtcNow;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(ct);

        return ToDto(profile);
    }

    private static PlayerMindProfileDto ToDto(PlayerMindProfile p)
    {
        return new PlayerMindProfileDto(
            p.PlayerId,
            p.ConfidenceLevel,
            p.RiskTolerance,
            p.PreferredPace,
            p.LearningStyle,
            p.CompetitivePreference,
            p.SocialPreference,
            p.ChurnRiskScore,
            p.FrustrationRiskScore,
            p.RewardSensitivityScore,
            p.StoreAffinityScore,
            p.NotificationFatigueScore,
            p.Archetype,
            DeserializeDecimalMap(p.CategoryStrengthsJson),
            DeserializeDecimalMap(p.CategoryWeaknessesJson),
            DeserializeObjectMap(p.PreferenceJson),
            DeserializeObjectMap(p.GuardrailJson),
            p.LastCalculatedAt
        );
    }

    private static Dictionary<string, decimal> DeserializeDecimalMap(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, decimal>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static Dictionary<string, object> DeserializeObjectMap(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }
}
```

---

## 13. PersonalizationService Skeleton

```csharp
using Tycoon.Backend.Application.Personalization.DTOs;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationService : IPersonalizationService
{
    private readonly IPlayerMindProfileService _profiles;
    private readonly IPersonalizationGuardrailService _guardrails;
    private readonly IPersonalizationAuditService _audit;

    public PersonalizationService(
        IPlayerMindProfileService profiles,
        IPersonalizationGuardrailService guardrails,
        IPersonalizationAuditService audit)
    {
        _profiles = profiles;
        _guardrails = guardrails;
        _audit = audit;
    }

    public async Task<PlayerHomePersonalizationDto> GetHomeAsync(
        Guid playerId,
        CancellationToken ct = default)
    {
        var profile = await _profiles.GetOrCreateAsync(playerId, ct);

        var candidates = BuildLocalCandidates(profile);

        var approved = new List<PlayerRecommendationDto>();
        var priority = 100;

        foreach (var candidate in candidates)
        {
            var guardrail = _guardrails.Apply(profile, candidate);

            await _audit.LogDecisionAsync(
                playerId,
                null,
                guardrail.Allowed ? "allowed" : "blocked",
                "backend",
                guardrail.BlockReason ?? candidate.Reason,
                profile,
                candidate,
                guardrail.AppliedRules,
                new { guardrail.Allowed },
                ct);

            if (!guardrail.Allowed) continue;

            approved.Add(new PlayerRecommendationDto(
                Guid.NewGuid(),
                candidate.Type,
                priority--,
                candidate.Score,
                candidate.Reason,
                candidate.Payload,
                guardrail.AppliedRules,
                DateTimeOffset.UtcNow.AddHours(6)
            ));
        }

        var coach = BuildCoachBrief(profile);

        return new PlayerHomePersonalizationDto(
            playerId,
            RecommendedMode: profile.FrustrationRiskScore >= 0.65m ? "learn" : "play",
            RecommendedCategory: PickWeakCategory(profile),
            RecommendedDifficulty: profile.ConfidenceLevel < 0.45m ? "easy" : "medium",
            Recommendations: approved,
            CoachBrief: coach,
            Guardrails: new Dictionary<string, object>
            {
                ["frustrationRiskScore"] = profile.FrustrationRiskScore,
                ["notificationFatigueScore"] = profile.NotificationFatigueScore
            }
        );
    }

    public async Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(
        Guid playerId,
        CancellationToken ct = default)
    {
        var home = await GetHomeAsync(playerId, ct);
        return home.Recommendations;
    }

    public Task AcceptRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default)
    {
        // TODO: mark accepted_at and record behavior event.
        return Task.CompletedTask;
    }

    public Task DismissRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default)
    {
        // TODO: mark dismissed_at and record behavior event.
        return Task.CompletedTask;
    }

    private static List<PersonalizationCandidateDto> BuildLocalCandidates(PlayerMindProfileDto profile)
    {
        var candidates = new List<PersonalizationCandidateDto>();

        if (profile.FrustrationRiskScore >= 0.65m)
        {
            candidates.Add(new PersonalizationCandidateDto(
                "learning_module",
                "confidence-warmup",
                0.92m,
                "You had a tougher recent session, so a low-pressure warm-up is recommended.",
                new Dictionary<string, object>
                {
                    ["title"] = "Confidence Warm-Up",
                    ["route"] = "/learn-hub",
                    ["tone"] = "encouraging"
                }
            ));
        }

        candidates.Add(new PersonalizationCandidateDto(
            "mission",
            "daily-focus",
            0.75m,
            "This mission fits your current play pattern.",
            new Dictionary<string, object>
            {
                ["title"] = "Daily Focus",
                ["route"] = "/missions"
            }
        ));

        return candidates;
    }

    private static CoachBriefDto BuildCoachBrief(PlayerMindProfileDto profile)
    {
        if (profile.FrustrationRiskScore >= 0.65m)
        {
            return new CoachBriefDto(
                "Warm-Up Recommended",
                "Try a quick practice round before jumping into ranked play.",
                "learn",
                "/learn-hub",
                "low_pressure"
            );
        }

        return new CoachBriefDto(
            "Ready for a Challenge",
            "You look ready for a strong session. Try a focused match.",
            "play",
            "/game-menu",
            "encouraging"
        );
    }

    private static string? PickWeakCategory(PlayerMindProfileDto profile)
    {
        if (profile.CategoryWeaknesses.Count == 0) return null;

        return profile.CategoryWeaknesses
            .OrderByDescending(x => x.Value)
            .First()
            .Key;
    }
}
```

---

## 14. Audit Service Skeleton

```csharp
using System.Text.Json;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Personalization;

namespace Tycoon.Backend.Application.Personalization;

public sealed class PersonalizationAuditService : IPersonalizationAuditService
{
    private readonly IAppDb _db;

    public PersonalizationAuditService(IAppDb db)
    {
        _db = db;
    }

    public async Task LogDecisionAsync(
        Guid playerId,
        Guid? recommendationId,
        string decisionType,
        string source,
        string reason,
        object inputSignals,
        object candidate,
        object guardrails,
        object finalDecision,
        CancellationToken ct = default)
    {
        _db.PersonalizationAuditLogs.Add(new PersonalizationAuditLog
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            RecommendationId = recommendationId,
            DecisionType = decisionType,
            Source = source,
            Reason = reason,
            InputSignalsJson = JsonSerializer.Serialize(inputSignals),
            CandidateJson = JsonSerializer.Serialize(candidate),
            GuardrailsAppliedJson = JsonSerializer.Serialize(guardrails),
            FinalDecisionJson = JsonSerializer.Serialize(finalDecision),
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _db.SaveChangesAsync(ct);
    }
}
```

---

## 15. Public API Endpoints

### `PersonalizationEndpoints.cs`

```csharp
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Backend.Application.Personalization.DTOs;

namespace Tycoon.Backend.Api.Features.Personalization;

public static class PersonalizationEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/personalization")
            .RequireAuthorization()
            .WithTags("Personalization")
            .WithOpenApi();

        group.MapGet("/profile/{playerId:guid}", async (
            Guid playerId,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            var profile = await profiles.GetOrCreateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapPost("/profile/{playerId:guid}/event", async (
            Guid playerId,
            [FromBody] PlayerBehaviorEventDto request,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            await profiles.RecordEventAsync(playerId, request, ct);
            return Results.Accepted();
        });

        group.MapPost("/profile/{playerId:guid}/recalculate", async (
            Guid playerId,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            var profile = await profiles.RecalculateAsync(playerId, ct);
            return Results.Ok(profile);
        });

        group.MapGet("/home/{playerId:guid}", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var result = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapGet("/recommendations/{playerId:guid}", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var result = await personalization.GetRecommendationsAsync(playerId, ct);
            return Results.Ok(result);
        });

        group.MapPost("/recommendations/{recommendationId:guid}/accept", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            await personalization.AcceptRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });

        group.MapPost("/recommendations/{recommendationId:guid}/dismiss", async (
            Guid recommendationId,
            [FromQuery] Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            await personalization.DismissRecommendationAsync(recommendationId, playerId, ct);
            return Results.NoContent();
        });
    }
}
```

---

## 16. Coach Endpoints

```csharp
using Tycoon.Backend.Application.Personalization;
using Tycoon.Backend.Application.Personalization.DTOs;

namespace Tycoon.Backend.Api.Features.Coach;

public static class CoachEndpoints
{
    public static void Map(WebApplication app)
    {
        var group = app.MapGroup("/coach")
            .RequireAuthorization()
            .WithTags("Coach")
            .WithOpenApi();

        group.MapGet("/{playerId:guid}/daily-brief", async (
            Guid playerId,
            IPersonalizationService personalization,
            CancellationToken ct) =>
        {
            var home = await personalization.GetHomeAsync(playerId, ct);
            return Results.Ok(home.CoachBrief);
        });

        group.MapPost("/{playerId:guid}/feedback", async (
            Guid playerId,
            CoachFeedbackRequest request,
            IPlayerMindProfileService profiles,
            CancellationToken ct) =>
        {
            await profiles.RecordEventAsync(playerId, new PlayerBehaviorEventDto(
                "coach_feedback",
                "coach",
                null,
                null,
                null,
                new Dictionary<string, object>
                {
                    ["briefId"] = request.BriefId,
                    ["feedback"] = request.Feedback
                },
                DateTimeOffset.UtcNow
            ), ct);

            return Results.Accepted();
        });
    }
}

public sealed record CoachFeedbackRequest(string BriefId, string Feedback);
```

---

## 17. Admin Debug Endpoint

### `AdminPersonalizationEndpoints.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Api.Features.AdminPersonalization;

public static class AdminPersonalizationEndpoints
{
    public static void Map(RouteGroupBuilder admin)
    {
        var group = admin.MapGroup("/personalization")
            .WithTags("AdminPersonalization")
            .WithOpenApi();

        group.MapGet("/debug/{playerId:guid}", async (
            Guid playerId,
            IAppDb db,
            CancellationToken ct) =>
        {
            var profile = await db.PlayerMindProfiles
                .FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);

            var recentEvents = await db.PlayerBehaviorEvents
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.OccurredAt)
                .Take(25)
                .ToListAsync(ct);

            var recentAudit = await db.PersonalizationAuditLogs
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAt)
                .Take(25)
                .ToListAsync(ct);

            return Results.Ok(new
            {
                playerId,
                profile,
                recentEvents,
                recentAudit
            });
        });

        group.MapGet("/summary", async (IAppDb db, CancellationToken ct) =>
        {
            var archetypes = await db.PlayerMindProfiles
                .GroupBy(x => x.Archetype)
                .Select(g => new { archetype = g.Key, count = g.Count() })
                .ToListAsync(ct);

            return Results.Ok(new
            {
                archetypes,
                generatedAt = DateTimeOffset.UtcNow
            });
        });
    }
}
```

---

## 18. Program.cs Registration

Add usings:

```csharp
using Tycoon.Backend.Api.Features.Personalization;
using Tycoon.Backend.Api.Features.Coach;
using Tycoon.Backend.Api.Features.AdminPersonalization;
using Tycoon.Backend.Application.Personalization;
```

Add services:

```csharp
builder.Services.Configure<PersonalizationOptions>(
    builder.Configuration.GetSection("Personalization"));

builder.Services.AddScoped<IPlayerMindProfileService, PlayerMindProfileService>();
builder.Services.AddScoped<IPersonalizationService, PersonalizationService>();
builder.Services.AddScoped<IPersonalizationGuardrailService, PersonalizationGuardrailService>();
builder.Services.AddScoped<IPersonalizationAuditService, PersonalizationAuditService>();
```

Map public endpoints:

```csharp
PersonalizationEndpoints.Map(app);
CoachEndpoints.Map(app);
```

Map admin endpoints:

```csharp
AdminPersonalizationEndpoints.Map(admin);
```

---

# Part B — FastAPI Sidecar Scaffolding

---

## 19. Suggested Sidecar Structure

```text
sidecar/
  app/
    api/
      routes/
        personalization.py
    schemas/
      personalization.py
    services/
      personalization_scoring.py
```

---

## 20. Sidecar Schemas

### `app/schemas/personalization.py`

```python
from pydantic import BaseModel, Field
from typing import Any, Dict, List, Optional


class BehaviorEvent(BaseModel):
    eventType: str
    eventSource: str
    category: Optional[str] = None
    difficulty: Optional[str] = None
    mode: Optional[str] = None
    metadata: Dict[str, Any] = Field(default_factory=dict)


class PlayerProfileSnapshot(BaseModel):
    confidenceLevel: float = 0.5
    churnRiskScore: float = 0.0
    frustrationRiskScore: float = 0.0
    notificationFatigueScore: float = 0.0
    archetype: str = "new_player"


class ScorePlayerRequest(BaseModel):
    playerId: str
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)
    currentProfile: PlayerProfileSnapshot


class ScorePlayerResponse(BaseModel):
    churnRiskScore: float
    frustrationRiskScore: float
    confidenceLevel: float
    recommendedArchetype: str
    categoryStrengths: Dict[str, float] = Field(default_factory=dict)
    categoryWeaknesses: Dict[str, float] = Field(default_factory=dict)
    signals: Dict[str, Any] = Field(default_factory=dict)


class RecommendationCandidate(BaseModel):
    type: str
    targetId: Optional[str] = None
    score: float
    reason: str
    payload: Dict[str, Any] = Field(default_factory=dict)


class RecommendationCandidateRequest(BaseModel):
    playerId: str
    profile: PlayerProfileSnapshot
    recentEvents: List[BehaviorEvent] = Field(default_factory=list)


class RecommendationCandidateResponse(BaseModel):
    candidates: List[RecommendationCandidate]
```

---

## 21. Sidecar Scoring Service

### `app/services/personalization_scoring.py`

```python
from app.schemas.personalization import (
    ScorePlayerRequest,
    ScorePlayerResponse,
    RecommendationCandidate,
    RecommendationCandidateRequest,
    RecommendationCandidateResponse,
)


def score_player(request: ScorePlayerRequest) -> ScorePlayerResponse:
    total_answers = 0
    incorrect = 0
    slow_answers = 0
    category_misses: dict[str, int] = {}

    for event in request.recentEvents:
        if event.eventType != "question_answered":
            continue

        total_answers += 1

        correct = bool(event.metadata.get("correct", False))
        answer_time_ms = int(event.metadata.get("answerTimeMs", 0))

        if not correct:
            incorrect += 1
            if event.category:
                category_misses[event.category] = category_misses.get(event.category, 0) + 1

        if answer_time_ms > 9000:
            slow_answers += 1

    miss_rate = incorrect / total_answers if total_answers else 0.0
    slow_rate = slow_answers / total_answers if total_answers else 0.0

    frustration = min(1.0, (miss_rate * 0.65) + (slow_rate * 0.35))
    confidence = max(0.0, min(1.0, 1.0 - frustration))

    if frustration >= 0.70:
        archetype = "confidence_builder"
    elif total_answers < 5:
        archetype = "new_player"
    else:
        archetype = request.currentProfile.archetype

    weaknesses = {
        category: min(1.0, misses / max(1, total_answers))
        for category, misses in category_misses.items()
    }

    return ScorePlayerResponse(
        churnRiskScore=min(1.0, request.currentProfile.churnRiskScore + (frustration * 0.10)),
        frustrationRiskScore=frustration,
        confidenceLevel=confidence,
        recommendedArchetype=archetype,
        categoryStrengths={},
        categoryWeaknesses=weaknesses,
        signals={
            "missRate": miss_rate,
            "slowRate": slow_rate,
            "totalAnswers": total_answers,
        },
    )


def recommendation_candidates(
    request: RecommendationCandidateRequest,
) -> RecommendationCandidateResponse:
    candidates: list[RecommendationCandidate] = []

    if request.profile.frustrationRiskScore >= 0.65:
        candidates.append(
            RecommendationCandidate(
                type="learning_module",
                targetId="confidence-warmup",
                score=0.92,
                reason="Player has elevated frustration risk; recommend low-pressure learning.",
                payload={"tone": "encouraging", "difficultyStrategy": "warmup"},
            )
        )

    if request.profile.notificationFatigueScore < 0.50:
        candidates.append(
            RecommendationCandidate(
                type="coach_tip",
                targetId=None,
                score=0.70,
                reason="Player can receive one helpful coach recommendation.",
                payload={"tone": "supportive"},
            )
        )

    return RecommendationCandidateResponse(candidates=candidates)
```

---

## 22. Sidecar Routes

### `app/api/routes/personalization.py`

```python
from fastapi import APIRouter

from app.schemas.personalization import (
    ScorePlayerRequest,
    ScorePlayerResponse,
    RecommendationCandidateRequest,
    RecommendationCandidateResponse,
)
from app.services.personalization_scoring import (
    score_player,
    recommendation_candidates,
)

router = APIRouter(prefix="/personalization", tags=["personalization"])


@router.post("/score-player", response_model=ScorePlayerResponse)
async def score_player_route(request: ScorePlayerRequest) -> ScorePlayerResponse:
    return score_player(request)


@router.post(
    "/recommendation-candidates",
    response_model=RecommendationCandidateResponse,
)
async def recommendation_candidates_route(
    request: RecommendationCandidateRequest,
) -> RecommendationCandidateResponse:
    return recommendation_candidates(request)
```

Register in `app/main.py`:

```python
from fastapi import FastAPI
from app.api.routes.personalization import router as personalization_router

app = FastAPI(title="Synaptix Sidecar")

app.include_router(personalization_router)
```

---

## 23. Testing Checklist

Backend tests:

- `GetOrCreateAsync` creates default profile.
- `RecordEventAsync` stores event.
- `RecalculateAsync` updates confidence/frustration.
- Store offer blocked for high frustration.
- Notification blocked for high fatigue.
- Ranked difficulty modifier blocked.
- `/personalization/home/{playerId}` returns safe payload.
- `/admin/personalization/debug/{playerId}` returns profile/events/audit.

Sidecar tests:

- Score endpoint handles empty events.
- Score endpoint calculates frustration.
- Recommendation candidate endpoint returns low-pressure recommendation for high frustration.

---

## 24. Implementation Order

1. Add domain models.
2. Add DbSet properties and EF configurations.
3. Add migration.
4. Add DTOs.
5. Add services.
6. Add guardrails.
7. Add audit service.
8. Add endpoints.
9. Add Program.cs registrations.
10. Add Sidecar routes.
11. Add tests.
12. Connect frontend.

---

## 25. Final Notes

This scaffold intentionally starts conservative.

Recommended production posture:

```text
AdaptiveQuestions = false initially
AdaptiveMissions = true
AdaptiveStore = true with guardrails
AdaptiveNotifications = true with fatigue limits
CoachEnabled = true
UseSidecar = true with fallback
```
