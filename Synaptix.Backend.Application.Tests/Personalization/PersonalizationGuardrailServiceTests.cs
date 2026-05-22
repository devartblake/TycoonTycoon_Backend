using FluentAssertions;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Personalization;

public sealed class PersonalizationGuardrailServiceTests
{
    private static PersonalizationGuardrailService NewService(
        bool enabled = true,
        decimal frustrationThreshold = 0.75m,
        decimal notificationFatigueThreshold = 0.70m)
    {
        var options = Options.Create(new PersonalizationOptions
        {
            Enabled = enabled,
            FrustrationPaidOfferSuppressionThreshold = frustrationThreshold,
            NotificationFatigueThreshold = notificationFatigueThreshold
        });
        return new PersonalizationGuardrailService(options);
    }

    private static PlayerMindProfileDto MakeProfile(
        decimal frustrationRiskScore = 0m,
        decimal notificationFatigueScore = 0m,
        bool personalizationEnabled = true) =>
        new(
            PlayerId: Guid.NewGuid(),
            ConfidenceLevel: 0.50m,
            RiskTolerance: 0.50m,
            PreferredPace: "balanced",
            LearningStyle: "mixed",
            CompetitivePreference: "balanced",
            SocialPreference: "solo",
            ChurnRiskScore: 0m,
            FrustrationRiskScore: frustrationRiskScore,
            RewardSensitivityScore: 0.50m,
            StoreAffinityScore: 0.50m,
            NotificationFatigueScore: notificationFatigueScore,
            Archetype: "new_player",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Preferences: new Dictionary<string, object>(),
            Guardrails: new Dictionary<string, object>(),
            PersonalizationEnabled: personalizationEnabled,
            SidecarScoringEnabled: true,
            LastCalculatedAt: null);

    private static PersonalizationCandidateDto MakeCandidate(
        string type = "mission",
        decimal score = 0.80m) =>
        new(type, score, new Dictionary<string, object>());

    // ── System-level personalization disabled ─────────────────────────────────

    [Fact]
    public void Apply_BlocksAll_WhenPersonalizationGloballyDisabled()
    {
        var svc = NewService(enabled: false);
        var result = svc.Apply(MakeProfile(), MakeCandidate());

        result.Allowed.Should().BeFalse();
        result.AppliedRules.Should().ContainKey("personalization_disabled");
    }

    // ── Player opt-out ────────────────────────────────────────────────────────

    [Fact]
    public void Apply_Blocks_WhenPlayerPersonalizationDisabled()
    {
        var svc = NewService();
        var result = svc.Apply(MakeProfile(personalizationEnabled: false), MakeCandidate());

        result.Allowed.Should().BeFalse();
        result.AppliedRules.Should().ContainKey("player_personalization_disabled");
    }

    // ── Frustration / store offer ─────────────────────────────────────────────

    [Fact]
    public void Apply_BlocksStoreOffer_WhenFrustrationRiskAtOrAboveThreshold()
    {
        var svc = NewService(frustrationThreshold: 0.75m);
        var profile = MakeProfile(frustrationRiskScore: 0.75m);
        var candidate = MakeCandidate(type: "store_offer");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Contain("frustration");
        result.AppliedRules.Should().ContainKey("suppress_paid_offers_when_frustrated");
    }

    [Fact]
    public void Apply_AllowsStoreOffer_WhenFrustrationRiskBelowThreshold()
    {
        var svc = NewService(frustrationThreshold: 0.75m);
        var profile = MakeProfile(frustrationRiskScore: 0.74m);
        var candidate = MakeCandidate(type: "store_offer");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeTrue();
    }

    // ── Notification fatigue ──────────────────────────────────────────────────

    [Fact]
    public void Apply_BlocksNotification_WhenFatigueAtOrAboveThreshold()
    {
        var svc = NewService(notificationFatigueThreshold: 0.70m);
        var profile = MakeProfile(notificationFatigueScore: 0.70m);
        var candidate = MakeCandidate(type: "notification");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Contain("fatigue");
        result.AppliedRules.Should().ContainKey("notification_fatigue_limit");
    }

    [Fact]
    public void Apply_AllowsNotification_WhenFatigueBelowThreshold()
    {
        var svc = NewService(notificationFatigueThreshold: 0.70m);
        var profile = MakeProfile(notificationFatigueScore: 0.69m);
        var candidate = MakeCandidate(type: "notification");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeTrue();
    }

    // ── Ranked difficulty lock ────────────────────────────────────────────────

    [Fact]
    public void Apply_BlocksRankedDifficultyModifier_Unconditionally()
    {
        var svc = NewService();
        var candidate = MakeCandidate(type: "ranked_difficulty_modifier");

        var result = svc.Apply(MakeProfile(), candidate);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Contain("Ranked difficulty");
        result.AppliedRules.Should().ContainKey("ranked_fairness_lock");
    }

    // ── Sidecar direct reward grant ───────────────────────────────────────────

    [Fact]
    public void Apply_BlocksRewardGrant_PreventsSidecarFromGrantingRewards()
    {
        var svc = NewService();
        var candidate = MakeCandidate(type: "reward_grant");

        var result = svc.Apply(MakeProfile(), candidate);

        result.Allowed.Should().BeFalse();
        result.BlockReason.Should().Contain("reward");
        result.AppliedRules.Should().ContainKey("sidecar_direct_reward_grant_blocked");
    }

    // ── Allow-through ─────────────────────────────────────────────────────────

    [Fact]
    public void Apply_AllowsStoreFreeOffer_EvenWhenFrustrationRiskAboveThreshold()
    {
        var svc = NewService(frustrationThreshold: 0.75m);
        var profile = MakeProfile(frustrationRiskScore: 0.80m);
        var candidate = MakeCandidate(type: "store_free_offer");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeTrue("free support offers must pass through for frustrated players");
    }

    [Fact]
    public void Apply_Allows_WhenNoGuardrailsTriggered()
    {
        var svc = NewService();
        var profile = MakeProfile(frustrationRiskScore: 0.10m, notificationFatigueScore: 0.10m);
        var candidate = MakeCandidate(type: "mission");

        var result = svc.Apply(profile, candidate);

        result.Allowed.Should().BeTrue();
        result.BlockReason.Should().BeNull();
        result.AppliedRules.Should().ContainKey("allowed");
    }
}
