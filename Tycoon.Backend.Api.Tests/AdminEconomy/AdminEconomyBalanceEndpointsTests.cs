using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminEconomy;

public sealed class AdminEconomyBalanceEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;

    public AdminEconomyBalanceEndpointsTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    private async Task<GameBalanceConfigDto> GetCurrentBalanceAsync()
    {
        var resp = await _admin.GetAsync("/admin/economy/balance");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var cfg = await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>();
        cfg.Should().NotBeNull();
        return cfg!;
    }

    private async Task RestoreBalanceAsync(GameBalanceConfigDto cfg)
    {
        var restore = new UpdateGameBalanceConfigRequest(
            MaxEnergy: cfg.MaxEnergy,
            StartEnergy: cfg.StartEnergy,
            RegenMinutesPerEnergy: cfg.RegenMinutesPerEnergy,
            DailyFreeEnergy: cfg.DailyFreeEnergy,
            AdEnergyMin: cfg.AdEnergyMin,
            AdEnergyMax: cfg.AdEnergyMax,
            LevelUpFullRefill: cfg.LevelUpFullRefill,
            PremiumEnergyCapBonus: cfg.PremiumEnergyCapBonus,
            PremiumRegenMultiplier: cfg.PremiumRegenMultiplier,
            Modes: cfg.Modes,
            Safeguards: cfg.Safeguards
        );
        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", restore);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Balance_Get_Returns_Current_Config()
    {
        var resp = await _admin.GetAsync("/admin/economy/balance");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var cfg = await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>();
        cfg.Should().NotBeNull();
        cfg!.MaxEnergy.Should().BeGreaterThan(0);
        cfg.Modes.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Balance_Patch_Updates_Config()
    {
        var original = await GetCurrentBalanceAsync();
        try
        {
            var patch = new UpdateGameBalanceConfigRequest(
                MaxEnergy: 23,
                StartEnergy: 21,
                RegenMinutesPerEnergy: null,
                DailyFreeEnergy: null,
                AdEnergyMin: null,
                AdEnergyMax: null,
                LevelUpFullRefill: null,
                PremiumEnergyCapBonus: null,
                PremiumRegenMultiplier: null,
                Modes: null,
                Safeguards: null
            );

            var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", patch);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var cfg = await resp.Content.ReadFromJsonAsync<GameBalanceConfigDto>();
            cfg.Should().NotBeNull();
            cfg!.MaxEnergy.Should().Be(23);
            cfg.StartEnergy.Should().Be(21);
        }
        finally
        {
            await RestoreBalanceAsync(original);
        }
    }

    [Fact]
    public async Task Simulate_Returns_Energy_Projection()
    {
        var original = await GetCurrentBalanceAsync();
        try
        {
            var patched = new UpdateGameBalanceConfigRequest(
                MaxEnergy: 30,
                StartEnergy: 18,
                RegenMinutesPerEnergy: 10,
                DailyFreeEnergy: null,
                AdEnergyMin: null,
                AdEnergyMax: null,
                LevelUpFullRefill: null,
                PremiumEnergyCapBonus: null,
                PremiumRegenMultiplier: null,
                Modes:
                [
                    new ModeBalanceRuleDto("casual", 3, null, false, 0),
                    new ModeBalanceRuleDto("ranked", 4, null, false, 100),
                    new ModeBalanceRuleDto("guardian", 5, 2, false, 150)
                ],
                Safeguards: new SafeguardConfigDto(
                    FirstSessionsReducedCostCount: 3,
                    FirstSessionsEnergyDiscount: 1,
                    DailyFreeJackpotTickets: 1,
                    ReviveBaseGemCost: 5,
                    AlmostWinReviveDiscountPercent: 20,
                    PityLossThreshold: 3,
                    PityDifficultyReductionPercent: 0.10m
                )
            );
            var patchResp = await _admin.PatchAsJsonAsync("/admin/economy/balance", patched);
            patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var req = new EconomySimulationRequest(
                SessionMinutes: 20,    // regen = 2
                SessionNumber: 1,      // discount = 1
                CasualMatches: 2,      // (3-1) * 2 = 4
                RankedMatches: 1,      // (4-1) * 1 = 3
                GuardianMatches: 1     // (5-1) * 1 = 4
            );                         // total spend = 11 => 18 - 11 + 2 = 9

            var resp = await _admin.PostAsJsonAsync("/admin/economy/simulate", req);
            resp.StatusCode.Should().Be(HttpStatusCode.OK);

            var result = await resp.Content.ReadFromJsonAsync<EconomySimulationResponse>();
            result.Should().NotBeNull();
            result!.StartingEnergy.Should().Be(18);
            result.EnergySpent.Should().Be(11);
            result.EnergyRegenerated.Should().Be(2);
            result.EndingEnergy.Should().Be(9);
            result.EstimatedMatchesByMode.Should().Be(4);
            result.EstimatedSessionMinutes.Should().Be(20);
        }
        finally
        {
            await RestoreBalanceAsync(original);
        }
    }

    [Fact]
    public async Task Balance_Patch_Invalid_Config_Returns_Validation_Error()
    {
        var invalid = new UpdateGameBalanceConfigRequest(
            MaxEnergy: 10,
            StartEnergy: 25,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: null,
            AdEnergyMax: null,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes: null,
            Safeguards: null
        );

        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", invalid);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        await resp.HasErrorMessageContainingAsync("StartEnergy cannot exceed MaxEnergy");
        await resp.HasErrorDetailArrayContainingAsync("errors", "StartEnergy cannot exceed MaxEnergy.");
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(5, 2)]
    public async Task Balance_Patch_Invalid_AdRange_Returns_Validation_Error(int adMin, int adMax)
    {
        var invalid = new UpdateGameBalanceConfigRequest(
            MaxEnergy: null,
            StartEnergy: null,
            RegenMinutesPerEnergy: null,
            DailyFreeEnergy: null,
            AdEnergyMin: adMin,
            AdEnergyMax: adMax,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes: null,
            Safeguards: null
        );

        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", invalid);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        await resp.HasErrorMessageContainingAsync("Ad energy range is invalid");
        await resp.HasErrorDetailArrayContainingAsync("errors", "Ad energy range is invalid.");
    }

    [Fact]
    public async Task Balance_Patch_Invalid_Revive_Discount_Returns_Validation_Error()
    {
        var invalid = new UpdateGameBalanceConfigRequest(
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
            Safeguards: new SafeguardConfigDto(
                FirstSessionsReducedCostCount: 3,
                FirstSessionsEnergyDiscount: 1,
                DailyFreeJackpotTickets: 1,
                ReviveBaseGemCost: 5,
                AlmostWinReviveDiscountPercent: 101,
                PityLossThreshold: 3,
                PityDifficultyReductionPercent: 0.1m
            )
        );

        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", invalid);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        await resp.HasErrorMessageContainingAsync("AlmostWinReviveDiscountPercent must be between 0 and 100");
        await resp.HasErrorDetailArrayContainingAsync("errors", "AlmostWinReviveDiscountPercent must be between 0 and 100.");
    }

    [Fact]
    public async Task Balance_Patch_Multiple_Invalid_Fields_Returns_All_Errors()
    {
        var invalid = new UpdateGameBalanceConfigRequest(
            MaxEnergy: 0,
            StartEnergy: -1,
            RegenMinutesPerEnergy: 0,
            DailyFreeEnergy: null,
            AdEnergyMin: 5,
            AdEnergyMax: 2,
            LevelUpFullRefill: null,
            PremiumEnergyCapBonus: null,
            PremiumRegenMultiplier: null,
            Modes:
            [
                new ModeBalanceRuleDto("bad", -5, null, false, 0)
            ],
            Safeguards: null
        );

        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", invalid);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        await resp.HasErrorDetailArrayContainingAsync("errors", "MaxEnergy must be greater than zero.");
        await resp.HasErrorDetailArrayContainingAsync("errors", "StartEnergy cannot be negative.");
        await resp.HasErrorDetailArrayContainingAsync("errors", "RegenMinutesPerEnergy must be greater than zero.");
        await resp.HasErrorDetailArrayContainingAsync("errors", "Ad energy range is invalid.");
        await resp.HasErrorDetailArrayContainingAsync("errors", "Mode energyCost cannot be negative.");
    }

    [Fact]
    public async Task Simulate_Returns_Validation_Error_When_Required_Modes_Missing()
    {
        var original = await GetCurrentBalanceAsync();
        try
        {
            var patched = new UpdateGameBalanceConfigRequest(
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
                    new ModeBalanceRuleDto("casual", 3, null, false, 0)
                ],
                Safeguards: null
            );
            var patchResp = await _admin.PatchAsJsonAsync("/admin/economy/balance", patched);
            patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var simResp = await _admin.PostAsJsonAsync("/admin/economy/simulate", new EconomySimulationRequest(
                SessionMinutes: 10,
                SessionNumber: 1,
                CasualMatches: 1,
                RankedMatches: 1,
                GuardianMatches: 1
            ));
            simResp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            await simResp.HasErrorCodeAsync("VALIDATION_ERROR");
            await simResp.HasErrorDetailArrayContainingAsync("errors", "Simulation requires casual, ranked, and guardian mode rules.");
        }
        finally
        {
            await RestoreBalanceAsync(original);
        }
    }
}
