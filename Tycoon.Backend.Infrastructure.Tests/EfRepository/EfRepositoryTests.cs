using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Infrastructure.Repositories;

namespace Tycoon.Backend.Infrastructure.Tests.EfRepository;

public sealed class EfRepositoryTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public async Task GetAsync_Returns_Entity_WhenFound()
    {
        await using var db = NewDb();
        var mission = new Mission("Daily", "daily_play_3", "Play 3", "Complete 3 matches.", 3, 50);
        db.Missions.Add(mission);
        await db.SaveChangesAsync();

        var repo = new EfRepository<Mission>(db);
        var result = await repo.GetAsync(mission.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(mission.Id);
        result.Key.Should().Be("daily_play_3");
    }

    [Fact]
    public async Task GetAsync_Returns_Null_WhenNotFound()
    {
        await using var db = NewDb();
        var repo = new EfRepository<Mission>(db);

        var result = await repo.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task Add_Persists_Entity_AfterSaveChangesAsync()
    {
        await using var db = NewDb();
        var repo = new EfRepository<Mission>(db);
        var mission = new Mission("Weekly", "weekly_play_25", "Play 25", "Complete 25 matches.", 25, 200);

        repo.Add(mission);
        await db.SaveChangesAsync();

        var saved = await db.Missions.FindAsync(mission.Id);
        saved.Should().NotBeNull();
        saved!.Key.Should().Be("weekly_play_25");
        saved.Goal.Should().Be(25);
    }

    [Fact]
    public async Task Add_Multiple_Entities_AllPersisted()
    {
        await using var db = NewDb();
        var repo = new EfRepository<Mission>(db);

        var m1 = new Mission("Daily", "daily_win_1", "Win 1", "Win a match.", 1, 30);
        var m2 = new Mission("Weekly", "weekly_win_10", "Win 10", "Win 10 matches.", 10, 150);

        repo.Add(m1);
        repo.Add(m2);
        await db.SaveChangesAsync();

        var count = await db.Missions.CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public void Add_Stages_Entity_In_ChangeTracker_BeforeSave()
    {
        using var db = NewDb();
        var repo = new EfRepository<Mission>(db);
        var mission = new Mission("Daily", "daily_win_1", "Win 1", "Win a match.", 1, 30);

        repo.Add(mission);

        db.Entry(mission).State.Should().Be(EntityState.Added);
    }

    [Fact]
    public async Task GetAsync_Returns_CorrectEntity_WhenMultipleExist()
    {
        await using var db = NewDb();
        var m1 = new Mission("Daily", "daily_play_3", "Play 3", "Desc", 3, 50);
        var m2 = new Mission("Weekly", "weekly_play_25", "Play 25", "Desc", 25, 200);
        db.Missions.AddRange(m1, m2);
        await db.SaveChangesAsync();

        var repo = new EfRepository<Mission>(db);
        var result = await repo.GetAsync(m2.Id);

        result.Should().NotBeNull();
        result!.Key.Should().Be("weekly_play_25");
    }
}
