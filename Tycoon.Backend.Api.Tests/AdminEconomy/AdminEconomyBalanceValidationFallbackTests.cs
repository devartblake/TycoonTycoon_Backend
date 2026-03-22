using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Config;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminEconomy;

public sealed class AdminEconomyBalanceValidationFallbackTests : IClassFixture<AdminEconomyFallbackFactory>
{
    private readonly HttpClient _admin;

    public AdminEconomyBalanceValidationFallbackTests(AdminEconomyFallbackFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Balance_Patch_Fallback_InvalidOperation_Returns_Errors_Array()
    {
        var req = new UpdateGameBalanceConfigRequest(
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
            Safeguards: null
        );

        var resp = await _admin.PatchAsJsonAsync("/admin/economy/balance", req);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
        await resp.HasErrorDetailArrayContainingAsync("errors", "forced fallback invalid op");
    }
}

public sealed class AdminEconomyFallbackFactory : TycoonApiFactory
{
    protected override void ConfigureWebHost(Microsoft.AspNetCore.Hosting.IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGameBalancePolicyService>();
            services.AddScoped<IGameBalancePolicyService, ThrowingPolicyService>();
        });
    }
}

internal sealed class ThrowingPolicyService : IGameBalancePolicyService
{
    public Task<GameBalanceConfigDto> GetConfigAsync(CancellationToken ct)
        => Task.FromResult(new GameBalanceConfigDto(
            MaxEnergy: 20,
            StartEnergy: 20,
            RegenMinutesPerEnergy: 10,
            DailyFreeEnergy: 5,
            AdEnergyMin: 2,
            AdEnergyMax: 4,
            LevelUpFullRefill: true,
            PremiumEnergyCapBonus: 5,
            PremiumRegenMultiplier: 1.25m,
            Modes: [new ModeBalanceRuleDto("casual", 3, null, false, 0)],
            Safeguards: new SafeguardConfigDto(3, 1, 1, 5, 20, 3, 0.1m),
            UpdatedAtUtc: DateTimeOffset.UtcNow
        ));

    public Task<GameBalanceConfigDto> UpdateConfigAsync(UpdateGameBalanceConfigRequest req, CancellationToken ct)
        => throw new InvalidOperationException("forced fallback invalid op");

    public Task<(int SessionNumber, int Discount)> StartSessionAsync(Guid playerId, CancellationToken ct)
        => Task.FromResult((1, 0));

    public Task<(bool Granted, int RemainingToday)> ClaimDailyTicketAsync(Guid playerId, CancellationToken ct)
        => Task.FromResult((false, 0));

    public Task<int> ReportLossAsync(Guid playerId, CancellationToken ct)
        => Task.FromResult(0);

    public Task ResetLossAsync(Guid playerId, CancellationToken ct) => Task.CompletedTask;

    public Task<ModeEntryDecisionDto> TryEnterModeAsync(Guid playerId, string mode, CancellationToken ct)
        => Task.FromResult(new ModeEntryDecisionDto(true, "OK", "ok", 0, false, 20));
}
