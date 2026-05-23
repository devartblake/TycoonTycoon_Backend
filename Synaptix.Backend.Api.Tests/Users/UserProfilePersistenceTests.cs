using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Users;

public sealed class UserProfilePersistenceTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public UserProfilePersistenceTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetMe_AfterProfileUpdate_AndFreshLogin_ReturnsPersistedProfile()
    {
        var email = $"users-persist-{Guid.NewGuid():N}@example.com";
        const string password = "Passw0rd!";
        var originalHandle = $"persist_user_{Guid.NewGuid():N}";

        var signupClient = _factory.CreateClient();
        var signupResp = await signupClient.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: password,
            DeviceId: "ios-sim-signup",
            Username: originalHandle));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        signupClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", signup!.AccessToken);

        var patchResp = await signupClient.PatchAsJsonAsync(
            "/users/me",
            new UpdateProfileRequest("updated_handle", "US"));
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
        me!.Email.Should().Be(email.ToLowerInvariant());
        me.Handle.Should().Be("updated_handle");
        me.Country.Should().Be("US");
    }
}
