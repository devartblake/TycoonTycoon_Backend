using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Matches;

public sealed class MatchEntryPolicyTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;
    private readonly HttpClient _admin;

    public MatchEntryPolicyTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
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
    public async Task Start_PracticeMode_Allows_Legacy_Mode()
    {
        var playerId = Guid.NewGuid();
        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(playerId, "practice"));
        start.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Start_Jackpot_Returns_Conflict_When_No_Ticket_Available()
    {
        var playerId = Guid.NewGuid();

        // Consume today's free jackpot ticket through mobile economy flow.
        var claim = await _http.PostAsync($"/mobile/economy/daily-jackpot-ticket/claim?playerId={playerId}", null);
        claim.StatusCode.Should().Be(HttpStatusCode.OK);

        // Starting jackpot now should fail because ticket limit is exhausted.
        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(playerId, "jackpot"));
        start.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await start.HasErrorCodeAsync("MATCH_ENTRY_DENIED");
        await start.HasErrorDetailAsync("reasonCode", "NO_TICKET");
        await start.HasErrorDetailAsync("mode", "jackpot");
    }

    [Fact]
    public async Task MobileStart_Jackpot_Returns_ReasonCode_When_No_Ticket_Available()
    {
        var playerId = Guid.NewGuid();

        var claim = await _http.PostAsync($"/mobile/economy/daily-jackpot-ticket/claim?playerId={playerId}", null);
        claim.StatusCode.Should().Be(HttpStatusCode.OK);

        var start = await _http.PostAsJsonAsync("/mobile/matches/start", new StartMatchRequest(playerId, "jackpot"));
        start.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await start.HasErrorCodeAsync("MATCH_ENTRY_DENIED");
        await start.HasErrorDetailAsync("reasonCode", "NO_TICKET");
        await start.HasErrorDetailAsync("mode", "jackpot");
    }

    [Fact]
    public async Task Start_Returns_ReasonCode_When_Energy_Is_Insufficient()
    {
        var original = await GetCurrentBalanceAsync();
        try
        {
            var patch = new UpdateGameBalanceConfigRequest(
                MaxEnergy: null,
                StartEnergy: 0,
                RegenMinutesPerEnergy: null,
                DailyFreeEnergy: null,
                AdEnergyMin: null,
                AdEnergyMax: null,
                LevelUpFullRefill: null,
                PremiumEnergyCapBonus: null,
                PremiumRegenMultiplier: null,
                Modes:
                [
                    new ModeBalanceRuleDto("energy_only", 3, null, false, 0)
                ],
                Safeguards: null
            );
            var patchResp = await _admin.PatchAsJsonAsync("/admin/economy/balance", patch);
            patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var playerId = Guid.NewGuid();
            var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(playerId, "energy_only"));
            start.StatusCode.Should().Be(HttpStatusCode.Conflict);
            await start.HasErrorCodeAsync("MATCH_ENTRY_DENIED");
            await start.HasErrorDetailAsync("reasonCode", "INSUFFICIENT_ENERGY");
            await start.HasErrorDetailAsync("mode", "energy_only");
        }
        finally
        {
            await RestoreBalanceAsync(original);
        }
    }

    [Fact]
    public async Task MobileStart_Returns_ReasonCode_When_Energy_Is_Insufficient()
    {
        var original = await GetCurrentBalanceAsync();
        try
        {
            var patch = new UpdateGameBalanceConfigRequest(
                MaxEnergy: null,
                StartEnergy: 0,
                RegenMinutesPerEnergy: null,
                DailyFreeEnergy: null,
                AdEnergyMin: null,
                AdEnergyMax: null,
                LevelUpFullRefill: null,
                PremiumEnergyCapBonus: null,
                PremiumRegenMultiplier: null,
                Modes:
                [
                    new ModeBalanceRuleDto("energy_only_mobile", 3, null, false, 0)
                ],
                Safeguards: null
            );
            var patchResp = await _admin.PatchAsJsonAsync("/admin/economy/balance", patch);
            patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

            var playerId = Guid.NewGuid();
            var start = await _http.PostAsJsonAsync("/mobile/matches/start", new StartMatchRequest(playerId, "energy_only_mobile"));
            start.StatusCode.Should().Be(HttpStatusCode.Conflict);
            await start.HasErrorCodeAsync("MATCH_ENTRY_DENIED");
            await start.HasErrorDetailAsync("reasonCode", "INSUFFICIENT_ENERGY");
            await start.HasErrorDetailAsync("mode", "energy_only_mobile");
        }
        finally
        {
            await RestoreBalanceAsync(original);
        }
    }
}
