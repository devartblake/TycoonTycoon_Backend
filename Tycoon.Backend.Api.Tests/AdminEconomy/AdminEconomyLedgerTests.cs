using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminEconomy;

public sealed class AdminEconomyLedgerTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminEconomyLedgerTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task ApplyTransaction_IsIdempotent_ByEventId()
    {
        var playerId = Guid.NewGuid();
        var eventId = Guid.NewGuid();

        var req = new CreateEconomyTxnRequest(
            EventId: eventId,
            PlayerId: playerId,
            Kind: "test-award",
            Lines: new[]
            {
                new EconomyLineDto(CurrencyType.Coins, 100),
                new EconomyLineDto(CurrencyType.Xp, 50),
            },
            Note: "first"
        );

        var r1 = await _http.PostAsJsonAsync("/admin/economy/transactions", req);
        r1.EnsureSuccessStatusCode();
        var res1 = await r1.Content.ReadFromJsonAsync<EconomyTxnResultDto>();

        res1!.Status.Should().Be(EconomyTxnStatus.Applied);
        res1.BalanceCoins.Should().Be(100);
        res1.BalanceXp.Should().Be(50);

        // Same event id again => Duplicate, balances unchanged
        var r2 = await _http.PostAsJsonAsync("/admin/economy/transactions", req with { Note = "second" });
        r2.EnsureSuccessStatusCode();
        var res2 = await r2.Content.ReadFromJsonAsync<EconomyTxnResultDto>();

        res2!.Status.Should().Be(EconomyTxnStatus.Duplicate);
        res2.BalanceCoins.Should().Be(100);
        res2.BalanceXp.Should().Be(50);
    }

    [Fact]
    public async Task SpendFails_WhenInsufficientFunds()
    {
        var playerId = Guid.NewGuid();

        // Attempt to spend without prior award
        var spendReq = new CreateEconomyTxnRequest(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            Kind: "test-spend",
            Lines: new[] { new EconomyLineDto(CurrencyType.Coins, -10) },
            Note: "spend"
        );

        var r = await _http.PostAsJsonAsync("/admin/economy/transactions", spendReq);
        r.EnsureSuccessStatusCode();
        var res = await r.Content.ReadFromJsonAsync<EconomyTxnResultDto>();

        res!.Status.Should().Be(EconomyTxnStatus.InsufficientFunds);
        res.BalanceCoins.Should().Be(0);
    }

    [Fact]
    public async Task History_ReturnsTransactions()
    {
        var playerId = Guid.NewGuid();

        // award
        var awardReq = new CreateEconomyTxnRequest(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            Kind: "test-award-history",
            Lines: new[] { new EconomyLineDto(CurrencyType.Coins, 25) },
            Note: null
        );

        var a = await _http.PostAsJsonAsync("/admin/economy/transactions", awardReq);
        a.EnsureSuccessStatusCode();

        var h = await _http.GetAsync($"/admin/economy/history/{playerId}?page=1&pageSize=50");
        h.EnsureSuccessStatusCode();

        var hist = await h.Content.ReadFromJsonAsync<EconomyHistoryDto>();
        hist!.PlayerId.Should().Be(playerId);
        hist.Items.Should().NotBeEmpty();
        hist.Items.Any(x => x.Kind == "test-award-history").Should().BeTrue();
    }
}
