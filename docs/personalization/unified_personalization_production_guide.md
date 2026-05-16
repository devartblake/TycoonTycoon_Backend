# Synaptix Unified Personalization Layer — Production Implementation Guide

## 1. Executive Summary

This document upgrades the Unified Personalization Layer into a production-ready backend implementation plan for Synaptix / Trivia Tycoon.

The goal is to connect gameplay, learning, study, missions, store, notifications, events, matchmaking, and ML scoring into a single safe personalization layer.

The system follows this authority model:

```text
Flutter Frontend = renders recommendations and sends player actions
.NET Backend = owns authoritative rules, persistence, rewards, guardrails, and final decisions
FastAPI Sidecar = calculates ML/heuristic scores and recommendation candidates
```

The Sidecar should never directly mutate wallets, purchases, ranked outcomes, rewards, or player progression.

---

## 2. Target Architecture

```text
Flutter Client
  │
  ├── POST /personalization/profile/{playerId}/event
  ├── GET  /personalization/home/{playerId}
  ├── GET  /personalization/recommendations/{playerId}
  └── GET  /coach/{playerId}/daily-brief
        │
        ▼
.NET Backend API
        │
        ├── PlayerMindProfileService
        ├── PersonalizationService
        ├── PersonalizationGuardrailService
        ├── PersonalizationRecommendationService
        └── PersonalizationSidecarClient
                │
                ▼
FastAPI Sidecar
        │
        ├── /personalization/score-player
        ├── /personalization/recommendation-candidates
        ├── /personalization/category-profile
        ├── /personalization/notification-score
        └── /personalization/mission-fit
```

---

## 3. Core Domain Concepts

### 3.1 PlayerMindProfile

A gameplay personalization profile. This is not a clinical/psychological profile. It tracks inferred play-style signals used to make the game more adaptive, fair, and engaging.

Tracked dimensions:

| Dimension | Purpose |
|---|---|
| confidenceLevel | Estimate whether the player needs easier ramp-up or mastery pushes |
| riskTolerance | Estimate whether to recommend high-risk challenges |
| preferredPace | Slow, balanced, or fast play preference |
| learningStyle | Mixed, visual, repetition, mastery, competitive |
| competitivePreference | Solo, casual, ranked, high-competition |
| socialPreference | Solo, friends, guild/co-op, challenger |
| churnRiskScore | Likelihood of disengagement |
| frustrationRiskScore | Likelihood of negative session experience |
| rewardSensitivityScore | Responsiveness to rewards |
| storeAffinityScore | Responsiveness to store interactions |
| notificationFatigueScore | Risk of over-notifying |
| archetype | Human-readable player pattern |

Recommended archetypes:

```text
new_player
confidence_builder
streak_seeker
explorer
collector
risk_taker
social_challenger
mastery_path
comeback_player
premium_power_user
low_pressure_learner
```

---

## 4. PostgreSQL Schema

> Assumption: PostgreSQL 15/16 with `pgcrypto` enabled for `gen_random_uuid()`.

### 4.1 Migration SQL

```sql
CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TABLE IF NOT EXISTS player_mind_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL UNIQUE,

    confidence_level NUMERIC(5, 2) NOT NULL DEFAULT 0.50 CHECK (confidence_level >= 0 AND confidence_level <= 1),
    risk_tolerance NUMERIC(5, 2) NOT NULL DEFAULT 0.50 CHECK (risk_tolerance >= 0 AND risk_tolerance <= 1),

    preferred_pace TEXT NOT NULL DEFAULT 'balanced',
    learning_style TEXT NOT NULL DEFAULT 'mixed',
    competitive_preference TEXT NOT NULL DEFAULT 'balanced',
    social_preference TEXT NOT NULL DEFAULT 'solo',

    churn_risk_score NUMERIC(5, 2) NOT NULL DEFAULT 0.00 CHECK (churn_risk_score >= 0 AND churn_risk_score <= 1),
    frustration_risk_score NUMERIC(5, 2) NOT NULL DEFAULT 0.00 CHECK (frustration_risk_score >= 0 AND frustration_risk_score <= 1),
    reward_sensitivity_score NUMERIC(5, 2) NOT NULL DEFAULT 0.50 CHECK (reward_sensitivity_score >= 0 AND reward_sensitivity_score <= 1),
    store_affinity_score NUMERIC(5, 2) NOT NULL DEFAULT 0.50 CHECK (store_affinity_score >= 0 AND store_affinity_score <= 1),
    notification_fatigue_score NUMERIC(5, 2) NOT NULL DEFAULT 0.00 CHECK (notification_fatigue_score >= 0 AND notification_fatigue_score <= 1),

    archetype TEXT NOT NULL DEFAULT 'new_player',

    category_strengths_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    category_weaknesses_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    preference_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    guardrail_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    sidecar_scores_json JSONB NOT NULL DEFAULT '{}'::jsonb,

    personalization_enabled BOOLEAN NOT NULL DEFAULT true,
    sidecar_scoring_enabled BOOLEAN NOT NULL DEFAULT true,

    last_calculated_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),

    CONSTRAINT ck_player_mind_profiles_preferred_pace
        CHECK (preferred_pace IN ('slow', 'balanced', 'fast')),

    CONSTRAINT ck_player_mind_profiles_learning_style
        CHECK (learning_style IN ('mixed', 'visual', 'repetition', 'mastery', 'competitive')),

    CONSTRAINT ck_player_mind_profiles_competitive_preference
        CHECK (competitive_preference IN ('solo', 'casual', 'balanced', 'ranked', 'high_competition')),

    CONSTRAINT ck_player_mind_profiles_social_preference
        CHECK (social_preference IN ('solo', 'friends', 'guild', 'challenger', 'co_op'))
);

CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_archetype
ON player_mind_profiles (archetype);

CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_churn_risk
ON player_mind_profiles (churn_risk_score DESC);

CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_frustration_risk
ON player_mind_profiles (frustration_risk_score DESC);

CREATE INDEX IF NOT EXISTS ix_player_mind_profiles_updated_at
ON player_mind_profiles (updated_at DESC);
```

### 4.2 Behavior Events Table

```sql
CREATE TABLE IF NOT EXISTS player_behavior_events (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL,

    event_type TEXT NOT NULL,
    event_source TEXT NOT NULL,

    category TEXT NULL,
    difficulty TEXT NULL,
    mode TEXT NULL,

    metadata_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    occurred_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    ingested_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_player_time
ON player_behavior_events (player_id, occurred_at DESC);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_type
ON player_behavior_events (event_type);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_source
ON player_behavior_events (event_source);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_category
ON player_behavior_events (category);

CREATE INDEX IF NOT EXISTS ix_player_behavior_events_metadata_gin
ON player_behavior_events USING GIN (metadata_json);
```

Recommended event types:

```text
session_started
session_ended
question_answered
question_skipped
hint_used
powerup_used
match_started
match_completed
match_abandoned
learning_module_started
learning_module_completed
study_set_started
study_set_completed
mission_started
mission_completed
store_item_viewed
store_item_purchased
reward_claimed
notification_opened
notification_dismissed
recommendation_accepted
recommendation_dismissed
```

### 4.3 Recommendations Table

```sql
CREATE TABLE IF NOT EXISTS personalization_recommendations (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    player_id UUID NOT NULL,

    recommendation_type TEXT NOT NULL,
    source TEXT NOT NULL DEFAULT 'backend',

    priority INT NOT NULL DEFAULT 0,
    score NUMERIC(5, 2) NOT NULL DEFAULT 0.50 CHECK (score >= 0 AND score <= 1),

    payload_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    guardrail_json JSONB NOT NULL DEFAULT '{}'::jsonb,

    expires_at TIMESTAMPTZ NULL,
    accepted_at TIMESTAMPTZ NULL,
    dismissed_at TIMESTAMPTZ NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_personalization_recommendations_player_created
ON personalization_recommendations (player_id, created_at DESC);

CREATE INDEX IF NOT EXISTS ix_personalization_recommendations_type
ON personalization_recommendations (recommendation_type);

CREATE INDEX IF NOT EXISTS ix_personalization_recommendations_active
ON personalization_recommendations (player_id, expires_at)
WHERE accepted_at IS NULL AND dismissed_at IS NULL;
```

Recommended recommendation types:

```text
mode
question_set
learning_module
study_set
mission
store_offer
notification
coach_tip
event
skill_tree_path
```

### 4.4 Rules Table

```sql
CREATE TABLE IF NOT EXISTS personalization_rules (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    rule_key TEXT NOT NULL UNIQUE,
    description TEXT NOT NULL DEFAULT '',
    is_enabled BOOLEAN NOT NULL DEFAULT true,
    rule_json JSONB NOT NULL DEFAULT '{}'::jsonb,
    created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

Seed examples:

```sql
INSERT INTO personalization_rules (rule_key, description, rule_json)
VALUES
('suppress_paid_offers_when_frustrated', 'Prevent paid offer recommendations when frustration is high.', '{"frustrationThreshold":0.75}'::jsonb),
('notification_fatigue_limit', 'Limit notification recommendations for fatigued players.', '{"fatigueThreshold":0.70,"maxDaily":2}'::jsonb),
('ranked_fairness_lock', 'Prevent personalization from changing ranked fairness.', '{"enabled":true}'::jsonb)
ON CONFLICT (rule_key) DO NOTHING;
```

---

## 5. EF Core Entities

### 5.1 PlayerMindProfile

```csharp
namespace Tycoon.Backend.Domain.Personalization;

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

    public decimal ChurnRiskScore { get; set; }
    public decimal FrustrationRiskScore { get; set; }
    public decimal RewardSensitivityScore { get; set; } = 0.50m;
    public decimal StoreAffinityScore { get; set; } = 0.50m;
    public decimal NotificationFatigueScore { get; set; }

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

### 5.2 PlayerBehaviorEvent

```csharp
namespace Tycoon.Backend.Domain.Personalization;

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

### 5.3 PersonalizationRecommendation

```csharp
namespace Tycoon.Backend.Domain.Personalization;

public sealed class PersonalizationRecommendation
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }

    public string RecommendationType { get; set; } = "";
    public string Source { get; set; } = "backend";

    public int Priority { get; set; }
    public decimal Score { get; set; } = 0.50m;

    public string PayloadJson { get; set; } = "{}";
    public string GuardrailJson { get; set; } = "{}";

    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public DateTimeOffset? DismissedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
```

---

## 6. EF Core Configurations

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

        b.Property(x => x.PlayerId).HasColumnName("player_id");
        b.HasIndex(x => x.PlayerId).IsUnique();

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

        b.HasIndex(x => x.Archetype);
        b.HasIndex(x => x.ChurnRiskScore);
        b.HasIndex(x => x.FrustrationRiskScore);
    }
}
```

---

## 7. DTO Contracts

```csharp
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

public sealed record PlayerBehaviorEventDto(
    string EventType,
    string EventSource,
    string? Category,
    string? Difficulty,
    string? Mode,
    Dictionary<string, object>? Metadata,
    DateTimeOffset? OccurredAt
);

public sealed record PlayerRecommendationDto(
    Guid Id,
    string Type,
    int Priority,
    decimal Score,
    Dictionary<string, object> Payload,
    Dictionary<string, object> Guardrails,
    DateTimeOffset? ExpiresAt
);

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

## 8. C# Interfaces

```csharp
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

public interface IPersonalizationSidecarClient
{
    Task<SidecarPlayerScoresDto> ScorePlayerAsync(SidecarPlayerScoringRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
        SidecarRecommendationRequest request,
        CancellationToken ct = default);
}
```

---

## 9. Guardrail Rules

```csharp
public sealed record PersonalizationGuardrailResult(
    bool Allowed,
    string? BlockReason,
    Dictionary<string, object> AppliedRules
);

public sealed class PersonalizationGuardrailService : IPersonalizationGuardrailService
{
    public PersonalizationGuardrailResult Apply(PlayerMindProfileDto profile, PersonalizationCandidateDto candidate)
    {
        var rules = new Dictionary<string, object>();

        if (candidate.Type == "store_offer" && profile.FrustrationRiskScore >= 0.75m)
        {
            rules["suppress_paid_offers_when_frustrated"] = true;
            return new(false, "Paid offers suppressed due to high frustration risk.", rules);
        }

        if (candidate.Type == "notification" && profile.NotificationFatigueScore >= 0.70m)
        {
            rules["notification_fatigue_limit"] = true;
            return new(false, "Notification suppressed due to fatigue risk.", rules);
        }

        if (candidate.Type == "ranked_difficulty_modifier")
        {
            rules["ranked_fairness_lock"] = true;
            return new(false, "Ranked difficulty cannot be modified by personalization.", rules);
        }

        rules["allowed"] = true;
        return new(true, null, rules);
    }
}
```

---

## 10. Backend Endpoints

### 10.1 PersonalizationEndpoints.cs

```csharp
using Microsoft.AspNetCore.Mvc;
using Tycoon.Backend.Application.Personalization;

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

### 10.2 CoachEndpoints.cs

```csharp
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
                EventType: "coach_feedback",
                EventSource: "coach",
                Category: null,
                Difficulty: null,
                Mode: null,
                Metadata: new Dictionary<string, object>
                {
                    ["feedback"] = request.Feedback,
                    ["briefId"] = request.BriefId
                },
                OccurredAt: DateTimeOffset.UtcNow
            ), ct);

            return Results.Accepted();
        });
    }
}

public sealed record CoachFeedbackRequest(string BriefId, string Feedback);
```

---

## 11. Dependency Injection

Add to `Program.cs`:

```csharp
builder.Services.AddScoped<IPlayerMindProfileService, PlayerMindProfileService>();
builder.Services.AddScoped<IPersonalizationService, PersonalizationService>();
builder.Services.AddScoped<IPersonalizationGuardrailService, PersonalizationGuardrailService>();
builder.Services.AddHttpClient<IPersonalizationSidecarClient, PersonalizationSidecarClient>();

// Later in endpoint mapping:
PersonalizationEndpoints.Map(app);
CoachEndpoints.Map(app);
AdminPersonalizationEndpoints.Map(admin);
```

---

## 12. FastAPI Sidecar Implementation

### 12.1 Pydantic Schemas

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

### 12.2 FastAPI Routes

```python
from fastapi import APIRouter
from app.schemas.personalization import (
    ScorePlayerRequest,
    ScorePlayerResponse,
    RecommendationCandidateRequest,
    RecommendationCandidateResponse,
    RecommendationCandidate,
)

router = APIRouter(prefix="/personalization", tags=["personalization"])

@router.post("/score-player", response_model=ScorePlayerResponse)
async def score_player(request: ScorePlayerRequest) -> ScorePlayerResponse:
    incorrect = 0
    total_answers = 0
    slow_answers = 0
    category_misses: dict[str, int] = {}

    for event in request.recentEvents:
        if event.eventType == "question_answered":
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

@router.post("/recommendation-candidates", response_model=RecommendationCandidateResponse)
async def recommendation_candidates(
    request: RecommendationCandidateRequest,
) -> RecommendationCandidateResponse:
    candidates: list[RecommendationCandidate] = []

    if request.profile.frustrationRiskScore >= 0.65:
        candidates.append(RecommendationCandidate(
            type="learning_module",
            targetId="confidence-warmup",
            score=0.92,
            reason="Player has elevated frustration risk; recommend low-pressure learning.",
            payload={"tone": "encouraging", "difficultyStrategy": "warmup"},
        ))

    if request.profile.notificationFatigueScore < 0.50:
        candidates.append(RecommendationCandidate(
            type="coach_tip",
            targetId=None,
            score=0.70,
            reason="Player can receive one helpful coach recommendation.",
            payload={"tone": "supportive"},
        ))

    return RecommendationCandidateResponse(candidates=candidates)
```

### 12.3 Register Sidecar Router

```python
from fastapi import FastAPI
from app.api.routes.personalization import router as personalization_router

app = FastAPI(title="Synaptix Sidecar")

app.include_router(personalization_router)
```

---

## 13. Admin API Requirements

```http
GET  /admin/personalization/summary
GET  /admin/personalization/archetypes
GET  /admin/personalization/recommendations/performance
GET  /admin/personalization/player/{playerId}
POST /admin/personalization/player/{playerId}/recalculate
POST /admin/personalization/player/{playerId}/reset
GET  /admin/personalization/rules
PUT  /admin/personalization/rules
```

Admin dashboard widgets:

- Archetype distribution
- Churn-risk trend
- Frustration-risk trend
- Recommendation acceptance rate
- Recommendation dismissal rate
- Mission completion by archetype
- Learning recovery rate
- Notification fatigue
- Store conversion by offer type
- Question difficulty adjustment outcomes

---

## 14. Flutter Consumption Contract

Frontend should consume:

```http
GET /personalization/home/{playerId}
GET /personalization/recommendations/{playerId}
GET /coach/{playerId}/daily-brief
POST /personalization/recommendations/{recommendationId}/accept?playerId={playerId}
POST /personalization/recommendations/{recommendationId}/dismiss?playerId={playerId}
```

Frontend should not implement ToM logic directly. It should only render backend-approved recommendations.

---

## 15. Production Guardrails

1. Never increase difficulty to sell power-ups.
2. Never target frustrated users with aggressive paid offers.
3. Never bypass ranked fairness.
4. Never grant rewards directly from Sidecar output.
5. Never use sensitive/protected attributes.
6. Allow profile reset.
7. Allow personalization opt-out.
8. Add notification fatigue limits.
9. Log recommendation source and applied guardrails.
10. Keep all economy mutations in the .NET backend.

---

## 16. Recommended PR Order

1. Database + EF models
2. Application interfaces + services
3. Public personalization endpoints
4. Sidecar scoring endpoints
5. Backend Sidecar client
6. Guardrail service
7. Questions/Learning/Study integration
8. Missions integration
9. Notifications/Store integration
10. Coach API
11. Admin analytics
12. A/B testing framework

---

## 17. Done Criteria

- Profiles can be created and recalculated.
- Behavior events are ingested.
- Sidecar can score player state.
- Backend applies guardrails.
- Frontend can fetch home recommendations.
- Admin can view summary metrics.
- Store and notification personalization cannot violate guardrails.
- Ranked gameplay remains fair.
