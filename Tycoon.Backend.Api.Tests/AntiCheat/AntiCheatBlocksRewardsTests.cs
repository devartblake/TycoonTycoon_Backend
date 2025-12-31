using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AntiCheat;

public sealed class AntiCheatBlocksRewardsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AntiCheatBlocksRewardsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task SevereFlag_BlocksRewards()
    {
        var p1 = Guid.NewGuid();

        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "duel"));
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

        var r = await _http.PostAsJsonAsync("/matches/submit", submit);
        r.EnsureSuccessStatusCode();

        var res = await r.Content.ReadFromJsonAsync<SubmitMatchResponse>();
        res!.Status.Should().Be("Rejected");
        res.Awards.Should().BeEmpty();
    }
}
