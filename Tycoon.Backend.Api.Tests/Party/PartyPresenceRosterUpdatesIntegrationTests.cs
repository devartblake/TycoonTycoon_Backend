using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.SignalR.Client;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Party;

public sealed class PartyPresenceRosterUpdatesIntegrationTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public PartyPresenceRosterUpdatesIntegrationTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task PartyRosterUpdated_IncludesRealOnlinePlayerIds_OnInviteAccepted()
    {
        // Arrange: leader creates party, invites mate. Only mate connects to SignalR.
        var leader = Guid.NewGuid();
        var mate = Guid.NewGuid();

        await MakeFriendsAsync(leader, mate);

        // Create party (leader only)
        var created = await _http.PostAsJsonAsync("/party", new { LeaderPlayerId = leader });
        created.EnsureSuccessStatusCode();
        var roster1 = await created.Content.ReadFromJsonAsync<PartyRosterDto>();
        roster1.Should().NotBeNull();
        roster1!.Members.Should().HaveCount(1);
        roster1.Members[0].PlayerId.Should().Be(leader);

        // Invite mate (still not accepted)
        var inv = await _http.PostAsJsonAsync($"/party/{roster1.PartyId}/invite", new { FromPlayerId = leader, ToPlayerId = mate });
        inv.EnsureSuccessStatusCode();
        var invite = await inv.Content.ReadFromJsonAsync<PartyInviteDto>();
        invite.Should().NotBeNull();
        invite!.Status.Should().Be("Pending");

        // Connect ONLY mate to realtime, and listen for party.roster.updated
        var rosterUpdated = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var mateConn = await ConnectMatchHubAsync(mate, payload => rosterUpdated.TrySetResult(payload));

        // Act: mate accepts invite (should trigger party.roster.updated to party members via player groups)
        var acc = await _http.PostAsJsonAsync($"/party/invites/{invite.InviteId}/accept", new { PlayerId = mate });
        acc.EnsureSuccessStatusCode();

        // Assert: roster.updated payload includes mate as online, leader as offline (since leader not connected)
        var payload = await WaitAsync(rosterUpdated.Task, seconds: 6);

        payload.TryGetProperty("Roster", out var rosterEl).Should().BeTrue();
        payload.TryGetProperty("OnlinePlayerIds", out var onlineEl).Should().BeTrue();

        // OnlinePlayerIds should contain mate only
        onlineEl.ValueKind.Should().Be(JsonValueKind.Array);

        var online = onlineEl.EnumerateArray()
            .Select(x => x.GetGuid())
            .ToList();

        online.Should().Contain(mate);
        online.Should().NotContain(leader);

        // Roster should now show 2 members
        rosterEl.TryGetProperty("Members", out var membersEl).Should().BeTrue();
        membersEl.ValueKind.Should().Be(JsonValueKind.Array);

        var memberIds = membersEl.EnumerateArray()
            .Select(m => m.GetProperty("PlayerId").GetGuid())
            .ToList();

        memberIds.Should().Contain(new[] { leader, mate });
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

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
        var accepted = await accept.Content.ReadFromJsonAsync<FriendRequestDto>();
        accepted!.Status.Should().Be("Accepted");
    }

    private async Task<HubConnection> ConnectMatchHubAsync(Guid playerId, Action<JsonElement> onRosterUpdated)
    {
        var baseUri = _http.BaseAddress ?? new Uri("http://localhost");
        var hubUrl = new Uri(baseUri, $"/ws/match?playerId={playerId}");

        var conn = new HubConnectionBuilder()
            .WithUrl(hubUrl, opts =>
            {
                opts.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .WithAutomaticReconnect()
            .Build();

        conn.On<JsonElement>("party.roster.updated", payload => onRosterUpdated(payload));

        await conn.StartAsync();
        return conn;
    }

    private static async Task<T> WaitAsync<T>(Task<T> task, int seconds)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        var completed = await Task.WhenAny(task, Task.Delay(Timeout.Infinite, cts.Token));
        if (completed != task)
            throw new TimeoutException("Timed out waiting for realtime roster update.");
        return await task;
    }
}
