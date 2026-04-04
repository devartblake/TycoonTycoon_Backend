using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Api.Features.Store;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Store;

public sealed class StoreIapStrictValidationContractTests : IClassFixture<StrictIapConfiguredFactory>
{
    private readonly HttpClient _http;

    public StoreIapStrictValidationContractTests(StrictIapConfiguredFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ValidateIap_WithStrictConfigConfigured_DoesNotReturnConfigMissing()
    {
        var signupResp = await _http.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: $"iap-strict-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"iap_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        var playerId = Guid.Parse(signup!.UserId);
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        var resp = await _http.PostAsJsonAsync("/store/iap/validate", new StoreEndpoints.IapReceiptValidationRequest(
            PlayerId: playerId,
            Platform: "apple",
            Receipt: "dev-receipt-token",
            ProductId: "coins_pack_small",
            ExternalTransactionId: "tx-dev-001"));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var payload = await resp.Content.ReadFromJsonAsync<StoreEndpoints.IapReceiptValidationResponse>();
        payload.Should().NotBeNull();
        payload!.Status.Should().Be("StrictValidated");
        payload.Valid.Should().BeTrue();
    }
}

public sealed class StrictIapConfiguredFactory : TycoonApiFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureAppConfiguration((_, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Iap:EnableStrictValidation"] = "true",
                ["Iap:AppleSharedSecret"] = "test-apple-shared-secret",
                ["Iap:GooglePackageName"] = "com.tycoon.app.test",
                ["Iap:GoogleServiceAccountJsonPath"] = "/tmp/google-test-sa.json"
            });
        });
    }
}
