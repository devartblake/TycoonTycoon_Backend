using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Features.Crypto;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Crypto;

public sealed class CryptoPrizePoolAndStakingTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public CryptoPrizePoolAndStakingTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task FundAndDistributePrizePool_UpdatesPoolAndPlayerBalance()
    {
        var playerId = await SignupAndAuthorizeAsync(_http, "prize");
        await SeedCryptoUnitsAsync(playerId, 100);

        var fundResp = await _http.PostAsJsonAsync("/crypto/prize-pool/fund",
            new CryptoEconomyEndpoints.CryptoPrizePoolFundRequest(playerId, 40, "alpha"));
        fundResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var poolResp = await _http.GetAsync("/crypto/prize-pool/alpha");
        poolResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var pool = await poolResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.CryptoPrizePoolBalanceResponse>();
        pool.Should().NotBeNull();
        pool!.Units.Should().Be(40);

        _http.WithAdminOpsKey();
        var payoutResp = await _http.PostAsJsonAsync("/crypto/prize-pool/distribute",
            new CryptoEconomyEndpoints.CryptoPrizePoolDistributeRequest("alpha",
                new List<CryptoEconomyEndpoints.CryptoPrizePoolWinner> { new(playerId, 25) }));
        payoutResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var balanceResp = await _http.GetAsync($"/crypto/balance/{playerId}");
        balanceResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var balance = await balanceResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.CryptoBalanceResponse>();
        balance.Should().NotBeNull();
        balance!.Units.Should().Be(85);

        var poolAfterResp = await _http.GetAsync("/crypto/prize-pool/alpha");
        var poolAfter = await poolAfterResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.CryptoPrizePoolBalanceResponse>();
        poolAfter.Should().NotBeNull();
        poolAfter!.Units.Should().Be(15);
    }

    [Fact]
    public async Task StakeThenUnstake_TracksStakingPosition()
    {
        var playerId = await SignupAndAuthorizeAsync(_http, "stake");
        await SeedCryptoUnitsAsync(playerId, 50);

        var stakeResp = await _http.PostAsJsonAsync("/crypto/stake",
            new CryptoEconomyEndpoints.CryptoStakeRequest(playerId, 30, "season-1"));
        stakeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var positionResp = await _http.GetAsync($"/crypto/staking/{playerId}");
        positionResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var position = await positionResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.CryptoStakingPositionResponse>();
        position.Should().NotBeNull();
        position!.AvailableUnits.Should().Be(20);
        position.StakedUnits.Should().Be(30);

        var unstakeResp = await _http.PostAsJsonAsync("/crypto/unstake",
            new CryptoEconomyEndpoints.CryptoStakeRequest(playerId, 10, "season-1"));
        unstakeResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var positionAfterResp = await _http.GetAsync($"/crypto/staking/{playerId}");
        var positionAfter = await positionAfterResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.CryptoStakingPositionResponse>();
        positionAfter.Should().NotBeNull();
        positionAfter!.AvailableUnits.Should().Be(30);
        positionAfter.StakedUnits.Should().Be(20);
    }

    private static async Task<Guid> SignupAndAuthorizeAsync(HttpClient http, string userPrefix)
    {
        var signupResp = await http.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: $"{userPrefix}-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: $"{userPrefix}-device",
            Username: $"{userPrefix}_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup!.AccessToken);
        return Guid.Parse(signup.UserId);
    }

    private async Task SeedCryptoUnitsAsync(Guid playerId, int units)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-seed-grant");
        tx.AddActor(playerId, PlayerTransactionActorRole.Recipient);
        tx.AddItemChange("crypto:units", units, ItemOperation.Grant);
        tx.MarkApplied();

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync();
    }
}
