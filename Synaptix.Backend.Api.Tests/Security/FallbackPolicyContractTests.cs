using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Security;

// #405: verifies the global deny-by-default authorization FallbackPolicy that AdminPolicies.cs
// installs. Runs against FallbackPolicyApiFactory (fallback ACTIVE, mirroring production) so these
// tests actually exercise the deny-by-default behaviour the production config depends on — the
// default TycoonApiFactory disables the fallback and cannot.
public sealed class FallbackPolicyContractTests : IClassFixture<FallbackPolicyApiFactory>
{
    private readonly FallbackPolicyApiFactory _factory;

    public FallbackPolicyContractTests(FallbackPolicyApiFactory factory) => _factory = factory;

    [Fact]
    public async Task ProtectedEndpoint_WithoutToken_IsDeniedByFallback()
    {
        using var http = _factory.CreateClient();

        var response = await http.GetAsync("/api/v1/account/rewards/status");

        // Deny-by-default: an endpoint requiring auth returns 401 with no bearer.
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // Endpoints explicitly marked .AllowAnonymous() must stay reachable even with the deny-by-
    // default fallback active. If someone drops an AllowAnonymous, the endpoint starts 401-ing and
    // the matching assertion here fails loudly.
    [Theory]
    [InlineData("/healthz")]
    public async Task PublicGetEndpoints_StayReachable_UnderFallback(string path)
    {
        using var http = _factory.CreateClient();

        var response = await http.GetAsync(path);

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AnalyticsIngest_StaysAnonymous_UnderFallback()
    {
        using var http = _factory.CreateClient();

        // Anonymous ingest endpoint: a malformed/empty body may yield 4xx, but never 401.
        var response = await http.PostAsJsonAsync("/api/v1/analytics/track", new { });

        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AuthEntryPoint_DeviceBootstrap_StaysAnonymous_UnderFallback()
    {
        using var http = _factory.CreateClient();

        var response = await http.PostAsJsonAsync(
            "/api/v1/auth/device/bootstrap",
            new { deviceId = $"fallback-test-{Guid.NewGuid():N}", deviceType = "android" });

        // A pre-auth entry point must succeed with no bearer despite deny-by-default.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.IsSuccessStatusCode.Should().BeTrue();
    }
}
