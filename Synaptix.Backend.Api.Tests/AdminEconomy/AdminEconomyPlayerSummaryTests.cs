using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.AdminEconomy;

// Covers the operator economy-dashboard routes added for #421:
// GET /admin/economy/players/{id} (per-player summary) and GET /admin/economy/stats (aggregate).
public sealed class AdminEconomyPlayerSummaryTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;

    public AdminEconomyPlayerSummaryTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    private async Task PostCoinsAsync(Guid playerId, int delta, string kind)
    {
        var req = new CreateEconomyTxnRequest(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            Kind: kind,
            Lines: new[] { new EconomyLineDto(CurrencyType.Coins, delta) });

        var resp = await _admin.PostAsJsonAsync("/admin/economy/transactions", req);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<EconomyTxnResultDto>(TestJson.Default);
        result!.Status.Should().Be(EconomyTxnStatus.Applied);
    }

    [Fact]
    public async Task PlayerSummary_ReflectsBalanceEarnedAndSpent()
    {
        var playerId = Guid.NewGuid();
        await PostCoinsAsync(playerId, 100, "test-earn");
        await PostCoinsAsync(playerId, -30, "test-spend");

        var dto = await _admin.GetFromJsonAsync<AdminPlayerEconomyDto>($"/admin/economy/players/{playerId}");

        dto.Should().NotBeNull();
        dto!.PlayerId.Should().Be(playerId);
        dto.CurrentBalance.Should().Be(70);
        dto.TotalEarned.Should().Be(100);
        dto.TotalSpent.Should().Be(30);
        dto.LastTransactionAt.Should().NotBeNull();
    }

    [Fact]
    public async Task PlayerSummary_UnknownPlayer_Returns404()
    {
        var resp = await _admin.GetAsync($"/admin/economy/players/{Guid.NewGuid()}");
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stats_ReflectNewWalletDelta()
    {
        var before = await _admin.GetFromJsonAsync<AdminEconomyStatsDto>("/admin/economy/stats");
        before.Should().NotBeNull();

        var playerId = Guid.NewGuid();
        await PostCoinsAsync(playerId, 500, "test-stats");

        var after = await _admin.GetFromJsonAsync<AdminEconomyStatsDto>("/admin/economy/stats");
        after.Should().NotBeNull();

        after!.TotalPlayers.Should().Be(before!.TotalPlayers + 1);
        after.TotalCurrency.Should().Be(before.TotalCurrency + 500);
        after.LargestBalance.Should().BeGreaterThanOrEqualTo(500);
        after.AverageBalance.Should().BeInRange(after.SmallestBalance, after.LargestBalance);
    }
}
