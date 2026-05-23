using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Personalization;

public sealed class PlayerMindProfileServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static PlayerMindProfileService NewService(AppDb db, IPersonalizationSidecarClient? sidecar = null)
        => new(db, sidecar ?? new NullSidecarClient());

    private static PlayerBehaviorEventDto MakeEvent(
        string eventType = "match_completed",
        string eventSource = "ranked",
        string? category = "math",
        DateTimeOffset? occurredAt = null) =>
        new(eventType, eventSource, category, "medium", "ranked", null,
            occurredAt ?? DateTimeOffset.UtcNow);

    // ── GetOrCreateAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrCreate_CreatesDefaultProfile_WhenNoneExists()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var profile = await svc.GetOrCreateAsync(playerId);

        profile.PlayerId.Should().Be(playerId);
        profile.ConfidenceLevel.Should().Be(0.50m);
        profile.Archetype.Should().Be("new_player");
    }

    [Fact]
    public async Task GetOrCreate_ReturnsExistingProfile_WhenAlreadyExists()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var first = await svc.GetOrCreateAsync(playerId);
        var second = await svc.GetOrCreateAsync(playerId);

        second.PlayerId.Should().Be(first.PlayerId);
        db.PlayerMindProfiles.Count(p => p.PlayerId == playerId).Should().Be(1);
    }

    [Fact]
    public async Task GetOrCreate_PersistsProfileToDatabase()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        await svc.GetOrCreateAsync(playerId);

        var stored = await db.PlayerMindProfiles.SingleAsync(p => p.PlayerId == playerId);
        stored.Should().NotBeNull();
    }

    // ── RecordEventAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task RecordEvent_StoresBehaviorEvent()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();
        var evt = MakeEvent();

        await svc.RecordEventAsync(playerId, evt);

        var stored = await db.PlayerBehaviorEvents.SingleAsync(e => e.PlayerId == playerId);
        stored.EventType.Should().Be("match_completed");
        stored.EventSource.Should().Be("ranked");
        stored.Category.Should().Be("math");
    }

    [Fact]
    public async Task RecordEvent_SetsIngestedAt_ToUtcNow()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var before = DateTimeOffset.UtcNow;

        await svc.RecordEventAsync(Guid.NewGuid(), MakeEvent());

        var stored = await db.PlayerBehaviorEvents.FirstAsync();
        stored.IngestedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task RecordEvent_SerializesMetadata_WhenProvided()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var evt = new PlayerBehaviorEventDto(
            "match_completed", "ranked", "science", "hard", "ranked",
            new Dictionary<string, object> { ["score"] = 95 },
            DateTimeOffset.UtcNow);

        await svc.RecordEventAsync(Guid.NewGuid(), evt);

        var stored = await db.PlayerBehaviorEvents.FirstAsync();
        stored.MetadataJson.Should().Contain("score");
    }

    [Fact]
    public async Task RecordEvent_UsesEmptyJson_WhenMetadataIsNull()
    {
        await using var db = NewDb();
        var svc = NewService(db);

        await svc.RecordEventAsync(Guid.NewGuid(), MakeEvent());

        var stored = await db.PlayerBehaviorEvents.FirstAsync();
        stored.MetadataJson.Should().Be("{}");
    }

    // ── RecalculateAsync (local rules) ────────────────────────────────────────

    [Fact]
    public async Task Recalculate_CreatesProfile_WhenNoneExists()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();

        var profile = await svc.RecalculateAsync(playerId);

        profile.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public async Task Recalculate_SetsLastCalculatedAt()
    {
        await using var db = NewDb();
        var svc = NewService(db);
        var playerId = Guid.NewGuid();
        var before = DateTimeOffset.UtcNow;

        var profile = await svc.RecalculateAsync(playerId);

        profile.LastCalculatedAt.Should().NotBeNull();
        profile.LastCalculatedAt!.Value.Should().BeOnOrAfter(before);
    }

    [Fact]
    public async Task Recalculate_LocalRules_RaiseConfidence_WithHighActivity()
    {
        await using var db = NewDb();
        // Use throwing sidecar so local rules are not overridden
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        // Record 20 events (max activity ratio = 1.0 → confidence = 0.90)
        for (var i = 0; i < 20; i++)
            await svc.RecordEventAsync(playerId, MakeEvent());

        var profile = await svc.RecalculateAsync(playerId);

        profile.ConfidenceLevel.Should().BeGreaterThan(0.50m);
    }

    [Fact]
    public async Task Recalculate_LocalRules_LowerChurnRisk_WithHighActivity()
    {
        await using var db = NewDb();
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 15; i++)
            await svc.RecordEventAsync(playerId, MakeEvent());

        var profile = await svc.RecalculateAsync(playerId);

        profile.ChurnRiskScore.Should().BeLessThan(0.50m);
    }

    [Fact]
    public async Task Recalculate_LocalRules_SetArchetype_Competitor_ForRankedEvents()
    {
        await using var db = NewDb();
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            await svc.RecordEventAsync(playerId, MakeEvent(eventSource: "ranked"));

        var profile = await svc.RecalculateAsync(playerId);

        profile.Archetype.Should().Be("competitor");
    }

    [Fact]
    public async Task Recalculate_LocalRules_SetArchetype_Learner_ForPracticeEvents()
    {
        await using var db = NewDb();
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 5; i++)
            await svc.RecordEventAsync(playerId, MakeEvent(eventSource: "practice"));

        var profile = await svc.RecalculateAsync(playerId);

        profile.Archetype.Should().Be("learner");
    }

    [Fact]
    public async Task Recalculate_LocalRules_PopulateCategoryStrengths_FromEvents()
    {
        await using var db = NewDb();
        var svc = NewService(db, new ThrowingSidecarClient());
        var playerId = Guid.NewGuid();

        await svc.RecordEventAsync(playerId, MakeEvent(category: "math"));
        await svc.RecordEventAsync(playerId, MakeEvent(category: "science"));
        await svc.RecordEventAsync(playerId, MakeEvent(category: "math"));

        var profile = await svc.RecalculateAsync(playerId);

        profile.CategoryStrengths.Should().ContainKey("math");
        profile.CategoryStrengths["math"].Should().BeApproximately(0.6667m, 0.001m);
    }

    [Fact]
    public async Task Recalculate_SidecarScores_Override_LocalRules_WhenSidecarEnabled()
    {
        await using var db = NewDb();
        var sidecar = new StubSidecarClient(new SidecarPlayerScoresDto(
            ChurnRiskScore: 0.99m,
            FrustrationRiskScore: 0.88m,
            ConfidenceLevel: 0.11m,
            RecommendedArchetype: "at_risk",
            CategoryStrengths: new Dictionary<string, decimal> { ["math"] = 0.5m },
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>()));
        var svc = NewService(db, sidecar);
        var playerId = Guid.NewGuid();

        await svc.RecordEventAsync(playerId, MakeEvent(eventSource: "ranked"));
        // Ensure sidecar scoring enabled (default = true)
        var profile = await svc.RecalculateAsync(playerId);

        profile.ChurnRiskScore.Should().Be(0.99m);
        profile.ConfidenceLevel.Should().Be(0.11m);
        profile.Archetype.Should().Be("at_risk");
    }

    [Fact]
    public async Task Recalculate_RetainsLocalRules_WhenSidecarThrows()
    {
        await using var db = NewDb();
        var sidecar = new ThrowingSidecarClient();
        var svc = NewService(db, sidecar);
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 20; i++)
            await svc.RecordEventAsync(playerId, MakeEvent(eventSource: "practice"));

        var profile = await svc.RecalculateAsync(playerId);

        // Local rules should have run; archetype from local rules
        profile.Archetype.Should().Be("learner");
        profile.ConfidenceLevel.Should().BeGreaterThan(0.50m);
    }

    // ── Stubs ─────────────────────────────────────────────────────────────────

    private sealed class NullSidecarClient : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(new SidecarPlayerScoresDto(0m, 0m, 0.50m, "new_player",
                new Dictionary<string, decimal>(),
                new Dictionary<string, decimal>(),
                new Dictionary<string, object>()));

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
                Array.Empty<SidecarRecommendationCandidateDto>());

        public Task<SidecarNotificationScoreDto> GetNotificationScoreAsync(
            SidecarNotificationScoreRequest request, CancellationToken ct = default) =>
            Task.FromResult(new SidecarNotificationScoreDto(
                request.CurrentProfile.NotificationFatigueScore, true, 24));
    }

    private sealed class StubSidecarClient(SidecarPlayerScoresDto scores) : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            Task.FromResult(scores);

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
                Array.Empty<SidecarRecommendationCandidateDto>());

        public Task<SidecarNotificationScoreDto> GetNotificationScoreAsync(
            SidecarNotificationScoreRequest request, CancellationToken ct = default) =>
            Task.FromResult(new SidecarNotificationScoreDto(
                request.CurrentProfile.NotificationFatigueScore, true, 24));
    }

    private sealed class ThrowingSidecarClient : IPersonalizationSidecarClient
    {
        public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
            SidecarPlayerScoringRequest request, CancellationToken ct = default) =>
            throw new HttpRequestException("Sidecar unavailable");

        public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
            SidecarRecommendationRequest request, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
                Array.Empty<SidecarRecommendationCandidateDto>());

        public Task<SidecarNotificationScoreDto> GetNotificationScoreAsync(
            SidecarNotificationScoreRequest request, CancellationToken ct = default) =>
            throw new HttpRequestException("Sidecar unavailable");
    }
}
