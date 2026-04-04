using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Users;

public sealed class UserCareerSummaryEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public UserCareerSummaryEndpointTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CareerSummary_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/users/{Guid.NewGuid()}/career-summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CareerSummary_WithProfiles_ReturnsAggregatedStats()
    {
        var client = _factory.CreateClient();
        var email = $"career-summary-{Guid.NewGuid():N}@example.com";

        var signupResp = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"career_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        var userId = Guid.Parse(signup!.UserId);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var seasonA = new PlayerSeasonProfile(Guid.NewGuid(), userId, 0);
            seasonA.ApplyMatchOutcome(win: true, draw: false);
            seasonA.ApplyMatchOutcome(win: true, draw: false);
            seasonA.ApplyMatchOutcome(win: false, draw: false);

            var seasonB = new PlayerSeasonProfile(Guid.NewGuid(), userId, 0);
            seasonB.ApplyMatchOutcome(win: false, draw: true);
            seasonB.ApplyMatchOutcome(win: false, draw: false);

            db.PlayerSeasonProfiles.AddRange(seasonA, seasonB);
            await db.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<UserCareerSummaryDto>($"/users/{userId}/career-summary");
        response.Should().NotBeNull();
        response!.Wins.Should().Be(2);
        response.Losses.Should().Be(2);
        response.Draws.Should().Be(1);
        response.MatchesPlayed.Should().Be(5);
        response.WinRate.Should().Be(0.4m);
    }
}
