using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Moderation;

public sealed class RestrictedPlayerGetsNoAwardsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public RestrictedPlayerGetsNoAwardsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task Submit_Restricted_AppliedButNoAwards()
    {
        var playerId = Guid.NewGuid();
        _public.AuthenticateAsPlayer(_factory, playerId);

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 3, "test", null, null, null)); // 3=Restricted
        set.EnsureSuccessStatusCode();

        var start = await _public.PostAsJsonAsync("/api/v1/matches/start",
            new StartMatchRequest(playerId, "duel"));
        start.EnsureSuccessStatusCode();

        var started = await start.Content.ReadFromJsonAsync<StartMatchResponse>();
        started.Should().NotBeNull();

        var submit = new SubmitMatchRequest(
            EventId: Guid.NewGuid(),
            MatchId: started!.MatchId,
            Mode: "duel",
            Category: "general",
            QuestionCount: 5,
            StartedAtUtc: DateTime.UtcNow,
            EndedAtUtc: DateTimeOffset.UtcNow,
            Status: MatchStatus.Completed,
            Participants: new[]
            {
                new MatchParticipantResultDto(playerId, 10, 1, 4, 300)
            });

        var resp = await _public.PostAsJsonAsync("/api/v1/matches/submit", submit);
        resp.EnsureSuccessStatusCode();

        var res = await resp.Content.ReadFromJsonAsync<SubmitMatchResponse>();
        res!.Status.Should().Be("Applied");
        res.Awards.Should().BeEmpty();
    }
}
