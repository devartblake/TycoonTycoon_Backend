using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Missions;
using Synaptix.Backend.Application.Rewards;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Missions;

public sealed class ClaimMissionHandlerTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static Mission MakeMission(string type = "Daily", string key = "daily_play_3", int goal = 3) =>
        new(type, key, "Title", "Desc", goal, rewardXp: 50, rewardCoins: 10);

    private static ClaimMissionHandler NewHandlerWithRewards(
        AppDb db,
        Synaptix.Backend.Application.Personalization.IPlayerMindProfileService? mindProfiles = null,
        IReadOnlyList<string>? allowlistedKeys = null)
        => new(
            db,
            mindProfiles,
            new RewardOutcomeService(new FixedRewardRng()),
            Options.Create(new MissionRewardOptions
            {
                ReactorMissionKeys = allowlistedKeys ?? []
            }));

    [Fact]
    public async Task Handle_Returns_NotFound_WhenNoClaim()
    {
        await using var db = NewDb();
        var handler = new ClaimMissionHandler(db);
        var playerId = Guid.NewGuid();
        var missionId = Guid.NewGuid();

        var result = await handler.Handle(new ClaimMission(playerId, missionId, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.NotFound);
    }

    [Fact]
    public async Task Handle_Returns_NotCompleted_WhenClaimExists_ButNotFinished()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(1, mission.Goal); // incomplete
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.NotCompleted);
    }

    [Fact]
    public async Task Handle_Returns_AlreadyClaimed_WhenClaimIsClaimed()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        claim.MarkClaimed();
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.AlreadyClaimed);
    }

    [Fact]
    public async Task Handle_Returns_Claimed_AndPersists_WhenValid()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.Claimed);
        result.MissionType.Should().Be("Daily");
        result.RewardXp.Should().Be(50);
        result.RewardCoins.Should().Be(10);

        // Verify claim is persisted as claimed
        var saved = await db.MissionClaims.SingleAsync(x => x.PlayerId == playerId && x.MissionId == mission.Id);
        saved.Claimed.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_Returns_UpdatedMissionList_AfterClaim()
    {
        await using var db = NewDb();

        var m1 = MakeMission("Daily", "daily_play_3");
        var m2 = MakeMission("Daily", "daily_win_1", goal: 1);
        db.Missions.AddRange(m1, m2);

        var playerId = Guid.NewGuid();
        var claim1 = new MissionClaim(playerId, m1.Id);
        claim1.AddProgress(3, m1.Goal);
        var claim2 = new MissionClaim(playerId, m2.Id);
        claim2.AddProgress(1, m2.Goal);
        db.MissionClaims.AddRange(claim1, claim2);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, m1.Id, ""), CancellationToken.None);

        result.UpdatedMissions.Should().HaveCount(2);
        result.UpdatedMissions.Should().Contain(x => x.MissionId == m1.Id && x.Claimed);
        result.UpdatedMissions.Should().Contain(x => x.MissionId == m2.Id);
    }

    [Fact]
    public async Task Handle_TypeFilter_OnlyReturns_MatchingType_InUpdatedList()
    {
        await using var db = NewDb();

        var daily = MakeMission("Daily", "daily_play_3");
        var weekly = MakeMission("Weekly", "weekly_play_25", goal: 25);
        db.Missions.AddRange(daily, weekly);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, daily.Id);
        claim.AddProgress(3, daily.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, daily.Id, "Daily"), CancellationToken.None);

        result.UpdatedMissions.Should().OnlyContain(x => x.Type == "Daily");
    }

    [Fact]
    public async Task Handle_Returns_NotFound_WhenMissionDefinitionMissing()
    {
        await using var db = NewDb();

        var playerId = Guid.NewGuid();
        var fakeMissionId = Guid.NewGuid();

        // Claim exists but no mission definition
        var claim = new MissionClaim(playerId, fakeMissionId);
        claim.AddProgress(3, 3);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db);
        var result = await handler.Handle(new ClaimMission(playerId, fakeMissionId, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.NotFound);
    }

    // ── Behavior event emission ───────────────────────────────────────────────

    [Fact]
    public async Task Handle_Claimed_RecordsBehaviorEvent_ViaPlayerMindProfileService()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var mindProfiles = new TrackingMindProfileService();
        var handler = new ClaimMissionHandler(db, mindProfiles);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.Claimed);
        mindProfiles.RecordedEvents.Should().ContainSingle(
            e => e.PlayerId == playerId && e.EventType == "mission_completed",
            "a mission_completed event must be recorded when a mission reward is claimed");
    }

    [Fact]
    public async Task Handle_NotClaimed_DoesNotRecord_BehaviorEvent()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(1, mission.Goal); // incomplete
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var mindProfiles = new TrackingMindProfileService();
        var handler = new ClaimMissionHandler(db, mindProfiles);
        await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        mindProfiles.RecordedEvents.Should().BeEmpty("no event should be emitted when the mission is not yet completed");
    }

    [Fact]
    public async Task Handle_Claimed_MindProfileServiceThrows_DoesNotBubbleUp()
    {
        await using var db = NewDb();
        var mission = MakeMission();
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = new ClaimMissionHandler(db, new ThrowingMindProfileService());

        // Should not throw despite service failure
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);
        result.Status.Should().Be(ClaimMissionStatus.Claimed);
    }

    [Fact]
    public async Task Handle_ReactorMission_Claimed_ReturnsReactorPayload()
    {
        await using var db = NewDb();
        var mission = MakeMission(type: "Daily", key: "reactor_daily_score", goal: 3);
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = NewHandlerWithRewards(db, allowlistedKeys: ["reactor_daily_score"]);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.Claimed);
        result.RewardMechanismId.Should().Be("reactor");
        result.ReactorSpinPayload.Should().NotBeNull();
        result.ReactorSpinPayload!.SpinId.Should().StartWith("rr_");
        result.ReactorSpinPayload.ClaimToken.Should().NotBeNullOrWhiteSpace();

        var createdSession = await db.RewardSessions
            .SingleOrDefaultAsync(s => s.PlayerId == playerId && s.ReactorId == "mission-reactor");
        createdSession.Should().NotBeNull();
    }

    [Fact]
    public async Task Handle_ReactorMission_AlreadyClaimed_IsIdempotent_AndDoesNotCreateDuplicateSpin()
    {
        await using var db = NewDb();
        var mission = MakeMission(type: "Daily", key: "reactor_daily_score", goal: 3);
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = NewHandlerWithRewards(db, allowlistedKeys: ["reactor_daily_score"]);
        var first = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);
        var second = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        first.Status.Should().Be(ClaimMissionStatus.Claimed);
        second.Status.Should().Be(ClaimMissionStatus.AlreadyClaimed);
        second.RewardMechanismId.Should().Be("reactor");
        second.ReactorSpinPayload.Should().NotBeNull();
        second.ReactorSpinPayload!.SpinId.Should().Be(first.ReactorSpinPayload!.SpinId);

        var count = await db.RewardSessions.CountAsync(s => s.PlayerId == playerId && s.ReactorId == "mission-reactor");
        count.Should().Be(1);
    }

    [Fact]
    public async Task Handle_MissionNotInAllowlist_UsesDirectMechanism()
    {
        await using var db = NewDb();
        var mission = MakeMission(type: "Daily", key: "reactor_daily_score", goal: 3);
        db.Missions.Add(mission);

        var playerId = Guid.NewGuid();
        var claim = new MissionClaim(playerId, mission.Id);
        claim.AddProgress(3, mission.Goal);
        db.MissionClaims.Add(claim);
        await db.SaveChangesAsync();

        var handler = NewHandlerWithRewards(db, allowlistedKeys: []);
        var result = await handler.Handle(new ClaimMission(playerId, mission.Id, ""), CancellationToken.None);

        result.Status.Should().Be(ClaimMissionStatus.Claimed);
        result.RewardMechanismId.Should().Be("direct");
        result.ReactorSpinPayload.Should().BeNull();

        var count = await db.RewardSessions.CountAsync(s => s.PlayerId == playerId && s.ReactorId == "mission-reactor");
        count.Should().Be(0);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed record RecordedEvent(Guid PlayerId, string EventType);

    private sealed class TrackingMindProfileService : Synaptix.Backend.Application.Personalization.IPlayerMindProfileService
    {
        public List<RecordedEvent> RecordedEvents { get; } = [];

        public Task<Synaptix.Shared.Contracts.Dtos.PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default)
            => throw new NotImplementedException();

        public Task RecordEventAsync(Guid playerId, Synaptix.Shared.Contracts.Dtos.PlayerBehaviorEventDto dto, CancellationToken ct = default)
        {
            RecordedEvents.Add(new RecordedEvent(playerId, dto.EventType));
            return Task.CompletedTask;
        }

        public Task<Synaptix.Shared.Contracts.Dtos.PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default)
            => throw new NotImplementedException();
    }

    private sealed class ThrowingMindProfileService : Synaptix.Backend.Application.Personalization.IPlayerMindProfileService
    {
        public Task<Synaptix.Shared.Contracts.Dtos.PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default)
            => throw new InvalidOperationException("test failure");

        public Task RecordEventAsync(Guid playerId, Synaptix.Shared.Contracts.Dtos.PlayerBehaviorEventDto dto, CancellationToken ct = default)
            => throw new InvalidOperationException("test failure");

        public Task<Synaptix.Shared.Contracts.Dtos.PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default)
            => throw new InvalidOperationException("test failure");
    }

    private sealed class FixedRewardRng : IRewardRng
    {
        public double NextDouble() => 0.01;
    }
}
