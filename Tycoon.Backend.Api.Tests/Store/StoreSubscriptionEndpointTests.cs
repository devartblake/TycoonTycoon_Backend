using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Store;

public sealed class StoreSubscriptionEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public StoreSubscriptionEndpointTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task SubscriptionStatus_WithoutAuth_Returns401()
    {
        var response = await _http.GetAsync($"/store/subscription/status/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActivateSubscription_ThenStatus_ReturnsActiveSubscription()
    {
        var signupResp = await _http.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: $"sub-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"sub_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        var playerId = Guid.Parse(signup!.UserId);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        var activate = await _http.PostAsJsonAsync("/store/subscription/activate", new ActivateSubscriptionRequest(
            PlayerId: playerId,
            Tier: "premium",
            BillingPeriod: "monthly",
            ExternalTransactionId: "sub-tx-001"));
        activate.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await _http.GetFromJsonAsync<SubscriptionStatusDto>($"/store/subscription/status/{playerId}");
        status.Should().NotBeNull();
        status!.IsActive.Should().BeTrue();
        status.Tier.Should().Be("premium");
        status.BillingPeriod.Should().Be("monthly");
    }
}
