using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Auth;

public sealed class AuthEndpointsContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AuthEndpointsContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Signup_HappyPath_Returns_Tokens_And_UserId()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/signup", NewSignupPayload());

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("userId").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Signup_DuplicateEmail_Returns_409_Conflict()
    {
        var email = UniqueEmail();
        await _http.PostAsJsonAsync("/api/v1/auth/signup", NewSignupPayload(email));

        var dup = await _http.PostAsJsonAsync("/api/v1/auth/signup", NewSignupPayload(email));

        dup.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var body = await dup.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("error").GetString().Should().Be("email_already_exists");
    }

    [Theory]
    [InlineData("", "Passw0rd!", "dev-1", "missing email")]
    [InlineData("user@example.com", "", "dev-2", "missing password")]
    [InlineData("user2@example.com", "Passw0rd!", "", "missing deviceId")]
    [InlineData("user3@example.com", "short", "dev-3", "password too short")]
    public async Task Signup_InvalidInput_Returns_400(string email, string password, string deviceId, string _)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/signup", new { email, password, deviceId, username = "testuser" });
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns_Tokens()
    {
        const string password = "Passw0rd!";
        var email = UniqueEmail();
        await _http.PostAsJsonAsync("/api/v1/auth/signup", NewSignupPayload(email));

        var resp = await _http.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email,
            password,
            deviceId = UniqueId(),
        });

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("refreshToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_WrongPassword_Returns_401()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/login", new
        {
            email = "nobody@example.com",
            password = "wrongpassword",
            deviceId = UniqueId(),
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ValidToken_Returns_New_Tokens()
    {
        var signup = await _http.PostAsJsonAsync("/api/v1/auth/signup", NewSignupPayload());
        var signupBody = await signup.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = signupBody.GetProperty("refreshToken").GetString()!;

        var resp = await _http.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });

        resp.IsSuccessStatusCode.Should().BeTrue();
        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("accessToken").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Refresh_InvalidToken_Returns_401()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken = "not-a-real-token" });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Without_Bearer_Returns_401()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/auth/logout", new { deviceId = UniqueId() });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static string UniqueEmail() => $"test-{Guid.NewGuid():N}@example.com";
    private static string UniqueId() => Guid.NewGuid().ToString("N")[..12];

    private static object NewSignupPayload(string? email = null) => new
    {
        email = email ?? UniqueEmail(),
        password = "Passw0rd!",
        deviceId = UniqueId(),
        username = $"u{UniqueId()}",
    };
}
