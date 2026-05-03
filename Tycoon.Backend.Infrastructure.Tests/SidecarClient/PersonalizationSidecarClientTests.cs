using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Infrastructure.SidecarClient;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Infrastructure.Tests.SidecarClient;

public sealed class PersonalizationSidecarClientTests
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    // ── SidecarPersonalizationOptions ─────────────────────────────────────────

    [Fact]
    public void Options_Defaults_AreCorrect()
    {
        var opts = new SidecarPersonalizationOptions();

        opts.BaseUrl.Should().Be("http://localhost:8001");
        opts.TimeoutSeconds.Should().Be(5);
        opts.Enabled.Should().BeTrue();
    }

    // ── NullPersonalizationSidecarClient ──────────────────────────────────────

    [Fact]
    public async Task NullClient_ScorePlayer_ReturnsNeutralDefaults()
    {
        IPersonalizationSidecarClient client = new NullPersonalizationSidecarClient();

        var result = await client.ScorePlayerAsync(
            new SidecarPlayerScoringRequest(
                "player-1",
                [],
                new SidecarPlayerSnapshotDto(0.5m, 0.2m, 0.1m, 0m, "new_player")));

        result.ConfidenceLevel.Should().Be(0.50m);
        result.ChurnRiskScore.Should().Be(0m);
        result.FrustrationRiskScore.Should().Be(0m);
        result.RecommendedArchetype.Should().Be("new_player");
        result.CategoryStrengths.Should().BeEmpty();
        result.CategoryWeaknesses.Should().BeEmpty();
        result.Signals.Should().BeEmpty();
    }

    [Fact]
    public async Task NullClient_GetRecommendationCandidates_ReturnsEmptyList()
    {
        IPersonalizationSidecarClient client = new NullPersonalizationSidecarClient();

        var result = await client.GetRecommendationCandidatesAsync(
            new SidecarRecommendationRequest(
                "player-1",
                new SidecarPlayerSnapshotDto(0.5m, 0.2m, 0.1m, 0m, "new_player"),
                []));

        result.Should().BeEmpty();
    }

    // ── PersonalizationSidecarClient HTTP mapping ─────────────────────────────

    [Fact]
    public async Task ScorePlayer_MapsHttpResponse_ToDto()
    {
        var responseBody = new
        {
            churnRiskScore = 0.75,
            frustrationRiskScore = 0.50,
            confidenceLevel = 0.60,
            recommendedArchetype = "competitor",
            categoryStrengths = new Dictionary<string, double> { ["math"] = 0.8 },
            categoryWeaknesses = new Dictionary<string, double> { ["science"] = 0.3 },
            signals = new Dictionary<string, object> { ["streakDays"] = 3 }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(responseBody, JsonOpts));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://sidecar-test")
        };

        IPersonalizationSidecarClient client = new PersonalizationSidecarClient(httpClient);

        var result = await client.ScorePlayerAsync(
            new SidecarPlayerScoringRequest(
                "player-1",
                [],
                new SidecarPlayerSnapshotDto(0.5m, 0.2m, 0.1m, 0m, "new_player")));

        result.ChurnRiskScore.Should().Be(0.75m);
        result.FrustrationRiskScore.Should().Be(0.50m);
        result.ConfidenceLevel.Should().Be(0.60m);
        result.RecommendedArchetype.Should().Be("competitor");
        result.CategoryStrengths.Should().ContainKey("math").WhoseValue.Should().Be(0.8m);
        result.CategoryWeaknesses.Should().ContainKey("science").WhoseValue.Should().Be(0.3m);
    }

    [Fact]
    public async Task GetRecommendationCandidates_MapsHttpResponse_ToDto()
    {
        var responseBody = new
        {
            candidates = new[]
            {
                new
                {
                    type = "mission",
                    targetId = "mission-42",
                    score = 0.9,
                    reason = "High engagement match",
                    payload = new Dictionary<string, object> { ["difficulty"] = "medium" }
                }
            }
        };

        var handler = new FakeHttpMessageHandler(
            HttpStatusCode.OK,
            JsonSerializer.Serialize(responseBody, JsonOpts));

        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://sidecar-test")
        };

        IPersonalizationSidecarClient client = new PersonalizationSidecarClient(httpClient);

        var result = await client.GetRecommendationCandidatesAsync(
            new SidecarRecommendationRequest(
                "player-1",
                new SidecarPlayerSnapshotDto(0.5m, 0.2m, 0.1m, 0m, "new_player"),
                []));

        result.Should().HaveCount(1);
        result[0].Type.Should().Be("mission");
        result[0].TargetId.Should().Be("mission-42");
        result[0].Score.Should().Be(0.9m);
        result[0].Reason.Should().Be("High engagement match");
    }

    [Fact]
    public async Task ScorePlayer_ThrowsHttpRequestException_OnNonSuccessStatus()
    {
        var handler = new FakeHttpMessageHandler(HttpStatusCode.InternalServerError, "{}");
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://sidecar-test") };
        IPersonalizationSidecarClient client = new PersonalizationSidecarClient(httpClient);

        var act = () => client.ScorePlayerAsync(
            new SidecarPlayerScoringRequest(
                "player-1",
                [],
                new SidecarPlayerSnapshotDto(0.5m, 0.2m, 0.1m, 0m, "new_player")));

        await act.Should().ThrowAsync<HttpRequestException>();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private sealed class FakeHttpMessageHandler(HttpStatusCode statusCode, string body)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
            });
    }
}
