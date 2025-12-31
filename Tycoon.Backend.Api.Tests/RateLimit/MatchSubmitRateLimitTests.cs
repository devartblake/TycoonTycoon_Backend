using System.Net;
using System.Net.Http.Json;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.RateLimit;

public sealed class MatchSubmitRateLimitTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MatchSubmitRateLimitTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Submit_IsRateLimited()
    {
        var p1 = Guid.NewGuid();

        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "duel"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        // Fire more than 10 submits within window (note: idempotency may respond Duplicate, but limiter should hit)
        for (int i = 0; i < 30; i++)
        {
            var submit = new SubmitMatchRequest(
                EventId: Guid.NewGuid(),
                MatchId: started!.MatchId,
                Mode: "duel",
                Category: "general",
                QuestionCount: 5,
                StartedAtUtc: started.StartedAt,
                EndedAtUtc: DateTimeOffset.UtcNow,
                Status: MatchStatus.Completed,
                Participants: new[] { new MatchParticipantResultDto(p1, 10, 1, 4, 300) }
            );

            var resp = await _http.PostAsJsonAsync("/matches/submit", submit);

            if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                return;
        }

        Assert.True(false, "Expected at least one 429 TooManyRequests response.");
    }
}
