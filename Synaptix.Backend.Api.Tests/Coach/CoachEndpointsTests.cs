using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Coach;

public sealed class CoachEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public CoachEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    // ── Anonymous access returns 401 ──────────────────────────────────────

    [Fact]
    public async Task GetDailyBrief_AnonymousAccess_Returns401()
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.GetAsync("/coach/00000000-0000-0000-0000-000000000001/daily-brief");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostFeedback_AnonymousAccess_Returns401()
    {
        using var anon = _factory.CreateClient();
        var resp = await anon.PostAsJsonAsync(
            "/coach/00000000-0000-0000-0000-000000000001/feedback",
            new CoachFeedbackRequest("brief-123", "helpful"));
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── Authenticated owner — happy-path contract tests ───────────────────

    [Fact]
    public async Task GetDailyBrief_AuthenticatedOwner_Returns200WithFullContract()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.GetAsync($"/coach/{playerId}/daily-brief");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var brief = await resp.Content.ReadFromJsonAsync<CoachBriefDto>();
        brief.Should().NotBeNull();
        brief!.Title.Should().NotBeNullOrWhiteSpace();
        brief.Message.Should().NotBeNullOrWhiteSpace();
        brief.RecommendedAction.Should().NotBeNullOrWhiteSpace();
        brief.Tone.Should().NotBeNullOrWhiteSpace();
        // TargetRoute is nullable by contract; when present it must be non-empty
        if (brief.TargetRoute is not null)
            brief.TargetRoute.Should().NotBeEmpty();
    }

    [Fact]
    public async Task PostFeedback_AuthenticatedOwner_Returns202()
    {
        var (token, playerId) = await SignupAsync();
        using var http = AuthClient(token);

        var resp = await http.PostAsJsonAsync(
            $"/coach/{playerId}/feedback",
            new CoachFeedbackRequest(BriefId: "brief-abc", Feedback: "very helpful"));

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    // ── Cross-user access returns 403 ─────────────────────────────────────

    [Fact]
    public async Task GetDailyBrief_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var resp = await http.GetAsync($"/coach/{otherPlayerId}/daily-brief");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PostFeedback_AuthenticatedAsOtherUser_Returns403()
    {
        var (token, _) = await SignupAsync();
        using var http = AuthClient(token);

        var otherPlayerId = Guid.NewGuid();
        var resp = await http.PostAsJsonAsync(
            $"/coach/{otherPlayerId}/feedback",
            new CoachFeedbackRequest(BriefId: "brief-xyz", Feedback: "not helpful"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private async Task<(string Token, Guid PlayerId)> SignupAsync()
    {
        using var anon = _factory.CreateClient();

        var email    = $"coach_{Guid.NewGuid():N}@example.com";
        var password = "Passw0rd!Test";
        var deviceId = $"dev-{Guid.NewGuid():N}";

        var resp = await anon.PostAsJsonAsync("/auth/signup",
            new SignupRequest(email, password, deviceId, Username: $"coach_{Guid.NewGuid():N}"));

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
