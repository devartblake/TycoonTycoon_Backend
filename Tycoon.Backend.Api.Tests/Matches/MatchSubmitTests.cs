using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Matches;

public sealed class MatchSubmitTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;
    private readonly HttpClient _admin;

    public MatchSubmitTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Submit_IsIdempotent_And_AwardsOnce()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        // Start match (host = p1)
        var start = await _http.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "duel"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        var matchId = started!.MatchId;
        var eventId = Guid.NewGuid();

        var req = new SubmitMatchRequest(
            EventId: eventId,
            MatchId: matchId,
            Mode: "duel",
            Category: "general",
            QuestionCount: 10,
            StartedAtUtc: started.StartedAt,
            EndedAtUtc: DateTimeOffset.UtcNow,
            Status: MatchStatus.Completed,
            Participants: new[]
            {
                new MatchParticipantResultDto(p1, 100, 8, 2, 1200),
                new MatchParticipantResultDto(p2,  80, 6, 4, 1500),
            }
        );

        var r1 = await _http.PostAsJsonAsync("/matches/submit", req);
        r1.EnsureSuccessStatusCode();
        var res1 = await r1.Content.ReadFromJsonAsync<SubmitMatchResponse>();
        res1!.Status.Should().Be("Applied");
        res1.Awards.Should().HaveCount(2);

        var r2 = await _http.PostAsJsonAsync("/matches/submit", req);
        r2.EnsureSuccessStatusCode();
        var res2 = await r2.Content.ReadFromJsonAsync<SubmitMatchResponse>();
        res2!.Status.Should().Be("Duplicate");

        // Verify economy history includes match-complete for host (best-effort)
        var hist = await _admin.GetAsync($"/admin/economy/history/{p1}?page=1&pageSize=50");
        hist.EnsureSuccessStatusCode();
        var dto = await hist.Content.ReadFromJsonAsync<EconomyHistoryDto>();

        dto!.Items.Any(x => x.Kind == "match-complete").Should().BeTrue();
    }
}
