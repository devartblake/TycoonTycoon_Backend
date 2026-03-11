using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Users;

public sealed class UserProfileSecurityContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public UserProfileSecurityContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateMe_WithUnknownAuthenticatedUser_ReturnsNotFoundEnvelope()
    {
        var client = _factory.CreateClient();

        var email = $"users-contract-{Guid.NewGuid():N}@example.com";
        var signupResp = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"contract_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup!.AccessToken);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var user = await db.Users.FindAsync(signup.User.Id);
            user.Should().NotBeNull();
            db.Users.Remove(user!);
            await db.SaveChangesAsync();
        }

        var patch = await client.PatchAsJsonAsync("/users/me", new UpdateProfileRequest("updated_handle", "US"));

        patch.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await patch.HasErrorCodeAsync("NOT_FOUND");
    }
}
