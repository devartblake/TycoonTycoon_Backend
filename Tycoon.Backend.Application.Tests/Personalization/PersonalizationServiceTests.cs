using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Tests.Personalization;

public sealed class PersonalizationServiceTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static PersonalizationService NewService(
        AppDb db,
        IPersonalizationSidecarClient? sidecar = null,
        bool guardrailsEnabled = true,
        decimal frustrationThreshold = 0.75m,
        decimal notificationFatigueThreshold = 0.70m,
        bool adaptiveStore = true)
    {
        var sidecarClient = sidecar ?? new EmptySidecarClient();

        var mindProfileSvc = new PlayerMindProfileService(db, sidecarClient);

        var guardrailOptions = Options.Create(new PersonalizationOptions
        {
            Enabled = guardrailsEnabled,
            FrustrationPaidOfferSuppressionThreshold = frustrationThreshold,
            NotificationFatigueThreshold = notificationFatigueThreshold
        });
        var guardrailSvc = new PersonalizationGuardrailService(guardrailOptions);

        var serviceOptions = Options.Create(new PersonalizationOptions
        {
            Enabled = guardrailsEnabled,
            AdaptiveStore = adaptiveStore,
            FrustrationPaidOfferSuppressionThreshold = frustrationThreshold,
            NotificationFatigueThreshold = notificationFatigueThreshold
        });

        var auditSvc = new PersonalizationAuditService(db);

        return new PersonalizationService(db, mindProfileSvc, guardrailSvc, sidecarClient, auditSvc, serviceOptions);
    }

    // ── GetHomeAsync — home payload structure ─────────────────────────────────

    [Fact]
    public async Task GetHomeAsync_NewPlayer_ReturnsAllRequiredFields()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);

        home.Should().NotBeNull();
        home.PlayerId.Should().Be(playerId);
        home.RecommendedMode.Should().NotBeNullOrEmpty();
        home.Recommendations.Should().NotBeNull();
        home.CoachBrief.Should().NotBeNull();
        home.Guardrails.Should().NotBeNull();
        home.Guardrails.Should().ContainKey("personalizationEnabled");
        home.Guardrails.Should().ContainKey("sidecarEnabled");
    }

    [Fact]
    public async Task GetHomeAsync_NewPlayer_ReturnsRecommendedCategoryAndDifficulty()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);

        // A new player with no events has empty CategoryWeaknesses → category is null
        home.RecommendedCategory.Should().BeNull();
        // Default confidence = 0.50 → medium difficulty
        home.RecommendedDifficulty.Should().Be("medium");
    }

    // ── GetHomeAsync — recommended mode by archetype ──────────────────────────

    [Theory]
    [InlineData("risk_taker", "ranked")]
    [InlineData("social_challenger", "ranked")]
    [InlineData("low_pressure_learner", "practice")]
    [InlineData("confidence_builder", "practice")]
    [InlineData("mastery_path", "study")]
    [InlineData("new_player", "casual")]
    [InlineData("unknown_archetype", "casual")]
    public async Task GetHomeAsync_RecommendedMode_MatchesArchetype(string archetype, string expectedMode)
    {
        await using var db = NewDb();
        // Use a sidecar stub that reports a specific archetype
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.10m,
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: archetype,
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());
        var svc = NewService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();

        // Trigger recalculation so the archetype is applied from the sidecar
        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var home = await svc.GetHomeAsync(playerId);

        home.RecommendedMode.Should().Be(expectedMode);
    }

    // ── GetHomeAsync — coach brief ────────────────────────────────────────────

    [Fact]
    public async Task GetHomeAsync_HighFrustration_CoachBriefTone_IsSupportive()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.80m,   // >= 0.70 → supportive tone
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: "new_player",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        await mindSvc.RecordEventAsync(Guid.NewGuid(), MakeEvent());

        // Build service with the same scored profile already in db
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var home = await svc.GetHomeAsync(playerId);

        home.CoachBrief!.Tone.Should().Be("supportive");
    }

    [Fact]
    public async Task GetHomeAsync_HighChurnRisk_CoachBriefTone_IsEncouraging()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.80m,           // >= 0.65 → encouraging tone
            FrustrationRiskScore: 0.10m,
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: "new_player",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var home = await svc.GetHomeAsync(playerId);

        home.CoachBrief!.Tone.Should().Be("encouraging");
    }

    // ── GetHomeAsync — sidecar candidates → recommendations ──────────────────

    [Fact]
    public async Task GetHomeAsync_SidecarEnabled_AllowedCandidate_AppearsInRecommendations()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "mission",
            TargetId: "mission-1",
            Score: 0.85m,
            Reason: "You love missions",
            Payload: new Dictionary<string, object> { ["missionId"] = "mission-1" });

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);

        home.Recommendations.Should().HaveCount(1);
        var rec = home.Recommendations[0];
        rec.Type.Should().Be("mission");
        rec.Source.Should().Be("sidecar");
        rec.Score.Should().Be(0.85m);
    }

    [Fact]
    public async Task GetHomeAsync_SidecarEnabled_AllowedCandidate_IsPersisted()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "mission",
            TargetId: null,
            Score: 0.90m,
            Reason: "Top pick",
            Payload: new Dictionary<string, object>());

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        await svc.GetHomeAsync(playerId);

        var stored = await db.PersonalizationRecommendations
            .Where(r => r.PlayerId == playerId)
            .ToListAsync();

        stored.Should().HaveCount(1);
        stored[0].RecommendationType.Should().Be("mission");
        stored[0].Source.Should().Be("sidecar");
        stored[0].Score.Should().Be(0.90m);
    }

    [Fact]
    public async Task GetHomeAsync_SidecarEnabled_PaidOffer_IsBlockedWhenFrustrated()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.80m,   // above suppression threshold (0.75)
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: "new_player",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var candidate = new SidecarRecommendationCandidateDto(
            Type: "store_offer",
            TargetId: "pack-gold",
            Score: 0.95m,
            Reason: "Hot deal",
            Payload: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ComposedSidecarClient(scores, [candidate]));
        var home = await svc.GetHomeAsync(playerId);

        home.Recommendations.Should().BeEmpty("paid offer must be suppressed when frustration risk is high");
    }

    [Fact]
    public async Task GetHomeAsync_SidecarDisabled_ReturnsEmptyRecommendations()
    {
        await using var db = NewDb();
        // profile has PersonalizationEnabled = true (default) but SidecarScoringEnabled = false
        var playerId = Guid.NewGuid();

        // Create a profile with sidecar disabled
        var profileEntity = new Tycoon.Backend.Domain.Personalization.PlayerMindProfile
        {
            PlayerId = playerId,
            PersonalizationEnabled = true,
            SidecarScoringEnabled = false
        };
        db.PlayerMindProfiles.Add(profileEntity);
        await db.SaveChangesAsync();

        var candidatesThatShouldNotBeUsed = new[]
        {
            new SidecarRecommendationCandidateDto("mission", null, 0.9m, "test", new Dictionary<string, object>())
        };

        var svc = NewService(db, new RecommendationSidecarClient(candidatesThatShouldNotBeUsed));
        var home = await svc.GetHomeAsync(playerId);

        home.Recommendations.Should().BeEmpty("sidecar must not be called when SidecarScoringEnabled=false");
    }

    [Fact]
    public async Task GetHomeAsync_SidecarThrows_ReturnsEmptyRecommendations()
    {
        await using var db = NewDb();
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);

        home.Recommendations.Should().BeEmpty("sidecar failure must fall back to empty candidates");
    }

    // ── GetHomeAsync — mission recommendations ────────────────────────────────

    [Fact]
    public async Task GetHomeAsync_NewPlayer_ReturnsRecommendedMissions()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);

        home.RecommendedMissions.Should().NotBeNull();
        home.RecommendedMissions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetHomeAsync_HighFrustrationPlayer_GetsOnlyLowPressureMissions()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.70m,   // >= 0.65 → low-pressure only
            ConfidenceLevel: 0.30m,
            RecommendedArchetype: "confidence_builder",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var home = await svc.GetHomeAsync(playerId);

        home.RecommendedMissions.Should().NotBeEmpty();
        home.RecommendedMissions.Should().AllSatisfy(m => m.IsLowPressure.Should().BeTrue(),
            "high-frustration players must only receive low-pressure missions");
        home.RecommendedMissions.Should().AllSatisfy(m =>
            m.MissionArchetype.Should().Be("confidence_builder"),
            "high-frustration players should get confidence_builder missions");
    }

    [Theory]
    [InlineData("streak_seeker",     "streak_seeker")]
    [InlineData("risk_taker",        "risk_taker")]
    [InlineData("social_challenger", "social_challenger")]
    [InlineData("explorer",          "explorer")]
    [InlineData("comeback_player",   "comeback_player")]
    [InlineData("mastery_path",      "mastery_path")]
    [InlineData("collector",         "collector")]
    [InlineData("new_player",        "confidence_builder")]
    public async Task GetHomeAsync_RecommendedMissions_MatchArchetype(string archetype, string expectedMissionArchetype)
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.10m,   // low frustration — archetype mapping applies
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: archetype,
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var home = await svc.GetHomeAsync(playerId);

        home.RecommendedMissions.Should().NotBeEmpty();
        home.RecommendedMissions.First().MissionArchetype.Should().Be(expectedMissionArchetype);
    }

    [Fact]
    public async Task GetHomeAsync_UnknownArchetype_ReturnsExplorerMission()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.10m,
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: "unknown_archetype",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var home = await svc.GetHomeAsync(playerId);

        home.RecommendedMissions.Should().NotBeEmpty();
        home.RecommendedMissions.First().MissionArchetype.Should().Be("explorer");
    }

    // ── GetRecommendationsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetRecommendationsAsync_ReturnsEquivalentList_ToGetHomeAsync()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "mission",
            TargetId: null,
            Score: 0.75m,
            Reason: "Good fit",
            Payload: new Dictionary<string, object>());

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        var recs = await svc.GetRecommendationsAsync(playerId);

        recs.Should().HaveCount(1);
        recs[0].Source.Should().Be("sidecar");
        recs[0].Score.Should().Be(0.75m);
    }

    // ── AcceptRecommendationAsync / DismissRecommendationAsync ────────────────

    [Fact]
    public async Task AcceptRecommendationAsync_SetsAcceptedAt()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "mission", TargetId: null, Score: 0.8m, Reason: "r",
            Payload: new Dictionary<string, object>());

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);
        var recId = home.Recommendations[0].Id;

        await svc.AcceptRecommendationAsync(recId, playerId);

        var stored = await db.PersonalizationRecommendations.FindAsync(recId);
        stored!.AcceptedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DismissRecommendationAsync_SetsDismissedAt()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "mission", TargetId: null, Score: 0.8m, Reason: "r",
            Payload: new Dictionary<string, object>());

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        var home = await svc.GetHomeAsync(playerId);
        var recId = home.Recommendations[0].Id;

        await svc.DismissRecommendationAsync(recId, playerId);

        var stored = await db.PersonalizationRecommendations.FindAsync(recId);
        stored!.DismissedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptRecommendationAsync_NonExistentId_IsNoOp()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        // Should not throw
        await svc.Invoking(s => s.AcceptRecommendationAsync(Guid.NewGuid(), playerId))
                 .Should().NotThrowAsync();
    }

    // ── GetStoreRecommendationsAsync ──────────────────────────────────────────

    [Fact]
    public async Task GetStoreRecommendationsAsync_ReturnsPlayerId()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.PlayerId.Should().Be(playerId);
        result.Offers.Should().NotBeNull();
        result.AppliedGuardrails.Should().NotBeNull();
        result.AppliedGuardrails.Should().ContainKey("adaptiveStoreEnabled");
        result.AppliedGuardrails.Should().ContainKey("personalizationEnabled");
        result.AppliedGuardrails.Should().ContainKey("frustrationPaidOfferSuppressed");
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_AdaptiveStoreDisabled_ReturnsNoOffers()
    {
        await using var db = NewDb();
        var svc = NewService(db, adaptiveStore: false);
        var playerId = Guid.NewGuid();

        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.Offers.Should().BeEmpty("adaptive store is disabled");
        result.AppliedGuardrails["adaptiveStoreEnabled"].Should().Be(false);
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_FrustratedPlayer_PayloadIncludesFreeOffer()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.80m,   // above suppression threshold (0.75)
            ConfidenceLevel: 0.20m,
            RecommendedArchetype: "confidence_builder",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.Offers.Should().NotBeEmpty("frustrated player should receive a free support offer");
        result.Offers.Should().AllSatisfy(o =>
            o.Type.Should().Be("store_free_offer", "only free offers are allowed for frustrated players"));
        result.AppliedGuardrails["frustrationPaidOfferSuppressed"].Should().Be(true);
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_PaidOffer_SuppressedWhenPlayerFrustrated()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.80m,
            ConfidenceLevel: 0.20m,
            RecommendedArchetype: "confidence_builder",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var paidCandidate = new SidecarRecommendationCandidateDto(
            Type: "store_offer",
            TargetId: "pack-gold",
            Score: 0.95m,
            Reason: "Hot deal",
            Payload: new Dictionary<string, object> { ["isPaid"] = true });

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ComposedSidecarClient(scores, [paidCandidate]));
        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.Offers.Should().NotContain(o => o.Type == "store_offer",
            "paid offers must be suppressed when player is frustrated");
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_PaidOffer_AllowedWhenPlayerNotFrustrated()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.10m,   // well below threshold
            ConfidenceLevel: 0.70m,
            RecommendedArchetype: "risk_taker",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var paidCandidate = new SidecarRecommendationCandidateDto(
            Type: "store_offer",
            TargetId: "pack-gold",
            Score: 0.85m,
            Reason: "Great deal for competitive players.",
            Payload: new Dictionary<string, object> { ["isPaid"] = true });

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ComposedSidecarClient(scores, [paidCandidate]));
        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.Offers.Should().Contain(o => o.Type == "store_offer",
            "paid offers are allowed when player is not frustrated");
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_OfferIncludesReasonAndGuardrails()
    {
        await using var db = NewDb();
        var candidate = new SidecarRecommendationCandidateDto(
            Type: "store_offer",
            TargetId: null,
            Score: 0.80m,
            Reason: "Personalized store pick",
            Payload: new Dictionary<string, object>());

        var svc = NewService(db, new RecommendationSidecarClient([candidate]));
        var playerId = Guid.NewGuid();

        var result = await svc.GetStoreRecommendationsAsync(playerId);

        result.Offers.Should().HaveCount(1);
        var offer = result.Offers[0];
        offer.Reason.Should().NotBeNullOrEmpty();
        offer.Guardrails.Should().NotBeNull();
        offer.Source.Should().Be("store");
    }

    [Fact]
    public async Task GetStoreRecommendationsAsync_FrustratedPlayer_FreeOfferIsPersisted()
    {
        await using var db = NewDb();
        var scores = new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.10m,
            FrustrationRiskScore: 0.80m,
            ConfidenceLevel: 0.20m,
            RecommendedArchetype: "confidence_builder",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>());

        var mindSvc = new PlayerMindProfileService(db, new ScoringSidecarClient(scores));
        var playerId = Guid.NewGuid();
        await mindSvc.RecordEventAsync(playerId, MakeEvent());
        await mindSvc.RecalculateAsync(playerId);

        var svc = NewService(db, new ScoringSidecarClient(scores));
        await svc.GetStoreRecommendationsAsync(playerId);

        var stored = await db.PersonalizationRecommendations
            .Where(r => r.PlayerId == playerId && r.RecommendationType == "store_free_offer")
            .ToListAsync();

        stored.Should().HaveCount(1, "the free support offer must be persisted for audit purposes");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static PlayerBehaviorEventDto MakeEvent(string eventSource = "ranked") =>
        new("match_completed", eventSource, "math", "medium", eventSource, null, DateTimeOffset.UtcNow);

    // ── Sidecar stubs ─────────────────────────────────────────────────────────

    /// <summary>Returns no candidates and default scores.</summary>
    private sealed class EmptySidecarClient : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(new SidecarPlayerScoresDto(0m, 0m, 0.50m, "new_player",
                new Dictionary<string, decimal>(), new Dictionary<string, decimal>(),
                new Dictionary<string, object>()));

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
                Array.Empty<SidecarRecommendationCandidateDto>());
    }

    /// <summary>Returns a fixed set of scores but no recommendation candidates.</summary>
    private sealed class ScoringSidecarClient(SidecarPlayerScoresDto scores) : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(scores);

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
                Array.Empty<SidecarRecommendationCandidateDto>());
    }

    /// <summary>Returns a fixed list of recommendation candidates but default scores.</summary>
    private sealed class RecommendationSidecarClient(
        IReadOnlyList<SidecarRecommendationCandidateDto> candidates) : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(new SidecarPlayerScoresDto(0m, 0m, 0.50m, "new_player",
                new Dictionary<string, decimal>(), new Dictionary<string, decimal>(),
                new Dictionary<string, object>()));

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult(candidates);
    }

    /// <summary>Returns specific scores AND specific recommendation candidates.</summary>
    private sealed class ComposedSidecarClient(
        SidecarPlayerScoresDto scores,
        IReadOnlyList<SidecarRecommendationCandidateDto> candidates) : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(scores);

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult(candidates);
    }

    /// <summary>Throws on every call to simulate a sidecar outage.</summary>
    private sealed class ThrowingSidecarClient : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            throw new HttpRequestException("Sidecar unavailable");

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            throw new HttpRequestException("Sidecar unavailable");
    }
}
