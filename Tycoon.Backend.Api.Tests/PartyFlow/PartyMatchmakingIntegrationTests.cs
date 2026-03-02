using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Party;

public sealed class PartyMatchmakingIntegrationTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;
    private readonly HttpClient _admin;

    public PartyMatchmakingIntegrationTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task PartyMatchmaking_MatchesTwoParties_And_NotifiesMembers()
    {
        // Players
        var aLeader = Guid.NewGuid();
        var aMate = Guid.NewGuid();
        var bLeader = Guid.NewGuid();
        var bMate = Guid.NewGuid();

        // Friend edges are required for invites (leader -> invitee).
        await MakeFriendsAsync(aLeader, aMate);
        await MakeFriendsAsync(bLeader, bMate);

        // Create Party A and Party B
        var partyA = await CreatePartyWithMemberAsync(aLeader, aMate);
        var partyB = await CreatePartyWithMemberAsync(bLeader, bMate);

        // Connect realtime listeners for Party A members (both should receive party.matched)
        var aLeaderMatched = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var aMateMatched = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);

        await using var connLeader = await ConnectMatchHubAsync(aLeader, payload => aLeaderMatched.TrySetResult(payload));
        await using var connMate = await ConnectMatchHubAsync(aMate, payload => aMateMatched.TrySetResult(payload));

        // Enqueue Party A -> should queue
        var q1 = await _http.PostAsJsonAsync($"/party/{partyA.PartyId}/enqueue",
            new PartyEnqueueBody(aLeader, "ranked", 1));

        q1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var qr1 = await q1.Content.ReadFromJsonAsync<PartyQueueResultDto>();
        qr1!.Status.Should().Be("Queued");
        qr1.PartyId.Should().Be(partyA.PartyId);
        qr1.TicketId.Should().NotBeNull();

        // Enqueue Party B -> should match and respond OK
        var q2 = await _http.PostAsJsonAsync($"/party/{partyB.PartyId}/enqueue",
            new PartyEnqueueBody(bLeader, "ranked", 1));

        q2.StatusCode.Should().Be(HttpStatusCode.OK);
        var qr2 = await q2.Content.ReadFromJsonAsync<PartyQueueResultDto>();
        qr2!.Status.Should().Be("Matched");
        qr2.PartyId.Should().Be(partyB.PartyId);
        qr2.OpponentPartyId.Should().Be(partyA.PartyId);

        // Realtime: both Party A members should receive "party.matched"
        var leaderPayload = await WaitAsync(aLeaderMatched.Task, seconds: 6);
        var matePayload = await WaitAsync(aMateMatched.Task, seconds: 6);

        AssertPartyMatchedPayload(leaderPayload, expectedPartyId: partyA.PartyId, expectedOpponentPartyId: partyB.PartyId);
        AssertPartyMatchedPayload(matePayload, expectedPartyId: partyA.PartyId, expectedOpponentPartyId: partyB.PartyId);
    }

    [Fact]
    public async Task PartyMatchmaking_EnqueueIsIdempotent_WhenAlreadyQueued()
    {
        var leader = Guid.NewGuid();
        var mate = Guid.NewGuid();

        await MakeFriendsAsync(leader, mate);

        var party = await CreatePartyWithMemberAsync(leader, mate);

        // 1st enqueue => queued
        var r1 = await _http.PostAsJsonAsync($"/party/{party.PartyId}/enqueue",
            new PartyEnqueueBody(leader, "ranked", 1));

        r1.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var q1 = await r1.Content.ReadFromJsonAsync<PartyQueueResultDto>();
        q1!.Status.Should().Be("Queued");
        q1.TicketId.Should().NotBeNull();

        // 2nd enqueue => still queued; should return the same queued ticket (or at least a queued ticket)
        var r2 = await _http.PostAsJsonAsync($"/party/{party.PartyId}/enqueue",
            new PartyEnqueueBody(leader, "ranked", 1));

        r2.StatusCode.Should().Be(HttpStatusCode.Accepted);
        var q2 = await r2.Content.ReadFromJsonAsync<PartyQueueResultDto>();
        q2!.Status.Should().Be("Queued");
        q2.TicketId.Should().NotBeNull();

        // Preferably same ticket id; acceptable if implementation returns the same queued ticket.
        q2.TicketId.Should().Be(q1.TicketId);
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

        var resp = await _http.PostAsJsonAsync($"/party/{party.PartyId}/enqueue",
            new PartyEnqueueBody(leader, "ranked", 1));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    // ---------------------------------------------------------------------
    // Helpers (local to test)
    // ---------------------------------------------------------------------

    private sealed record PartyEnqueueBody(Guid LeaderPlayerId, string Mode, int Tier);

    private static void AssertPartyMatchedPayload(JsonElement payload, Guid expectedPartyId, Guid expectedOpponentPartyId)
    {
        payload.ValueKind.Should().Be(JsonValueKind.Object);

        payload.TryGetProperty("PartyId", out var partyIdEl).Should().BeTrue();
        payload.TryGetProperty("OpponentPartyId", out var oppIdEl).Should().BeTrue();

        var partyId = partyIdEl.GetGuid();
        var oppId = oppIdEl.GetGuid();

        partyId.Should().Be(expectedPartyId);
        oppId.Should().Be(expectedOpponentPartyId);

        payload.TryGetProperty("Mode", out _).Should().BeTrue();
        payload.TryGetProperty("Scope", out _).Should().BeTrue();
        payload.TryGetProperty("TicketId", out _).Should().BeTrue();
    }

    private async Task MakeFriendsAsync(Guid from, Guid to)
    {
        // Send request
        var send = await _http.PostAsJsonAsync("/friends/request", new { FromPlayerId = from, ToPlayerId = to });
        send.EnsureSuccessStatusCode();
        var req = await send.Content.ReadFromJsonAsync<FriendRequestDto>();
        req.Should().NotBeNull();

        // If API returned synthetic accepted (RequestId == Guid.Empty), we’re done
        if (req!.RequestId == Guid.Empty || req.Status == "Accepted")
            return;

        // Accept (recipient must accept)
        var accept = await _http.PostAsJsonAsync($"/friends/request/{req.RequestId}/accept", new { PlayerId = to });
        accept.EnsureSuccessStatusCode();
        var accepted = await accept.Content.ReadFromJsonAsync<FriendRequestDto>();
        accepted!.Status.Should().Be("Accepted");
    }

    private async Task<PartyRosterDto> CreatePartyWithMemberAsync(Guid leader, Guid mate)
    {
        // Create party
        var created = await _http.PostAsJsonAsync("/party", new { LeaderPlayerId = leader });
        created.EnsureSuccessStatusCode();
        var roster = await created.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster.Should().NotBeNull();
        roster!.LeaderPlayerId.Should().Be(leader);

        // Invite mate
        var inv = await _http.PostAsJsonAsync($"/party/{roster.PartyId}/invite", new { FromPlayerId = leader, ToPlayerId = mate });
        inv.EnsureSuccessStatusCode();
        var invite = await inv.Content.ReadFromJsonAsync<PartyInviteDto>();
        invite.Should().NotBeNull();
        invite!.Status.Should().Be("Pending");

        // Accept invite
        var acc = await _http.PostAsJsonAsync($"/party/invites/{invite.InviteId}/accept", new { PlayerId = mate });
        acc.EnsureSuccessStatusCode();
        var accepted = await acc.Content.ReadFromJsonAsync<PartyInviteDto>();
        accepted!.Status.Should().Be("Accepted");

        // Confirm roster now has 2 members
        var refreshed = await _http.GetAsync($"/party/{roster.PartyId}");
        refreshed.EnsureSuccessStatusCode();
        var roster2 = await refreshed.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster2!.Members.Should().HaveCount(2);

        return roster2;
    }

    private async Task<HubConnection> ConnectMatchHubAsync(Guid playerId, Action<JsonElement> onPartyMatched)
    {
        // Use the TestServer handler so SignalR connects to the in-memory host.
        var baseUri = _http.BaseAddress ?? new Uri("http://localhost");
        var hubUrl = new Uri(baseUri, $"/ws/match?playerId={playerId}");

        var conn = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                // Critical: route SignalR traffic through WebApplicationFactory's TestServer handler
                opts.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        conn.On<JsonElement>("party.matched", payload => onPartyMatched(payload));

        await conn.StartAsync();
        return conn;
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, int seconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        if (completed != task)
            throw new TimeoutException("Timed out waiting for realtime notification.");
        return await task;
    }
}
