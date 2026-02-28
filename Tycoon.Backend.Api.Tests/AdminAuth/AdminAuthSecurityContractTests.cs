using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminAuth;

public sealed class AdminAuthSecurityContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminAuthSecurityContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task AdminMe_WithoutBearer_Returns401()
    {
        var resp = await _http.GetAsync("/admin/auth/me");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminMe_WithUserToken_Returns403()
    {
        var userToken = await SignupAndGetUserTokenAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, "/admin/auth/me");
        req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userToken);

        var resp = await _http.SendAsync(req);
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminLogin_InvalidCredentials_Eventually429()
    {
        HttpStatusCode? hit = null;

        for (var i = 0; i < 20; i++)
        {
            var resp = await _http.PostAsJsonAsync("/admin/auth/login",
                new AdminLoginRequest("nobody@example.com", "wrong-pass"));

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                hit = resp.StatusCode;
                break;
            }

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await resp.HasErrorCodeAsync("UNAUTHORIZED");
        }

        hit.Should().Be(HttpStatusCode.TooManyRequests, "admin auth login should be rate-limited");
    }

    [Fact]
    public async Task AdminRefresh_InvalidToken_Eventually429()
    {
        HttpStatusCode? hit = null;

        for (var i = 0; i < 30; i++)
        {
            var resp = await _http.PostAsJsonAsync("/admin/auth/refresh", new RefreshRequest("bogus-refresh-token"));

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
            {
                hit = resp.StatusCode;
                break;
            }

            resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            await resp.HasErrorCodeAsync("UNAUTHORIZED");
        }

        hit.Should().Be(HttpStatusCode.TooManyRequests, "admin auth refresh should be rate-limited");
    }

    private async Task<string> SignupAndGetUserTokenAsync()
    {
        var email = $"user_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!123";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var signupResp = await _http.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"u_{Guid.NewGuid():N}"));

        signupResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        signup!.AccessToken.Should().NotBeNullOrWhiteSpace();

        return signup.AccessToken;
    }
}
