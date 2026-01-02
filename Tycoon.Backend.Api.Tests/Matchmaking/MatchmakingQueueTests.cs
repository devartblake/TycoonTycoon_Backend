using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Matchmaking;

public sealed class MatchmakingQueueTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _client;

    public MatchmakingQueueTests(TycoonApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    private sealed record EnqueueRequest(Guid PlayerId, string Mode, int Tier);
    private sealed record QueueResultDto(string Status, Guid? TicketId, Guid? MatchId, Guid? OpponentId);

    [Fact]
    public async Task Enqueue_Twice_IsIdempotent_ReturnsSameTicket()
    {
        var playerId = Guid.NewGuid();

        var req = new EnqueueRequest(playerId, "ranked", 1);

        // First enqueue
        var r1 = await _client.PostAsJsonAsync("/matchmaking/enqueue", req);
        r1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);

        var body1 = await r1.Content.ReadFromJsonAsync<QueueResultDto>();
        body1.Should().NotBeNull();
        body1!.Status.Should().Be("Queued");
        body1.TicketId.Should().NotBeNull();

        // Second enqueue should return same queued ticket
        var r2 = await _client.PostAsJsonAsync("/matchmaking/enqueue", req);
        r2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);

        var body2 = await r2.Content.ReadFromJsonAsync<QueueResultDto>();
        body2.Should().NotBeNull();
        body2!.Status.Should().Be("Queued");
        body2.TicketId.Should().Be(body1.TicketId);
    }

    [Fact]
    public async Task TwoPlayers_Enqueue_ShouldMatch()
    {
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();

        var req1 = new EnqueueRequest(p1, "ranked", 1);
        var req2 = new EnqueueRequest(p2, "ranked", 1);

        // Player 1 enqueues (likely queued)
        var r1 = await _client.PostAsJsonAsync("/matchmaking/enqueue", req1);
        r1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
        var b1 = await r1.Content.ReadFromJsonAsync<QueueResultDto>();
        b1.Should().NotBeNull();

        // Player 2 enqueues (should match or queue briefly)
        var r2 = await _client.PostAsJsonAsync("/matchmaking/enqueue", req2);
        r2.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);
        var b2 = await r2.Content.ReadFromJsonAsync<QueueResultDto>();
        b2.Should().NotBeNull();

        // One of them should get Matched immediately; if not, poll status once.
        if (b1!.Status != "Matched" && b2!.Status != "Matched")
        {
            var s1 = await _client.GetAsync($"/matchmaking/status/{p1}");
            s1.StatusCode.Should().Be(HttpStatusCode.OK);
            b1 = await s1.Content.ReadFromJsonAsync<QueueResultDto>();

            var s2 = await _client.GetAsync($"/matchmaking/status/{p2}");
            s2.StatusCode.Should().Be(HttpStatusCode.OK);
            b2 = await s2.Content.ReadFromJsonAsync<QueueResultDto>();
        }

        (b1!.Status == "Matched" || b2!.Status == "Matched").Should().BeTrue();
    }

    [Fact]
    public async Task Cancel_RemovesQueuedTicket()
    {
        var playerId = Guid.NewGuid();
        var req = new EnqueueRequest(playerId, "ranked", 1);

        var r1 = await _client.PostAsJsonAsync("/matchmaking/enqueue", req);
        r1.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Accepted);

        // cancel
        var cancel = await _client.PostAsJsonAsync("/matchmaking/cancel", new { PlayerId = playerId });
        cancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // status should be None or not Queued
        var status = await _client.GetAsync($"/matchmaking/status/{playerId}");
        status.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await status.Content.ReadFromJsonAsync<QueueResultDto>();
        body.Should().NotBeNull();
        body!.Status.Should().NotBe("Queued");
    }
}
