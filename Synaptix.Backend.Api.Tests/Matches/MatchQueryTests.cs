using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Matches;

public sealed class MatchQueryTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public MatchQueryTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatch_Returns_Detail()
    {
        var p1 = Guid.NewGuid();
        _http.AuthenticateAsPlayer(_factory, p1);

        var start = await _http.PostAsJsonAsync("/api/v1/matches/start", new StartMatchRequest(p1, "practice"));
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

        var s = await _http.PostAsJsonAsync("/api/v1/matches/submit", submit);
        s.EnsureSuccessStatusCode();

        var get = await _http.GetAsync($"/api/v1/matches/{matchId}");
        get.EnsureSuccessStatusCode();

        var detail = await get.Content.ReadFromJsonAsync<MatchDetailDto>(TestJson.Default);
        detail!.MatchId.Should().Be(matchId);
        detail.Category.Should().Be("science");
        detail.Participants.Should().HaveCount(1);
    }
}
