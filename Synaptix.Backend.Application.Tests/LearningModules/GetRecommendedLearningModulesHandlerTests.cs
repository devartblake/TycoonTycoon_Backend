using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.LearningModules;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.LearningModules;

public sealed class GetRecommendedLearningModulesHandlerTests
{
    // Each test gets its own isolated in-memory database.
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static GetRecommendedLearningModulesHandler NewHandler(
        AppDb db, IPlayerMindProfileService? profiles = null) =>
        new(db, profiles);

    private static LearningModule SeedModule(
        AppDb db, string title, string category, QuestionDifficulty difficulty)
    {
        var m = new LearningModule(title, $"{title} description", category, difficulty,
            rewardXp: 100, rewardCoins: 10);
        m.Publish();
        db.LearningModules.Add(m);
        db.SaveChanges();
        return m;
    }

    // ── No personalization ────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsModules_OrderedByDifficultyThenTitle_WhenNoProfile()
    {
        await using var db = NewDb();
        var hard = SeedModule(db, "Hard Module", "Science", QuestionDifficulty.Hard);
        var easy = SeedModule(db, "Easy Module", "Science", QuestionDifficulty.Easy);

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new GetRecommendedLearningModules(PlayerId: null, Count: 10),
            CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.Items[0].Id.Should().Be(easy.Id, "easy module sorts before hard");
        result.Items[1].Id.Should().Be(hard.Id);
    }

    [Fact]
    public async Task Handle_ExcludesCompletedModules_WhenPlayerIdProvided()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        var completed = SeedModule(db, "Done", "Science", QuestionDifficulty.Easy);
        var next = SeedModule(db, "Next", "Science", QuestionDifficulty.Easy);

        db.ModuleCompletions.Add(new ModuleCompletion(playerId, completed.Id));
        await db.SaveChangesAsync();

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new GetRecommendedLearningModules(playerId, Count: 10),
            CancellationToken.None);

        result.Items.Should().ContainSingle(x => x.Id == next.Id);
        result.Items.Should().NotContain(x => x.Id == completed.Id);
    }

    // ── Personalization: weak category prioritisation ─────────────────────────

    [Fact]
    public async Task Handle_PrioritisesWeakCategories_BeforeOtherCategories()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();

        // Weak category module has harder difficulty so it would normally sort last.
        var weakCatModule = SeedModule(db, "Geo Hard", "Geography", QuestionDifficulty.Hard);
        var otherModule   = SeedModule(db, "Music Easy", "Music", QuestionDifficulty.Easy);

        // Stub that returns a profile with Geography as a strong weakness.
        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: new Dictionary<string, decimal> { ["Geography"] = 0.85m },
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedLearningModules(playerId, Count: 10),
            CancellationToken.None);

        var ids = result.Items.Select(x => x.Id).ToList();
        ids.Should().Contain(weakCatModule.Id);
        ids.Should().Contain(otherModule.Id);

        ids.IndexOf(weakCatModule.Id).Should().BeLessThan(
            ids.IndexOf(otherModule.Id),
            "weak-category module must be surfaced before other modules");
    }

    [Fact]
    public async Task Handle_FillsRemainingSlots_WithNonWeakCategoryModules()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();

        var weakMod1 = SeedModule(db, "Weak 1", "Geography", QuestionDifficulty.Easy);
        var weakMod2 = SeedModule(db, "Weak 2", "Geography", QuestionDifficulty.Medium);
        var other1   = SeedModule(db, "Other 1", "Music", QuestionDifficulty.Easy);
        var other2   = SeedModule(db, "Other 2", "Art", QuestionDifficulty.Easy);

        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: new Dictionary<string, decimal> { ["Geography"] = 0.90m },
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedLearningModules(playerId, Count: 10),
            CancellationToken.None);

        var ids = result.Items.Select(x => x.Id).ToList();
        // Both weak-category modules come first.
        ids.IndexOf(weakMod1.Id).Should().BeLessThan(ids.IndexOf(other1.Id));
        ids.IndexOf(weakMod2.Id).Should().BeLessThan(ids.IndexOf(other1.Id));
        // Other category modules are also returned.
        ids.Should().Contain(other1.Id);
        ids.Should().Contain(other2.Id);
    }

    [Fact]
    public async Task Handle_FallsBackToDefaultOrdering_WhenProfileHasNoWeaknesses()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();

        var hard = SeedModule(db, "Hard", "Science", QuestionDifficulty.Hard);
        var easy = SeedModule(db, "Easy", "Science", QuestionDifficulty.Easy);

        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: [],   // empty — no weak categories
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedLearningModules(playerId, Count: 10),
            CancellationToken.None);

        result.Items[0].Id.Should().Be(easy.Id, "default ordering by difficulty applies");
        result.Items[1].Id.Should().Be(hard.Id);
    }

    [Fact]
    public async Task Handle_DoesNotFail_WhenProfileServiceThrows()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        SeedModule(db, "Module A", "Science", QuestionDifficulty.Easy);

        var handler = NewHandler(db, new ThrowingProfileService());
        var act = async () => await handler.Handle(
            new GetRecommendedLearningModules(playerId, Count: 10),
            CancellationToken.None);

        // Personalization failure must never propagate to the caller.
        await act.Should().NotThrowAsync();
    }

    // ── Test doubles ─────────────────────────────────────────────────────────

    private sealed class FixedProfileService(PlayerMindProfileDto profile)
        : IPlayerMindProfileService
    {
        public Task<PlayerMindProfileDto> GetOrCreateAsync(
            Guid playerId, CancellationToken ct = default) =>
            Task.FromResult(profile);

        public Task RecordEventAsync(
            Guid playerId, PlayerBehaviorEventDto behaviorEvent, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task<PlayerMindProfileDto> RecalculateAsync(
            Guid playerId, CancellationToken ct = default) =>
            Task.FromResult(profile);

        public Task<PlayerMindProfileDto> SetPersonalizationEnabledAsync(
            Guid playerId, bool enabled, CancellationToken ct = default) =>
            Task.FromResult(profile);
    }

    private sealed class ThrowingProfileService : IPlayerMindProfileService
    {
        public Task<PlayerMindProfileDto> GetOrCreateAsync(
            Guid playerId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Profile service unavailable");

        public Task RecordEventAsync(
            Guid playerId, PlayerBehaviorEventDto behaviorEvent, CancellationToken ct = default) =>
            throw new InvalidOperationException("Profile service unavailable");

        public Task<PlayerMindProfileDto> RecalculateAsync(
            Guid playerId, CancellationToken ct = default) =>
            throw new InvalidOperationException("Profile service unavailable");

        public Task<PlayerMindProfileDto> SetPersonalizationEnabledAsync(
            Guid playerId, bool enabled, CancellationToken ct = default) =>
            throw new InvalidOperationException("Profile service unavailable");
    }
}
