using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Analytics.Models;

namespace Synaptix.Backend.Api.Tests.Analytics;

public sealed class AnalyticsCompatibilityEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AnalyticsCompatibilityEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Track_QuestionAnsweredEvent_PersistsAndReturnsAccepted()
    {
        var http = _factory.CreateClient();
        var timestamp = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var playerId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        var resp = await http.PostAsJsonAsync("/analytics/track", new
        {
            userId = playerId.ToString(),
            eventName = "question_answered",
            timestamp,
            properties = new
            {
                playerId,
                matchId,
                questionId = "q-track-1",
                mode = "ranked",
                category = "history",
                difficulty = 2,
                isCorrect = true,
                answerTimeMs = 950,
                pointsAwarded = 30
            }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await resp.Content.ReadFromJsonAsync<AnalyticsIngestResponse>();
        payload.Should().NotBeNull();
        payload!.Accepted.Should().Be(1);
        payload.Skipped.Should().Be(0);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDb>();
        db.QuestionAnsweredAnalyticsEvents.Should().ContainSingle(x =>
            x.PlayerId == playerId &&
            x.MatchId == matchId &&
            x.QuestionId == "q-track-1" &&
            x.AnsweredAtUtc == timestamp);
    }

    [Fact]
    public async Task Track_UnsupportedEvent_ReturnsAcceptedAsSkipped()
    {
        var http = _factory.CreateClient();

        var resp = await http.PostAsJsonAsync("/analytics/track", new
        {
            userId = Guid.NewGuid().ToString(),
            eventName = "app_opened",
            timestamp = DateTime.UtcNow,
            properties = new
            {
                platform = "web"
            }
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await resp.Content.ReadFromJsonAsync<AnalyticsIngestResponse>();
        payload.Should().NotBeNull();
        payload!.Accepted.Should().Be(0);
        payload.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task SessionStart_RootAnalyticsRoute_ReturnsAcceptedAsSkipped()
    {
        var http = _factory.CreateClient();

        var resp = await http.PostAsJsonAsync("/analytics/session_start", new
        {
            userId = Guid.NewGuid().ToString(),
            startedAtUtc = DateTime.UtcNow
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await resp.Content.ReadFromJsonAsync<AnalyticsIngestResponse>();
        payload.Should().NotBeNull();
        payload!.Accepted.Should().Be(0);
        payload.Skipped.Should().Be(1);
    }

    [Fact]
    public async Task SessionStart_V1AnalyticsRoute_ReturnsAcceptedAsSkipped()
    {
        var http = _factory.CreateClient();

        var resp = await http.PostAsJsonAsync("/api/v1/analytics/session_start", new
        {
            userId = Guid.NewGuid().ToString(),
            startedAtUtc = DateTime.UtcNow
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);

        var payload = await resp.Content.ReadFromJsonAsync<AnalyticsIngestResponse>();
        payload.Should().NotBeNull();
        payload!.Accepted.Should().Be(0);
        payload.Skipped.Should().Be(1);
    }

    private sealed class AnalyticsIngestResponse
    {
        public int Accepted { get; set; }
        public int Skipped { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
