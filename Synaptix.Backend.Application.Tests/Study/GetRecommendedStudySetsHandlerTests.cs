using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Analytics.Models;
using Synaptix.Backend.Application.Personalization;
using Synaptix.Backend.Application.Study;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Study;

public sealed class GetRecommendedStudySetsHandlerTests
{
    // Each test gets its own isolated in-memory database.
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static GetRecommendedStudySetsHandler NewHandler(
        AppDb db, IPlayerMindProfileService? profiles = null) =>
        new(db, profiles);

    private static void SeedQuestion(AppDb db, string text, string category)
    {
        var q = new Question(text, category, QuestionDifficulty.Easy, "A", mediaKey: null);
        q.ReplaceOptions(new[]
        {
            new QuestionOption(q.Id, "A", "Correct"),
            new QuestionOption(q.Id, "B", "Wrong")
        });
        q.SetStatus("Approved");
        db.Questions.Add(q);
        db.SaveChanges();
    }

    // ── Baseline — no personalization ─────────────────────────────────────────

    [Fact]
    public async Task Handle_ReturnsCategories_WithoutPersonalization()
    {
        await using var db = NewDb();
        SeedQuestion(db, "Q1", "Science");
        SeedQuestion(db, "Q2", "History");

        var handler = NewHandler(db);
        var result = await handler.Handle(
            new GetRecommendedStudySets(PlayerId: null, Count: 10),
            CancellationToken.None);

        result.Items.Should().Contain(x => x.Category == "Science");
        result.Items.Should().Contain(x => x.Category == "History");
    }

    // ── Profile-backed weak area ──────────────────────────────────────────────

    [Fact]
    public async Task Handle_AddsProfileWeakArea_WhenNoRollupData()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        SeedQuestion(db, "Geo Q1", "Geography");

        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: new Dictionary<string, decimal> { ["Geography"] = 0.8m },
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedStudySets(playerId, Count: 10),
            CancellationToken.None);

        result.Items.Should().Contain(
            x => x.Kind == StudySetKinds.WeakArea && x.Category == "Geography",
            "profile-backed weak area must be included");
    }

    [Fact]
    public async Task Handle_RollupWeakArea_PrecedesProfileWeakArea_WhenBothPresent()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        SeedQuestion(db, "History Q", "History");
        SeedQuestion(db, "Math Q", "Math");

        // Rollup data identifies History as the weak area.
        db.QuestionAnsweredPlayerDailyRollups.Add(new QuestionAnsweredPlayerDailyRollup
        {
            Id = $"rollup-{playerId}",
            Day = DateOnly.FromDateTime(DateTime.UtcNow),
            PlayerId = playerId,
            Mode = "study",
            Category = "History",
            Difficulty = (int)QuestionDifficulty.Easy,
            TotalAnswers = 10,
            CorrectAnswers = 1,
            WrongAnswers = 9
        });
        await db.SaveChangesAsync();

        // Profile identifies Math as the top weakness.
        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: new Dictionary<string, decimal> { ["Math"] = 0.75m, ["History"] = 0.5m },
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedStudySets(playerId, Count: 10),
            CancellationToken.None);

        var itemList = result.Items.ToList();
        var historyIdx = itemList.FindIndex(x => x.Kind == StudySetKinds.WeakArea && x.Category == "History");
        var mathIdx    = itemList.FindIndex(x => x.Kind == StudySetKinds.WeakArea && x.Category == "Math");

        historyIdx.Should().BeGreaterOrEqualTo(0, "rollup weak area must be present");
        mathIdx.Should().BeGreaterOrEqualTo(0, "profile weak area must be present");
        historyIdx.Should().BeLessThan(mathIdx, "rollup-based weak area (History) must precede profile-based one (Math)");
    }

    [Fact]
    public async Task Handle_DoesNotAddDuplicate_WhenProfileAndRollupAgree()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        SeedQuestion(db, "History Q", "History");

        db.QuestionAnsweredPlayerDailyRollups.Add(new QuestionAnsweredPlayerDailyRollup
        {
            Id = $"rollup-dup-{playerId}",
            Day = DateOnly.FromDateTime(DateTime.UtcNow),
            PlayerId = playerId,
            Mode = "study",
            Category = "History",
            Difficulty = (int)QuestionDifficulty.Easy,
            TotalAnswers = 10,
            CorrectAnswers = 2,
            WrongAnswers = 8
        });
        await db.SaveChangesAsync();

        // Profile also says History is the top weak category.
        var profiles = new FixedProfileService(new PlayerMindProfileDto(
            playerId, 0.50m, 0.50m, "balanced", "mixed", "balanced", "solo",
            0m, 0m, 0.50m, 0.50m, 0m, "new_player",
            CategoryStrengths: [],
            CategoryWeaknesses: new Dictionary<string, decimal> { ["History"] = 0.8m },
            Preferences: [],
            Guardrails: [],
            PersonalizationEnabled: true,
            SidecarScoringEnabled: false,
            LastCalculatedAt: null));

        var handler = NewHandler(db, profiles);
        var result = await handler.Handle(
            new GetRecommendedStudySets(playerId, Count: 10),
            CancellationToken.None);

        // Should have exactly one WeakArea set for History (no duplicate).
        result.Items.Count(x => x.Kind == StudySetKinds.WeakArea && x.Category == "History")
            .Should().Be(1, "profile weak area must not duplicate the rollup weak area");
    }

    [Fact]
    public async Task Handle_DoesNotFail_WhenProfileServiceThrows()
    {
        await using var db = NewDb();
        var playerId = Guid.NewGuid();
        SeedQuestion(db, "Science Q", "Science");

        var handler = NewHandler(db, new ThrowingProfileService());
        var act = async () => await handler.Handle(
            new GetRecommendedStudySets(playerId, Count: 10),
            CancellationToken.None);

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
