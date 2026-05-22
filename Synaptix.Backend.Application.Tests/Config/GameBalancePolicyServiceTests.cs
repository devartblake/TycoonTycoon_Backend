using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Config;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Tests.Config;

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

    [Fact]
    public async Task TryEnterMode_Does_Not_Reset_Energy_After_Depletion()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        for (var i = 0; i < 4; i++)
        {
            var allowed = await svc.TryEnterModeAsync(playerId, "guardian", CancellationToken.None);
            allowed.Allowed.Should().BeTrue();
        }

        var blocked = await svc.TryEnterModeAsync(playerId, "guardian", CancellationToken.None);
        blocked.Allowed.Should().BeFalse();
        blocked.CurrentEnergy.Should().Be(0);

        var blockedAgain = await svc.TryEnterModeAsync(playerId, "guardian", CancellationToken.None);
        blockedAgain.Allowed.Should().BeFalse();
        blockedAgain.CurrentEnergy.Should().Be(0);
    }

    [Fact]
    public async Task TryEnterMode_Applies_FirstSession_Discount_On_Third_Session()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        await svc.StartSessionAsync(playerId, CancellationToken.None); // 1
        await svc.StartSessionAsync(playerId, CancellationToken.None); // 2
        await svc.StartSessionAsync(playerId, CancellationToken.None); // 3

        var result = await svc.TryEnterModeAsync(playerId, "casual", CancellationToken.None);
        result.Allowed.Should().BeTrue();
        result.EnergyCostApplied.Should().Be(2); // base 3 - discount 1
    }

    [Fact]
    public async Task TryEnterMode_When_Mode_Requires_Ticket_And_Energy_Insufficient_Does_Not_Consume_Ticket()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);
        var playerId = Guid.NewGuid();

        await svc.UpdateConfigAsync(new UpdateGameBalanceConfigRequest(
            MaxEnergy: 5,
            StartEnergy: 5,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes:
            [
                new ModeBalanceRuleDto("ticketed", 6, null, true, 0)
            ],
            Safeguards: null
        ), CancellationToken.None);

        var blocked = await svc.TryEnterModeAsync(playerId, "ticketed", CancellationToken.None);
        blocked.Allowed.Should().BeFalse();
        blocked.ReasonCode.Should().Be("INSUFFICIENT_ENERGY");

        var claimAfterBlock = await svc.ClaimDailyTicketAsync(playerId, CancellationToken.None);
        claimAfterBlock.Granted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateConfig_Rejects_StartEnergy_Above_MaxEnergy()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);

        var act = async () => await svc.UpdateConfigAsync(new UpdateGameBalanceConfigRequest(
            MaxEnergy: 10,
            StartEnergy: 20,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes: null,
            Safeguards: null
        ), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*StartEnergy cannot exceed MaxEnergy*");
    }

    [Fact]
    public async Task UpdateConfig_Rejects_Negative_Mode_EnergyCost()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);

        var act = async () => await svc.UpdateConfigAsync(new UpdateGameBalanceConfigRequest(
            MaxEnergy: null,
            StartEnergy: null,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes:
            [
                new ModeBalanceRuleDto("casual", -1, null, false, 0)
            ],
            Safeguards: null
        ), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Mode energyCost cannot be negative*");
    }

    [Fact]
    public async Task UpdateConfig_Rejects_Safeguard_Discount_Percent_Above_100()
    {
        await using var db = NewDb();
        var svc = new GameBalancePolicyService(db);

        var invalidSafeguards = new SafeguardConfigDto(
            FirstSessionsReducedCostCount: 3,
            FirstSessionsEnergyDiscount: 1,
            DailyFreeJackpotTickets: 1,
            ReviveBaseGemCost: 5,
            AlmostWinReviveDiscountPercent: 120,
            PityLossThreshold: 3,
            PityDifficultyReductionPercent: 0.10m
        );

        var act = async () => await svc.UpdateConfigAsync(new UpdateGameBalanceConfigRequest(
            MaxEnergy: null,
            StartEnergy: null,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes: null,
            Safeguards: invalidSafeguards
        ), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*AlmostWinReviveDiscountPercent must be between 0 and 100*");
    }
}
