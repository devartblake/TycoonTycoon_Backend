using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Moderation;

public sealed class BannedPlayerCannotStartTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public BannedPlayerCannotStartTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }


    [Fact]
    public async Task SetStatus_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(Guid.NewGuid(), 4, "test", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SetStatus_Requires_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();

        var resp = await noKey.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(Guid.NewGuid(), 4, "test", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task StartMatch_Banned_Is403()
    {
        var playerId = Guid.NewGuid();

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "test", null, null, null)); // 4 = Banned

        set.EnsureSuccessStatusCode();

        var start = await _public.PostAsJsonAsync("/matches/start",
            new StartMatchRequest(playerId, "duel"));

        start.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
