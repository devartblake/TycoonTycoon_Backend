using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Application.Matches;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Application.Tests.Matches;

public sealed class StartMatchHandlerTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public async Task Handle_Creates_NewMatch_ForHost()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));
        var hostId = Guid.NewGuid();

        var result = await handler.Handle(new StartMatch(hostId, "solo"), CancellationToken.None);

        result.MatchId.Should().NotBeEmpty();
        result.StartedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task Handle_Persists_Match_ToDatabase()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));
        var hostId = Guid.NewGuid();

        var result = await handler.Handle(new StartMatch(hostId, "ranked"), CancellationToken.None);

        var saved = await db.Matches.FindAsync(result.MatchId);
        saved.Should().NotBeNull();
        saved!.HostPlayerId.Should().Be(hostId);
        saved.Mode.Should().Be("ranked");
    }

    [Fact]
    public async Task Handle_DefaultsMode_ToSolo_WhenEmpty()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));

        var result = await handler.Handle(new StartMatch(Guid.NewGuid(), ""), CancellationToken.None);

        var saved = await db.Matches.FindAsync(result.MatchId);
        saved!.Mode.Should().Be("solo");
    }

    [Fact]
    public async Task Handle_DefaultsMode_ToSolo_WhenWhitespace()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));

        var result = await handler.Handle(new StartMatch(Guid.NewGuid(), "   "), CancellationToken.None);

        var saved = await db.Matches.FindAsync(result.MatchId);
        saved!.Mode.Should().Be("solo");
    }

    [Fact]
    public async Task Handle_DifferentHosts_GetSeparateMatches()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));

        var r1 = await handler.Handle(new StartMatch(Guid.NewGuid(), "solo"), CancellationToken.None);
        var r2 = await handler.Handle(new StartMatch(Guid.NewGuid(), "solo"), CancellationToken.None);

        r1.MatchId.Should().NotBe(r2.MatchId);
        (await db.Matches.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Handle_Returns_Existing_Active_Match_ForSameHost()
    {
        await using var db = NewDb();
        var handler = new StartMatchHandler(db, new GameBalancePolicyService(db));
        var host = Guid.NewGuid();

        var first = await handler.Handle(new StartMatch(host, "guardian"), CancellationToken.None);
        var second = await handler.Handle(new StartMatch(host, "guardian"), CancellationToken.None);

        second.MatchId.Should().Be(first.MatchId);
    }
}
