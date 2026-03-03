using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Moderation;

public sealed class MatchmakingEnqueueModerationContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public MatchmakingEnqueueModerationContractTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task MatchmakingEnqueue_Banned_Is403Envelope()
    {
        var playerId = Guid.NewGuid();

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "test", null, null, null));
        set.EnsureSuccessStatusCode();

        var enqueue = await _public.PostAsJsonAsync("/matchmaking/enqueue", new { PlayerId = playerId, Mode = "duel", Tier = 1 });
        enqueue.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await enqueue.HasErrorCodeAsync("FORBIDDEN");
    }
}
