using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminPowerups;

public sealed class AdminPowerupsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminPowerupsEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Fact]
    public async Task Grant_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();
        var resp = await noKey.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(Guid.NewGuid()));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Grant_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");
        var resp = await wrongKey.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(Guid.NewGuid()));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task State_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();
        var resp = await noKey.GetAsync($"/admin/powerups/state/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task State_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");
        var resp = await wrongKey.GetAsync($"/admin/powerups/state/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task State_For_Unknown_Player_Returns_Empty_Powerups()
    {
        var playerId = Guid.NewGuid();

        var resp = await _http.GetAsync($"/admin/powerups/state/{playerId}");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var dto = await resp.Content.ReadFromJsonAsync<PowerupStateDto>(TestJson.Default);
        dto.Should().NotBeNull();
        dto!.PlayerId.Should().Be(playerId);
        dto.Powerups.Should().BeEmpty();
    }

    [Fact]
    public async Task Grant_Succeeds_And_State_Reflects_Balance()
    {
        var playerId = Guid.NewGuid();
        var req = BuildGrantRequest(playerId, PowerupType.Skip, quantity: 3);

        var grantResp = await _http.PostAsJsonAsync("/admin/powerups/grant", req);
        grantResp.IsSuccessStatusCode.Should().BeTrue();

        var stateResp = await _http.GetAsync($"/admin/powerups/state/{playerId}");
        stateResp.IsSuccessStatusCode.Should().BeTrue();

        var state = await stateResp.Content.ReadFromJsonAsync<PowerupStateDto>(TestJson.Default);
        state!.PlayerId.Should().Be(playerId);
        state.Powerups.Should().Contain(p => p.Type == PowerupType.Skip && p.Quantity >= 3);
    }

    [Fact]
    public async Task Grant_Multiple_Types_Are_Tracked_Independently()
    {
        var playerId = Guid.NewGuid();

        await _http.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(playerId, PowerupType.FiftyFifty, 2));
        await _http.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(playerId, PowerupType.DoublePoints, 1));

        var resp = await _http.GetAsync($"/admin/powerups/state/{playerId}");
        resp.IsSuccessStatusCode.Should().BeTrue();

        var state = await resp.Content.ReadFromJsonAsync<PowerupStateDto>(TestJson.Default);
        state!.Powerups.Should().Contain(p => p.Type == PowerupType.FiftyFifty && p.Quantity >= 2);
        state.Powerups.Should().Contain(p => p.Type == PowerupType.DoublePoints && p.Quantity >= 1);
    }

    [Fact]
    public async Task Grant_Accumulates_Quantity_For_Same_Type()
    {
        var playerId = Guid.NewGuid();

        await _http.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(playerId, PowerupType.ExtraTime, 2));
        await _http.PostAsJsonAsync("/admin/powerups/grant", BuildGrantRequest(playerId, PowerupType.ExtraTime, 3));

        var resp = await _http.GetAsync($"/admin/powerups/state/{playerId}");
        var state = await resp.Content.ReadFromJsonAsync<PowerupStateDto>(TestJson.Default);

        state!.Powerups.Should().Contain(p => p.Type == PowerupType.ExtraTime && p.Quantity >= 5);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static GrantPowerupRequest BuildGrantRequest(
        Guid playerId,
        PowerupType type = PowerupType.Skip,
        int quantity = 1) =>
        new(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            Type: type,
            Quantity: quantity,
            Reason: "admin-test"
        );
}
