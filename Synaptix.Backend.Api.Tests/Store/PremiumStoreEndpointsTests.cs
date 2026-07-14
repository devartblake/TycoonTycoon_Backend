using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Auth;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Store;

public sealed class PremiumStoreEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public PremiumStoreEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Premium_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/v1/store/premium");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Catalog_ReturnsPremiumPlansAsCompatibilityFallback()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<StoreCatalogDto>("/api/v1/store/catalog");

        response.Should().NotBeNull();
        response!.Items.Should().Contain(x => x.Sku == "sub:premium:monthly" && x.ItemType == "premium-subscription");
        response.Items.Should().Contain(x => x.Sku == "sub:premium:seasonal" && x.ItemType == "premium-subscription");
        response.Count.Should().Be(response.Items.Count);
    }

    [Fact]
    public async Task Catalog_SubscriptionFilter_ReturnsPremiumFallbackPlans()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<StoreCatalogDto>("/api/v1/store/catalog?itemType=subscription");

        response.Should().NotBeNull();
        response!.Items.Should().OnlyContain(x => x.ItemType == "premium-subscription");
        response.Items.Should().Contain(x => x.Sku == "sub:premium:monthly");
        response.Items.Should().Contain(x => x.Sku == "sub:premium:seasonal");
    }

    [Fact]
    public async Task Catalog_UnrelatedFilter_DoesNotReturnPremiumFallbackPlans()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetFromJsonAsync<StoreCatalogDto>("/api/v1/store/catalog?itemType=powerup");

        response.Should().NotBeNull();
        response!.Items.Should().NotContain(x => x.ItemType == "premium-subscription");
    }

    [Fact]
    public async Task Premium_WithAuth_ReturnsConfiguredCatalogAndNullSaleInfo()
    {
        using var client = _factory.CreateClient();
        await AuthenticateAsync(client, "premium-catalog");

        var response = await client.GetFromJsonAsync<PremiumStoreDto>("/api/v1/store/premium");

        response.Should().NotBeNull();
        response!.SaleInfo.Should().BeNull();
        response.AdFree.Plans.Should().HaveCountGreaterOrEqualTo(2);
        response.AdFree.Plans[0].Id.Should().Be("premium-monthly");
        response.AdFree.Plans[1].Id.Should().Be("premium-seasonal");
        response.RewardCenter.Cards.Should().Contain(x => x.RewardId == "daily-checkin");
        response.RewardCenter.Cards.Should().Contain(x => x.RewardId == "watch-ad");
    }

    [Fact]
    public async Task Rewards_ForSelf_ReturnsDefaultState()
    {
        using var client = _factory.CreateClient();
        var signup = await AuthenticateAsync(client, "reward-default");
        var playerId = Guid.Parse(signup.UserId);

        var response = await client.GetFromJsonAsync<RewardCenterDto>($"/api/v1/store/rewards/{playerId}");

        response.Should().NotBeNull();
        var dailyCheckin = response!.Cards.Single(x => x.RewardId == "daily-checkin");
        var watchAd = response.Cards.Single(x => x.RewardId == "watch-ad");

        dailyCheckin.IsClaimAvailable.Should().BeTrue();
        dailyCheckin.Availability.Should().Be("available");
        dailyCheckin.Progress.Should().Be(0);

        watchAd.IsClaimAvailable.Should().BeTrue();
        watchAd.RemainingClaims.Should().Be(3);
        watchAd.DailyCap.Should().Be(3);
    }

    [Fact]
    public async Task Rewards_ForDifferentPlayer_Returns403()
    {
        using var playerAClient = _factory.CreateClient();
        var playerA = await AuthenticateAsync(playerAClient, "reward-player-a");
        var playerAId = Guid.Parse(playerA.UserId);

        using var playerBClient = _factory.CreateClient();
        await AuthenticateAsync(playerBClient, "reward-player-b");

        var response = await playerBClient.GetAsync($"/api/v1/store/rewards/{playerAId}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rewards_ForDeletedSelf_Returns404()
    {
        using var client = _factory.CreateClient();
        var signup = await AuthenticateAsync(client, "reward-deleted");
        var playerId = Guid.Parse(signup.UserId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var user = await db.Users.FindAsync(playerId);
            user.Should().NotBeNull();
            db.Users.Remove(user!);
            await db.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/v1/store/rewards/{playerId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClaimDailyCheckin_CreditsWallet_AndSecondClaimReturns409()
    {
        using var client = _factory.CreateClient();
        var signup = await AuthenticateAsync(client, "claim-daily");
        var playerId = Guid.Parse(signup.UserId);

        var firstClaim = await client.PostAsync($"/api/v1/store/rewards/{playerId}/claim/daily-checkin", null);
        firstClaim.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await firstClaim.Content.ReadFromJsonAsync<ClaimStoreRewardResponseDto>();
        firstBody.Should().NotBeNull();
        firstBody!.RewardId.Should().Be("daily-checkin");
        firstBody.CoinsAwarded.Should().Be(25);
        firstBody.NewBalance.Should().Be(25);
        firstBody.CurrentStreak.Should().Be(1);

        var secondClaim = await client.PostAsync($"/api/v1/store/rewards/{playerId}/claim/daily-checkin", null);
        secondClaim.StatusCode.Should().Be(HttpStatusCode.Conflict);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        var rewardTxns = db.PlayerTransactions
            .Where(t => t.Kind == "store-reward-daily-checkin" && t.Actors.Any(a => a.PlayerId == playerId))
            .ToList();

        rewardTxns.Should().HaveCount(1);
        db.PlayerWallets.Single(w => w.PlayerId == playerId).Coins.Should().Be(25);
    }

    [Fact]
    public async Task ClaimWatchAd_StopsAtDailyCap()
    {
        using var client = _factory.CreateClient();
        var signup = await AuthenticateAsync(client, "claim-watch-ad");
        var playerId = Guid.Parse(signup.UserId);

        for (var i = 1; i <= 3; i++)
        {
            var response = await client.PostAsync($"/api/v1/store/rewards/{playerId}/claim/watch-ad", null);
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var body = await response.Content.ReadFromJsonAsync<ClaimStoreRewardResponseDto>();
            body.Should().NotBeNull();
            body!.RewardId.Should().Be("watch-ad");
            body.RemainingClaims.Should().Be(3 - i);
        }

        var fourthResponse = await client.PostAsync($"/api/v1/store/rewards/{playerId}/claim/watch-ad", null);
        fourthResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var rewards = await client.GetFromJsonAsync<RewardCenterDto>($"/api/v1/store/rewards/{playerId}");
        var watchAd = rewards!.Cards.Single(x => x.RewardId == "watch-ad");
        watchAd.IsClaimAvailable.Should().BeFalse();
        watchAd.RemainingClaims.Should().Be(0);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();
        db.PlayerTransactions.Count(t => t.Kind == "store-reward-watch-ad" && t.Actors.Any(a => a.PlayerId == playerId))
            .Should().Be(3);
        db.PlayerWallets.Single(w => w.PlayerId == playerId).Coins.Should().Be(45);
    }

    [Fact]
    public async Task ClaimReward_UnknownReward_Returns404()
    {
        using var client = _factory.CreateClient();
        var signup = await AuthenticateAsync(client, "claim-unknown");
        var playerId = Guid.Parse(signup.UserId);

        var response = await client.PostAsync($"/api/v1/store/rewards/{playerId}/claim/not-a-reward", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ClaimReward_ForDifferentPlayer_Returns403()
    {
        using var playerAClient = _factory.CreateClient();
        var playerA = await AuthenticateAsync(playerAClient, "claim-player-a");
        var playerAId = Guid.Parse(playerA.UserId);

        using var playerBClient = _factory.CreateClient();
        await AuthenticateAsync(playerBClient, "claim-player-b");

        var response = await playerBClient.PostAsync($"/api/v1/store/rewards/{playerAId}/claim/daily-checkin", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<SignupResponse> AuthenticateAsync(HttpClient client, string prefix)
    {
        var email = $"{prefix}-{Guid.NewGuid():N}@example.com";
        var handle = $"{prefix}_{Guid.NewGuid():N}";
        var user = new User(email, handle, "test-password-hash");
        var token = string.Empty;

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var jwtSettings = scope.ServiceProvider.GetRequiredService<IOptions<JwtSettings>>().Value;
            db.Users.Add(user);
            await db.SaveChangesAsync();
            token = CreateAccessToken(user, jwtSettings);
        }

        var response = new SignupResponse(
            AccessToken: token,
            RefreshToken: "test-refresh-token",
            ExpiresIn: 3600,
            UserId: user.Id.ToString(),
            User: new UserDto(user.Id, user.Handle, user.Email, user.Country, user.AvatarUrl, user.Tier, user.Mmr));

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", response.AccessToken);

        return response;
    }

    private static string CreateAccessToken(User user, JwtSettings settings)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new("handle", user.Handle),
            new("role", "user"),
            new("scope", "profile:read profile:write gameplay:read gameplay:write"),
            new("client_type", "user"),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: settings.Issuer,
            audience: "mobile-app",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(settings.AccessTokenExpirationMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
