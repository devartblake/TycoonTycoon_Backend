using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Moderation;

public sealed class BannedPlayerCannotStartTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public BannedPlayerCannotStartTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }


    [Fact]
    public async Task SetStatus_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(Guid.NewGuid(), 4, "test", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SetStatus_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(Guid.NewGuid(), 4, "test", null, null, null));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task MobileStartMatch_Banned_Is403Envelope()
    {
        var playerId = Guid.NewGuid();

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "test", null, null, null));
        set.EnsureSuccessStatusCode();

        var start = await _public.PostAsJsonAsync("/api/v1/mobile/matches/start",
            new StartMatchRequest(playerId, "duel"));

        start.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await start.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task StartMatch_Banned_Is403()
    {
        var playerId = Guid.NewGuid();

        var set = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "test", null, null, null)); // 4 = Banned

        set.EnsureSuccessStatusCode();

        _public.AuthenticateAsPlayer(_factory, playerId);
        var start = await _public.PostAsJsonAsync("/api/v1/matches/start",
            new StartMatchRequest(playerId, "duel"));

        start.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await start.HasErrorCodeAsync("FORBIDDEN");
    }
}
