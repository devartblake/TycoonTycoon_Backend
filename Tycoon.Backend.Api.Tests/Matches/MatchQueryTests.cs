using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Matches;

public sealed class MatchQueryTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MatchQueryTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatch_Returns_Detail()
    {
        var p1 = Guid.NewGuid();

        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "practice"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        var matchId = started!.MatchId;

        var submit = new SubmitMatchRequest(
            EventId: Guid.NewGuid(),
            MatchId: matchId,
            Mode: "duel",
            Category: "science",
            QuestionCount: 5,
            StartedAtUtc: started.StartedAt,
            EndedAtUtc: DateTimeOffset.UtcNow,
            Status: MatchStatus.Completed,
            Participants: new[]
            {
                new MatchParticipantResultDto(p1, 50, 4, 1, 900)
            }
        );

        var s = await _http.PostAsJsonAsync("/matches/submit", submit);
        s.EnsureSuccessStatusCode();

        var get = await _http.GetAsync($"/matches/{matchId}");
        get.EnsureSuccessStatusCode();

        var detail = await get.Content.ReadFromJsonAsync<MatchDetailDto>();
        detail!.MatchId.Should().Be(matchId);
        detail.Category.Should().Be("science");
        detail.Participants.Should().HaveCount(1);
    }
}
