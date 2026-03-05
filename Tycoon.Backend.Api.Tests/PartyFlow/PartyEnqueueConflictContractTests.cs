using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Party;

public sealed class PartyEnqueueConflictContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public PartyEnqueueConflictContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task PartyEnqueue_WithNonLeaderActor_ReturnsConflictEnvelope()
    {
        var leader = Guid.NewGuid();
        var mate = Guid.NewGuid();
        var outsider = Guid.NewGuid();

        await MakeFriendsAsync(leader, mate);

        var created = await _http.PostAsJsonAsync("/party", new { LeaderPlayerId = leader });
        created.EnsureSuccessStatusCode();
        var roster = await created.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster.Should().NotBeNull();

        var inv = await _http.PostAsJsonAsync($"/party/{roster!.PartyId}/invite", new { FromPlayerId = leader, ToPlayerId = mate });
        inv.EnsureSuccessStatusCode();
        var invite = await inv.Content.ReadFromJsonAsync<PartyInviteDto>();
        invite.Should().NotBeNull();

        var acc = await _http.PostAsJsonAsync($"/party/invites/{invite!.InviteId}/accept", new { PlayerId = mate });
        acc.EnsureSuccessStatusCode();

        var enqueue = await _http.PostAsJsonAsync($"/party/{roster.PartyId}/enqueue", new { LeaderPlayerId = outsider, Mode = "ranked", Tier = 1 });

        enqueue.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await enqueue.HasErrorCodeAsync("CONFLICT");
    }

    private async Task MakeFriendsAsync(Guid from, Guid to)
    {
        var send = await _http.PostAsJsonAsync("/friends/request", new { FromPlayerId = from, ToPlayerId = to });
        send.EnsureSuccessStatusCode();
        var req = await send.Content.ReadFromJsonAsync<FriendRequestDto>();
        req.Should().NotBeNull();

        if (req!.RequestId == Guid.Empty || req.Status == "Accepted")
            return;

        var accept = await _http.PostAsJsonAsync($"/friends/request/{req.RequestId}/accept", new { PlayerId = to });
        accept.EnsureSuccessStatusCode();
    }
}
