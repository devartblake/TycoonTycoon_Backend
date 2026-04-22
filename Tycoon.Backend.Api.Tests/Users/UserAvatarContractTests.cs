using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Users;

public sealed class UserAvatarContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public UserAvatarContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AvatarUploadUrl_RequiresAuthentication()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/users/me/avatar/upload-url", new AvatarUploadUrlRequest(
            "avatar.jpg",
            "image/jpeg",
            128));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AvatarUploadUrl_ReturnsPortableUploadContract()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PostAsJsonAsync("/users/me/avatar/upload-url", new AvatarUploadUrlRequest(
            "avatar.jpg",
            "image/jpeg",
            128));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AvatarUploadUrlResponse>();
        payload.Should().NotBeNull();
        payload!.ObjectKey.Should().StartWith("avatars/");
        payload.UploadUrl.Should().NotBeNullOrWhiteSpace();
        payload.PublicUrl.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task GetMe_AfterAvatarProfileUpdate_AndFreshLogin_ReturnsPersistedAvatarUrl()
    {
        var email = $"avatar-persist-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!";
        var signupClient = _factory.CreateClient();

        var signupResp = await signupClient.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: password,
            DeviceId: "ios-sim-signup",
            Username: $"avatar_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        signupClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", signup!.AccessToken);

        const string avatarUrl = "https://cdn.example.com/avatars/user-123/avatar.jpg";
        var patchResp = await signupClient.PatchAsJsonAsync(
            "/users/me",
            new UpdateProfileRequest("updated_handle", "US", avatarUrl));
        patchResp.EnsureSuccessStatusCode();

        var loginClient = _factory.CreateClient();
        var loginResp = await loginClient.PostAsJsonAsync("/auth/login", new LoginRequest(
            Email: email,
            Password: password,
            DeviceId: "ios-sim-fresh-login"));
        loginResp.EnsureSuccessStatusCode();

        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        login.Should().NotBeNull();

        loginClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login!.AccessToken);

        var me = await loginClient.GetFromJsonAsync<UserDto>("/users/me");

        me.Should().NotBeNull();
        me!.Handle.Should().Be("updated_handle");
        me.Country.Should().Be("US");
        me.AvatarUrl.Should().Be(avatarUrl);
    }

    [Fact]
    public async Task AvatarProfileUpdate_DoesNotBreakHandleAndCountryUpdates()
    {
        var client = await CreateAuthenticatedClientAsync();

        var response = await client.PatchAsJsonAsync(
            "/users/me",
            new UpdateProfileRequest("profile_contract_user", "CA", null));
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<UserDto>();
        payload.Should().NotBeNull();
        payload!.Handle.Should().Be("profile_contract_user");
        payload.Country.Should().Be("CA");
        payload.AvatarUrl.Should().BeNull();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();

        var signupResp = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: $"avatar-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"avatar_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", signup!.AccessToken);
        return client;
    }
}
