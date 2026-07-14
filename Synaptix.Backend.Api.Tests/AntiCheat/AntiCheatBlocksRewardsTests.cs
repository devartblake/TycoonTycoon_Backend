using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AntiCheat;

public sealed class AntiCheatBlocksRewardsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public AntiCheatBlocksRewardsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task SevereFlag_BlocksRewards()
    {
        var p1 = Guid.NewGuid();
        _http.AuthenticateAsPlayer(_factory, p1);

        var start = await _http.PostAsJsonAsync("/api/v1/matches/start", new StartMatchRequest(p1, "duel"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        // correct > questionCount triggers AC-001 severe
        var submit = new SubmitMatchRequest(
            EventId: Guid.NewGuid(),
            MatchId: started!.MatchId,
            Mode: "duel",
            Category: "general",
            QuestionCount: 5,
            StartedAtUtc: started.StartedAt,
            EndedAtUtc: DateTimeOffset.UtcNow,
            Status: MatchStatus.Completed,
            Participants: new[]
            {
                new MatchParticipantResultDto(p1, 100, Correct: 99, Wrong: 0, AvgAnswerTimeMs: 500)
            }
        );

        var r = await _http.PostAsJsonAsync("/api/v1/matches/submit", submit);
        r.EnsureSuccessStatusCode();

        var res = await r.Content.ReadFromJsonAsync<SubmitMatchResponse>();
        res!.Status.Should().Be("Rejected");
        res.Awards.Should().BeEmpty();
    }
}
