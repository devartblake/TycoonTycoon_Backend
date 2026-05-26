using System.Net;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaptix.Backend.Api.Features.Crypto;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Crypto;

public sealed class CryptoWithdrawalSettlementTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public CryptoWithdrawalSettlementTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ApproveWithdrawal_WithAdminOpsKey_AppliesPendingTransaction()
    {
        var playerId = await SignupAndAuthorizeAsync(_http, "withdraw-approve");
        var pendingId = await SeedPendingWithdrawalAsync(playerId, 12, "wallet-approve");

        _http.WithAdminOpsKey();
        SetCryptoServiceAuthorization(_http);
        var approveResp = await _http.PostAsync($"/crypto/withdraw/{pendingId}/approve", content: null);
        approveResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await approveResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.WithdrawalSettlementResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PlayerTransactionStatus.Applied.ToString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
        var tx = await db.PlayerTransactions.AsNoTracking().FirstAsync(x => x.Id == pendingId);
        tx.Status.Should().Be(PlayerTransactionStatus.Applied);
    }

    [Fact]
    public async Task PendingWithdrawals_WithoutAdminOpsKey_Returns401()
    {
        var _ = await SignupAndAuthorizeAsync(_http, "withdraw-list");
        var resp = await _http.GetAsync("/crypto/withdraw/pending?page=1&pageSize=10");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task RejectWithdrawal_WithAdminOpsKey_FailsPendingTransaction()
    {
        var playerId = await SignupAndAuthorizeAsync(_http, "withdraw-reject");
        var pendingId = await SeedPendingWithdrawalAsync(playerId, 9, "wallet-reject");

        _http.WithAdminOpsKey();
        SetCryptoServiceAuthorization(_http);
        var rejectResp = await _http.PostAsync($"/crypto/withdraw/{pendingId}/reject", content: null);
        rejectResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await rejectResp.Content.ReadFromJsonAsync<CryptoEconomyEndpoints.WithdrawalSettlementResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be(PlayerTransactionStatus.Failed.ToString());
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

    private async Task<Guid> SeedPendingWithdrawalAsync(Guid playerId, int units, string wallet)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();

        var tx = new PlayerTransaction(Guid.NewGuid(), "crypto-withdraw-request", receipt: wallet);
        tx.AddActor(playerId, PlayerTransactionActorRole.Sender);
        tx.AddItemChange("crypto:units", units, ItemOperation.Revoke);

        db.PlayerTransactions.Add(tx);
        await db.SaveChangesAsync();
        return tx.Id;
    }

    private void SetCryptoServiceAuthorization(HttpClient http)
    {
        using var scope = _factory.Services.CreateScope();
        var jwt = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SecretKey));
        var token = new JwtSecurityToken(
            issuer: jwt.Issuer,
            audience: "crypto-service",
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, "synaptix-crypto-service"),
                new Claim("role", "service"),
                new Claim("scope", "crypto:settlement")
            ],
            expires: DateTime.UtcNow.AddMinutes(10),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
    }
}
