using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Personalization;

public sealed class PersonalizationEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public PersonalizationEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    // ── Anonymous access returns 401 ──────────────────────────────────────

    [Theory]
    [InlineData("/personalization/profile/00000000-0000-0000-0000-000000000001")]
    [InlineData("/personalization/home/00000000-0000-0000-0000-000000000001")]
    [InlineData("/personalization/recommendations/00000000-0000-0000-0000-000000000001")]
    [InlineData("/personalization/notifications/00000000-0000-0000-0000-000000000001")]
    public async Task GetEndpoints_AnonymousAccess_Returns401(string route)
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.GetAsync(route);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/personalization/profile/00000000-0000-0000-0000-000000000001/event")]
    [InlineData("/personalization/profile/00000000-0000-0000-0000-000000000001/recalculate")]
    public async Task PostProfileEndpoints_AnonymousAccess_Returns401(string route)
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync(route, new { });
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("/personalization/recommendations/00000000-0000-0000-0000-000000000001/accept?playerId=00000000-0000-0000-0000-000000000002")]
    [InlineData("/personalization/recommendations/00000000-0000-0000-0000-000000000001/dismiss?playerId=00000000-0000-0000-0000-000000000002")]
    public async Task RecommendationActionEndpoints_AnonymousAccess_Returns401(string route)
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.PostAsync(route, content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated owner — happy-path contract tests ───────────────────

    [Fact]
    public async Task GetProfile_AuthenticatedOwner_Returns200WithProfile()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/personalization/profile/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await resp.Content.ReadFromJsonAsync<PlayerMindProfileDto>();
        profile.Should().NotBeNull();
        profile!.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public async Task PostEvent_AuthenticatedOwner_Returns202()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var evt = new PlayerBehaviorEventDto(
            EventType: "match_completed",
            EventSource: "ranked",
            Category: "math",
            Difficulty: "medium",
            Mode: "ranked",
            Metadata: null,
            OccurredAt: DateTimeOffset.UtcNow);

        var resp = await http.PostAsJsonAsync($"/personalization/profile/{playerId}/event", evt);

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task PostRecalculate_AuthenticatedOwner_Returns200WithProfile()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.PostAsync($"/personalization/profile/{playerId}/recalculate", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await resp.Content.ReadFromJsonAsync<PlayerMindProfileDto>();
        profile.Should().NotBeNull();
        profile!.PlayerId.Should().Be(playerId);
    }

    [Fact]
    public async Task GetHome_AuthenticatedOwner_Returns200WithRecommendationsAndCoachBrief()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/personalization/home/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var home = await resp.Content.ReadFromJsonAsync<PlayerHomePersonalizationDto>();
        home.Should().NotBeNull();
        home!.PlayerId.Should().Be(playerId);
        home.Recommendations.Should().NotBeNull();
        home.CoachBrief.Should().NotBeNull();
    }

    [Fact]
    public async Task GetRecommendations_AuthenticatedOwner_Returns200WithArray()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/personalization/recommendations/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var recs = await resp.Content.ReadFromJsonAsync<List<PlayerRecommendationDto>>();
        recs.Should().NotBeNull();
    }

    [Fact]
    public async Task AcceptRecommendation_NonExistentId_Returns204()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var fakeRecId = Guid.NewGuid();
        var resp = await http.PostAsync(
            $"/personalization/recommendations/{fakeRecId}/accept?playerId={playerId}",
            content: null);

        // The service silently no-ops if the record doesn't exist — still 204
        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DismissRecommendation_NonExistentId_Returns204()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var fakeRecId = Guid.NewGuid();
        var resp = await http.PostAsync(
            $"/personalization/recommendations/{fakeRecId}/dismiss?playerId={playerId}",
            content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetNotifications_AuthenticatedOwner_Returns200WithNotificationPersonalization()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/personalization/notifications/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<NotificationPersonalizationDto>();
        result.Should().NotBeNull();
        result!.PlayerId.Should().Be(playerId);
        result.AppliedGuardrails.Should().ContainKey("adaptiveNotificationsEnabled");
        result.AppliedGuardrails.Should().ContainKey("localFatigueSuppressed");
        result.AppliedGuardrails.Should().ContainKey("sidecarFatigueSuppressed");
    }

    [Fact]
    public async Task GetNotifications_AuthenticatedOwner_RecommendationIncludesToneAndIntent()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/personalization/notifications/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await resp.Content.ReadFromJsonAsync<NotificationPersonalizationDto>();
        result.Should().NotBeNull();

        // A fresh player has low fatigue so should receive a recommendation
        if (result!.CanReceiveNotification && result.Recommendation is not null)
        {
            result.Recommendation.Payload.Should().ContainKey("tone");
            result.Recommendation.Payload.Should().ContainKey("intent");
        }
    }

    [Fact]
    public async Task GetNotifications_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var resp = await http.GetAsync($"/personalization/notifications/{otherPlayerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Cross-user access returns 403 ─────────────────────────────────────

    [Fact]
    public async Task GetProfile_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var resp = await http.GetAsync($"/personalization/profile/{otherPlayerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostEvent_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var evt = new PlayerBehaviorEventDto(
            EventType: "match_completed",
            EventSource: "ranked",
            Category: null,
            Difficulty: null,
            Mode: null,
            Metadata: null,
            OccurredAt: null);
        var resp = await http.PostAsJsonAsync($"/personalization/profile/{otherPlayerId}/event", evt);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetHome_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var resp = await http.GetAsync($"/personalization/home/{otherPlayerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AcceptRecommendation_CrossUserPlayerId_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var fakeRecId = Guid.NewGuid();
        var otherPlayerId = Guid.NewGuid();
        var resp = await http.PostAsync(
            $"/personalization/recommendations/{fakeRecId}/accept?playerId={otherPlayerId}",
            content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(string Token, Guid PlayerId)> SignupAsync()
    {
        using var anon = _factory.CreateClient();

        var email    = $"ptest_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!Test";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var resp = await anon.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"ptest_{Guid.NewGuid():N}"));

        resp.EnsureSuccessStatusCode();

        var signup = await resp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        var playerId = Guid.Parse(signup!.UserId);
        return (signup.AccessToken, playerId);
    }

    private HttpClient AuthClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
