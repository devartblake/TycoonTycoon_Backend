using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Features.Ml;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Ml;

public sealed class MlScoringEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MlScoringEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ChurnRisk_WithoutConfiguredModel_FallsBackToHeuristic()
    {
        await SignupAndAuthorizeAsync(_http, "ml-churn");

        var resp = await _http.PostAsJsonAsync("/api/v1/ml/churn-risk", new MlScoringEndpoints.ChurnRiskRequest(
            PlayerId: Guid.NewGuid(),
            CorrectRate: 0.35m,
            DisconnectRate: 0.40m,
            RecentSessions: 2,
            DaysSinceLastSeen: 6));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resp.Content.ReadFromJsonAsync<MlScoringEndpoints.ChurnRiskResponse>();
        payload.Should().NotBeNull();
        payload!.Source.Should().Be("heuristic");
        payload.Score.Should().BeGreaterThan(0m);
    }

    [Fact]
    public async Task MatchQuality_WithoutConfiguredModel_FallsBackToHeuristic()
    {
        await SignupAndAuthorizeAsync(_http, "ml-quality");

        var resp = await _http.PostAsJsonAsync("/api/v1/ml/match-quality", new MlScoringEndpoints.MatchQualityRequest(
            MatchId: Guid.NewGuid(),
            CorrectRate: 0.70m,
            DisconnectRate: 0.05m,
            AverageAnswerTimeMs: 4200));

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var payload = await resp.Content.ReadFromJsonAsync<MlScoringEndpoints.MatchQualityResponse>();
        payload.Should().NotBeNull();
        payload!.Source.Should().Be("heuristic");
        payload.Score.Should().BeGreaterThan(0m);
    }

    private static async Task SignupAndAuthorizeAsync(HttpClient http, string userPrefix)
    {
        var signupResp = await http.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
            Email: $"{userPrefix}-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: $"{userPrefix}-device",
            Username: $"{userPrefix}_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup!.AccessToken);
    }
}
