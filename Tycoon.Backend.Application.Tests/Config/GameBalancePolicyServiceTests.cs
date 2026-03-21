using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Config;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Tests.Config;

public sealed class GameBalancePolicyServiceTests
{
    private static AppDb NewDb()
    {
        var opts = new DbContextOptionsBuilder<AppDb>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new AppDb(opts, dispatcher: null);
    }

    [Fact]
    public async Task GetConfig_Creates_Default_WhenMissing()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);

        var cfg = await svc.GetConfigAsync(CancellationToken.None);

        cfg.MaxEnergy.Should().Be(20);
        cfg.RegenMinutesPerEnergy.Should().Be(10);
        cfg.Modes.Should().Contain(x => x.Mode == "casual" && x.EnergyCost == 3);
    }

    [Fact]
    public async Task UpdateConfig_Overrides_Selected_Fields()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);

        var updated = await svc.UpdateConfigAsync(new UpdateGameBalanceConfigRequest(
            MaxEnergy: 25,
            StartEnergy: null,
            RegenMinutesPerEnergy: 8,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes: null,
            Safeguards: null
        ), CancellationToken.None);

        updated.MaxEnergy.Should().Be(25);
        updated.RegenMinutesPerEnergy.Should().Be(8);
        updated.StartEnergy.Should().Be(20);
    }

    [Fact]
    public async Task StartSession_Applies_Discount_For_First_Three_Sessions()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        var s1 = await svc.StartSessionAsync(playerId, CancellationToken.None);
        var s2 = await svc.StartSessionAsync(playerId, CancellationToken.None);
        var s3 = await svc.StartSessionAsync(playerId, CancellationToken.None);
        var s4 = await svc.StartSessionAsync(playerId, CancellationToken.None);

        s1.Discount.Should().Be(1);
        s2.Discount.Should().Be(1);
        s3.Discount.Should().Be(1);
        s4.Discount.Should().Be(0);
    }

    [Fact]
    public async Task ClaimDailyTicket_Allows_Only_Daily_Limit()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        var first = await svc.ClaimDailyTicketAsync(playerId, CancellationToken.None);
        var second = await svc.ClaimDailyTicketAsync(playerId, CancellationToken.None);

        first.Granted.Should().BeTrue();
        second.Granted.Should().BeFalse();
    }

    [Fact]
    public async Task ReportLoss_And_ResetLoss_Works()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        var one = await svc.ReportLossAsync(playerId, CancellationToken.None);
        var two = await svc.ReportLossAsync(playerId, CancellationToken.None);
        await svc.ResetLossAsync(playerId, CancellationToken.None);
        var afterReset = await svc.ReportLossAsync(playerId, CancellationToken.None);

        one.Should().Be(1);
        two.Should().Be(2);
        afterReset.Should().Be(1);
    }

    [Fact]
    public async Task TryEnterMode_Jackpot_Consumes_One_Daily_Ticket()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        var first = await svc.TryEnterModeAsync(playerId, "jackpot", CancellationToken.None);
        var second = await svc.TryEnterModeAsync(playerId, "jackpot", CancellationToken.None);

        first.Allowed.Should().BeTrue();
        first.TicketConsumed.Should().BeTrue();
        second.Allowed.Should().BeFalse();
        second.ReasonCode.Should().Be("NO_TICKET");
    }

    [Fact]
    public async Task TryEnterMode_Blocks_When_Energy_Insufficient()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        // Drain energy via guardian (cost 5, start 20 => 4 entries)
        for (var i = 0; i < 4; i++)
        {
            var allowed = await svc.TryEnterModeAsync(playerId, "guardian", CancellationToken.None);
            allowed.Allowed.Should().BeTrue();
        }

        var blocked = await svc.TryEnterModeAsync(playerId, "guardian", CancellationToken.None);
        blocked.Allowed.Should().BeFalse();
        blocked.ReasonCode.Should().Be("INSUFFICIENT_ENERGY");
    }
}
