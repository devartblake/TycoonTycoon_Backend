using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.GameEvents;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Entitlements.Entities;

namespace Synaptix.Backend.Application.Tests.GameEvents;

public sealed class ChampionSpectatorServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options;
        return new AppDb(opts, dispatcher: null);
    }

    private static async Task<GameEvent> EventAsync(AppDb db, Guid champion)
    {
        var ev = new GameEvent(GameEvent.ChampionVsTierKind, 1, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, 0, 0, 100);
        ev.SeedChampion(champion);
        ev.Open(DateTimeOffset.UtcNow);
        ev.Start(DateTimeOffset.UtcNow);
        ev.AddToJackpot(300);
        db.GameEvents.Add(ev);
        await db.SaveChangesAsync();
        return ev;
    }

    private static async Task GrantPassAsync(AppDb db, Guid playerId, DateTimeOffset? expiresAt)
    {
        db.PlayerEntitlements.Add(PlayerEntitlement.Grant(
            playerId, ChampionSpectatorService.PremiumSku, "spectator_pass", 1,
            Guid.NewGuid(), scope: expiresAt is null ? "permanent" : "seasonal", expiresAt: expiresAt));
        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task HasPremium_TrueForActive_FalseForNoneOrExpired()
    {
        await using var db = NewDb();
        var svc = new ChampionSpectatorService(db);
        var active = Guid.NewGuid();
        var expired = Guid.NewGuid();
        var none = Guid.NewGuid();
        await GrantPassAsync(db, active, null);
        await GrantPassAsync(db, expired, DateTimeOffset.UtcNow.AddDays(-1));

        (await svc.HasPremiumAsync(active, CancellationToken.None)).Should().BeTrue();
        (await svc.HasPremiumAsync(expired, CancellationToken.None)).Should().BeFalse();
        (await svc.HasPremiumAsync(none, CancellationToken.None)).Should().BeFalse();
        (await svc.HasPremiumAsync(Guid.Empty, CancellationToken.None)).Should().BeFalse();
    }

    [Fact]
    public async Task GetView_FreeViewer_GetsBasicCounts_NoFeed()
    {
        await using var db = NewDb();
        var champ = Guid.NewGuid();
        var ev = await EventAsync(db, champ);
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, champ, Guid.NewGuid()));
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, Guid.NewGuid(), Guid.NewGuid())
        { EliminatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();
        var svc = new ChampionSpectatorService(db);

        var view = await svc.GetViewAsync(ev.Id, Guid.NewGuid(), CancellationToken.None);

        view.Should().NotBeNull();
        view!.IsPremium.Should().BeFalse();
        view.IsLive.Should().BeTrue();
        view.AliveCount.Should().Be(1);
        view.JackpotPool.Should().Be(300);
        view.EliminationFeed.Should().BeEmpty(); // gated behind premium
    }

    [Fact]
    public async Task GetView_PremiumViewer_GetsEliminationCam()
    {
        await using var db = NewDb();
        var champ = Guid.NewGuid();
        var ev = await EventAsync(db, champ);
        var out1 = Guid.NewGuid();
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, champ, Guid.NewGuid()));
        db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, out1, Guid.NewGuid())
        { EliminatedAt = DateTimeOffset.UtcNow });
        await db.SaveChangesAsync();

        var viewer = Guid.NewGuid();
        await GrantPassAsync(db, viewer, DateTimeOffset.UtcNow.AddDays(7));
        var svc = new ChampionSpectatorService(db);

        var view = await svc.GetViewAsync(ev.Id, viewer, CancellationToken.None);

        view!.IsPremium.Should().BeTrue();
        view.EliminationFeed.Should().ContainSingle();
        view.EliminationFeed[0].PlayerId.Should().Be(out1);
        view.EliminationFeed[0].WasChampion.Should().BeFalse();
    }

    [Fact]
    public async Task GrantPass_MakesViewerPremium()
    {
        await using var db = NewDb();
        var svc = new ChampionSpectatorService(db);
        var player = Guid.NewGuid();

        await svc.GrantPassAsync(player, days: 30, CancellationToken.None);

        (await svc.HasPremiumAsync(player, CancellationToken.None)).Should().BeTrue();
    }
}
