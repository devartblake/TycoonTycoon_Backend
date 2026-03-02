using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Moderation;

public sealed class PartyEnqueueModerationContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public PartyEnqueueModerationContractTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task PartyEnqueue_BannedLeader_ReturnsForbiddenEnvelope()
    {
        var leader = Guid.NewGuid();
        var mate = Guid.NewGuid();

        await MakeFriendsAsync(leader, mate);
        var party = await CreatePartyWithMemberAsync(leader, mate);

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(leader, 4, "test", null, null, null));
        set.EnsureSuccessStatusCode();

        var resp = await _public.PostAsJsonAsync($"/party/{party.PartyId}/enqueue",
            new PartyEnqueueBody(leader, "ranked", 1));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    private sealed record PartyEnqueueBody(Guid LeaderPlayerId, string Mode, int Tier);

    private async Task MakeFriendsAsync(Guid from, Guid to)
    {
        var send = await _public.PostAsJsonAsync("/friends/request", new { FromPlayerId = from, ToPlayerId = to });
        send.EnsureSuccessStatusCode();
        var req = await send.Content.ReadFromJsonAsync<FriendRequestDto>();
        req.Should().NotBeNull();

        if (req!.RequestId == Guid.Empty || req.Status == "Accepted")
            return;

        var accept = await _public.PostAsJsonAsync($"/friends/request/{req.RequestId}/accept", new { PlayerId = to });
        accept.EnsureSuccessStatusCode();
    }

    private async Task<PartyRosterDto> CreatePartyWithMemberAsync(Guid leader, Guid mate)
    {
        var created = await _public.PostAsJsonAsync("/party", new { LeaderPlayerId = leader });
        created.EnsureSuccessStatusCode();
        var roster = await created.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster.Should().NotBeNull();

        var inv = await _public.PostAsJsonAsync($"/party/{roster!.PartyId}/invite", new { FromPlayerId = leader, ToPlayerId = mate });
        inv.EnsureSuccessStatusCode();
        var invite = await inv.Content.ReadFromJsonAsync<PartyInviteDto>();
        invite.Should().NotBeNull();

        var acc = await _public.PostAsJsonAsync($"/party/invites/{invite!.InviteId}/accept", new { PlayerId = mate });
        acc.EnsureSuccessStatusCode();

        var refreshed = await _public.GetAsync($"/party/{roster.PartyId}");
        refreshed.EnsureSuccessStatusCode();
        var roster2 = await refreshed.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster2.Should().NotBeNull();
        roster2!.Members.Should().HaveCount(2);

        return roster2;
    }
}
