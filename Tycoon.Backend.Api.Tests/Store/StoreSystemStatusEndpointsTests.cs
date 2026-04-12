using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Store;

public sealed class StoreSystemStatusEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StoreSystemStatusEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PublicStatus_ReflectsProviderAvailability()
    {
        using var factory = CreateFactory(stripeEnabled: true, payPalEnabled: false);
        using var client = factory.CreateClient();

        var status = await client.GetFromJsonAsync<StoreSystemStatusDto>("/store/system/status");
        status.Should().NotBeNull();
        status!.StoreEnabled.Should().BeTrue();
        status.PaymentsEnabled.Should().BeTrue();
        status.StripeConfigured.Should().BeTrue();
        status.StripeEnabled.Should().BeTrue();
        status.PayPalConfigured.Should().BeFalse();
        status.PayPalEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task AdminToggle_DisablesStripeCheckout_AndFrontendCanReadStatus()
    {
        using var factory = CreateFactory(stripeEnabled: true, payPalEnabled: true);
        using var admin = factory.CreateClient().WithAdminOpsKey();
        using var client = factory.CreateClient();

        var patchResp = await admin.PatchAsJsonAsync(
            "/admin/store/system/status",
            new UpdateStoreSystemStatusRequest(StoreEnabled: true, PaymentsEnabled: true, StripeEnabled: false, PayPalEnabled: true));

        patchResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var status = await client.GetFromJsonAsync<StoreSystemStatusDto>("/store/system/status");
        status.Should().NotBeNull();
        status!.StripeEnabled.Should().BeFalse();
        status.PayPalEnabled.Should().BeTrue();

        var signup = await SignupAsync(client, "store-toggle");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        var response = await client.PostAsJsonAsync(
            "/store/payments/checkout/session",
            new CreateStripeCheckoutSessionRequest(Guid.Parse(signup.UserId), "powerup:skip", 1));

        response.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        await response.HasErrorCodeAsync("STRIPE_DISABLED");
    }

    private WebApplicationFactory<Program> CreateFactory(bool stripeEnabled, bool payPalEnabled)
    {
        return _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, cfg) =>
            {
                cfg.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Stripe:Enabled"] = stripeEnabled ? "true" : "false",
                    ["PayPal:Enabled"] = payPalEnabled ? "true" : "false"
                });
            });
        });
    }

    private static async Task<SignupResponse> SignupAsync(HttpClient client, string prefix)
    {
        var signupResp = await client.PostAsJsonAsync(
            "/auth/signup",
            new SignupRequest(
                Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
                Password: "Passw0rd!",
                DeviceId: "ios-sim",
                Username: $"{prefix}_{Guid.NewGuid():N}"));

        signupResp.EnsureSuccessStatusCode();
        return (await signupResp.Content.ReadFromJsonAsync<SignupResponse>())!;
    }
}
