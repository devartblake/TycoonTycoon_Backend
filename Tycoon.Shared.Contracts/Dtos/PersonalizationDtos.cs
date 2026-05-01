namespace Tycoon.Shared.Contracts.Dtos;

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
    bool PersonalizationEnabled,
    bool SidecarScoringEnabled,
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
    string Source,
    int Priority,
    decimal Score,
    string Reason,
    Dictionary<string, object> Payload,
    Dictionary<string, object> Guardrails,
    DateTimeOffset? ExpiresAt
);

public sealed record CoachBriefDto(
    string Title,
    string Message,
    string RecommendedAction,
    string? TargetRoute,
    string Tone
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

// Sidecar client DTOs
public sealed record SidecarPlayerScoringRequest(
    string PlayerId,
    IReadOnlyList<PlayerBehaviorEventDto> RecentEvents,
    SidecarPlayerSnapshotDto CurrentProfile
);

public sealed record SidecarPlayerSnapshotDto(
    decimal ConfidenceLevel,
    decimal ChurnRiskScore,
    decimal FrustrationRiskScore,
    decimal NotificationFatigueScore,
    string Archetype
);

public sealed record SidecarPlayerScoresDto(
    decimal ChurnRiskScore,
    decimal FrustrationRiskScore,
    decimal ConfidenceLevel,
    string RecommendedArchetype,
    Dictionary<string, decimal> CategoryStrengths,
    Dictionary<string, decimal> CategoryWeaknesses,
    Dictionary<string, object> Signals
);

public sealed record SidecarRecommendationRequest(
    string PlayerId,
    SidecarPlayerSnapshotDto Profile,
    IReadOnlyList<PlayerBehaviorEventDto> RecentEvents
);

public sealed record SidecarRecommendationCandidateDto(
    string Type,
    string? TargetId,
    decimal Score,
    string Reason,
    Dictionary<string, object> Payload
);

// Guardrail types
public sealed record PersonalizationCandidateDto(
    string Type,
    decimal Score,
    Dictionary<string, object> Payload
);

public sealed record PersonalizationGuardrailResult(
    bool Allowed,
    string? BlockReason,
    Dictionary<string, object> AppliedRules
);

// Admin DTOs
public sealed record PersonalizationSummaryDto(
    Dictionary<string, int> ArchetypeCounts,
    int HighChurnRiskCount,
    int HighFrustrationRiskCount,
    int TotalProfiles,
    DateTimeOffset GeneratedAt
);

public sealed record PersonalizationRuleDto(
    Guid Id,
    string RuleKey,
    string Description,
    bool IsEnabled,
    Dictionary<string, object> Rule,
    DateTimeOffset UpdatedAt
);

public sealed record UpdatePersonalizationRuleRequest(
    bool? IsEnabled,
    Dictionary<string, object>? Rule
);

public sealed record CoachFeedbackRequest(string BriefId, string Feedback);
