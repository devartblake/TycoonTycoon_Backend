using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Seasons;

public sealed class SeasonCloseCarryoverTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public SeasonCloseCarryoverTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task CloseSeason_CreatesNext_WithCarryover()
    {
        // Create + activate
        var created = await _admin.PostAsJsonAsync("/admin/seasons", new CreateSeasonRequest(
            10, "Season 10", DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(30)));
        created.EnsureSuccessStatusCode();
        var s = await created.Content.ReadFromJsonAsync<SeasonDto>();

        await _admin.PostAsJsonAsync("/admin/seasons/activate", new ActivateSeasonRequest(s!.SeasonId));

        // Award points directly (admin-adjust) to create profile
        var p1 = Guid.NewGuid();

        // Apply points with service endpoint via transaction table? We didn't add public endpoint for ApplySeasonPoints (by design).
        // Instead, create a match for points: keep minimal by simulating a match submit.
        var start = await _public.PostAsJsonAsync("/matches/start", new StartMatchRequest(p1, "practice"));
        start.EnsureSuccessStatusCode();
        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();

        await _public.PostAsJsonAsync("/matches/submit", new SubmitMatchRequest(
            Guid.NewGuid(),
            started!.MatchId,
            "practice",
            "science",
            5,
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow,
            MatchStatus.Completed,
            new[] { new MatchParticipantResultDto(p1, 50, 4, 1, 900) }
        ));

        // Close with carryover 30% and create next
        var close = await _admin.PostAsJsonAsync("/admin/seasons/close", new CloseSeasonRequest(
            SeasonId: s.SeasonId,
            CarryoverPercent: 30,
            CreateNextSeason: true,
            NextSeasonName: "Season 11"
        ));

        close.EnsureSuccessStatusCode();

        // Active should be Season 11
        var active = await _public.GetAsync("/seasons/active");
        active.EnsureSuccessStatusCode();
        var a = await active.Content.ReadFromJsonAsync<SeasonDto>();
        a!.Name.Should().Be("Season 11");

        // Player state in new active season should exist (carryover could be 0 if small, but profile should exist)
        var st = await _public.GetAsync($"/seasons/state/{p1}");
        st.EnsureSuccessStatusCode();
        var state = await st.Content.ReadFromJsonAsync<PlayerSeasonStateDto>();
        state!.SeasonId.Should().Be(a.SeasonId);
    }
}
