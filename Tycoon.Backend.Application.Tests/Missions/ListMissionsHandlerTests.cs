using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Missions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;

namespace Tycoon.Backend.Application.Tests.Missions;

public sealed class ListMissionsHandlerTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public async Task Handle_Returns_OnlyActiveMissions()
    {
        await using var db = NewDb();

        var active = new Mission("Daily", "daily_play_3", "Play 3", "Desc", 3, 50, active: true);
        var inactive = new Mission("Daily", "daily_inactive", "Inactive", "Desc", 1, 10, active: false);
        inactive.Deactivate();
        db.Missions.AddRange(active, inactive);
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);
        var result = await handler.Handle(new ListMissions(""), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Key.Should().Be("daily_play_3");
    }

    [Fact]
    public async Task Handle_Returns_EmptyList_WhenNoActiveMissions()
    {
        await using var db = NewDb();
        var handler = new ListMissionsHandler(db);

        var result = await handler.Handle(new ListMissions(""), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersBy_Type_WhenProvided()
    {
        await using var db = NewDb();

        var daily = new Mission("Daily", "daily_play_3", "Play 3", "Desc", 3, 50);
        var weekly = new Mission("Weekly", "weekly_win_10", "Win 10", "Desc", 10, 150);
        db.Missions.AddRange(daily, weekly);
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);
        var result = await handler.Handle(new ListMissions("Weekly"), CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Type.Should().Be("Weekly");
        result[0].Key.Should().Be("weekly_win_10");
    }

    [Fact]
    public async Task Handle_Returns_AllActive_WhenTypeFilterIsEmpty()
    {
        await using var db = NewDb();

        db.Missions.AddRange(
            new Mission("Daily", "daily_play_3", "T", "D", 3, 50),
            new Mission("Weekly", "weekly_win_10", "T", "D", 10, 150),
            new Mission("Daily", "daily_win_1", "T", "D", 1, 30));
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);
        var result = await handler.Handle(new ListMissions(""), CancellationToken.None);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_Orders_ByType_ThenKey()
    {
        await using var db = NewDb();

        // Add in a different order to confirm sorting
        db.Missions.AddRange(
            new Mission("Weekly", "weekly_win_10", "T", "D", 10, 150),
            new Mission("Daily", "daily_win_1", "T", "D", 1, 30),
            new Mission("Daily", "daily_play_3", "T", "D", 3, 50));
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);
        var result = await handler.Handle(new ListMissions(""), CancellationToken.None);

        result[0].Key.Should().Be("daily_play_3", "Daily comes before Weekly, then keys are sorted");
        result[1].Key.Should().Be("daily_win_1");
        result[2].Key.Should().Be("weekly_win_10");
    }

    [Fact]
    public async Task Handle_Maps_MissionDto_CorrectFields()
    {
        await using var db = NewDb();

        var mission = new Mission("Daily", "daily_play_3", "Title", "Desc", 3, rewardXp: 75);
        db.Missions.Add(mission);
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);
        var result = await handler.Handle(new ListMissions(""), CancellationToken.None);

        var dto = result[0];
        dto.Id.Should().Be(mission.Id);
        dto.Type.Should().Be("Daily");
        dto.Key.Should().Be("daily_play_3");
        dto.Goal.Should().Be(3);
        dto.RewardXp.Should().Be(75);
    }

    [Fact]
    public async Task Handle_TypeFilter_IsCaseSensitive()
    {
        await using var db = NewDb();

        db.Missions.Add(new Mission("Daily", "daily_play_3", "T", "D", 3, 50));
        await db.SaveChangesAsync();

        var handler = new ListMissionsHandler(db);

        // Exact case match → returns result
        var exact = await handler.Handle(new ListMissions("Daily"), CancellationToken.None);
        exact.Should().HaveCount(1);

        // Different case → returns nothing (LINQ to InMemory is case-sensitive)
        var lower = await handler.Handle(new ListMissions("daily"), CancellationToken.None);
        lower.Should().BeEmpty();
    }
}
