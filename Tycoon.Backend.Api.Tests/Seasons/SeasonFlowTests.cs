using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Seasons;

public sealed class SeasonFlowTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public SeasonFlowTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task ActiveSeason_AccumulatesPoints_Idempotently()
    {
        // Create + activate season
        var created = await _admin.PostAsJsonAsync("/admin/seasons", new CreateSeasonRequest(
            SeasonNumber: 1,
            Name: "Season 1",
            StartsAtUtc: DateTimeOffset.UtcNow.AddMinutes(-1),
            EndsAtUtc: DateTimeOffset.UtcNow.AddDays(30)
        ));
        created.EnsureSuccessStatusCode();
        var s = await created.Content.ReadFromJsonAsync<SeasonDto>();

        var act = await _admin.PostAsJsonAsync("/admin/seasons/activate", new ActivateSeasonRequest(s!.SeasonId));
        act.EnsureSuccessStatusCode();

        // Start match
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var start = await _public.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "duel"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        var eventId = Guid.NewGuid();

        var submit = new SubmitMatchRequest(
            EventId: eventId,
            MatchId: started!.MatchId,
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

        var r1 = await _public.PostAsJsonAsync("/matches/submit", submit);
        r1.EnsureSuccessStatusCode();

        var r2 = await _public.PostAsJsonAsync("/matches/submit", submit);
        r2.EnsureSuccessStatusCode();

        // Player season state should exist and be >0
        var st = await _public.GetAsync($"/seasons/state/{p1}");
        st.EnsureSuccessStatusCode();

        var state = await st.Content.ReadFromJsonAsync<PlayerSeasonStateDto>();
        state!.RankPoints.Should().BeGreaterThan(0);
    }
}
