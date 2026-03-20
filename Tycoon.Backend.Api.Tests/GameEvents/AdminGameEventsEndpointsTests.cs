using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.GameEvents;

public sealed class AdminGameEventsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminGameEventsEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Fact]
    public async Task Create_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();
        var req = BuildCreateRequest();

        var resp = await noKey.PostAsJsonAsync("/admin/game-events/", req);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Create_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");
        var req = BuildCreateRequest();

        var resp = await wrongKey.PostAsJsonAsync("/admin/game-events/", req);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Open_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.PostAsync($"/admin/game-events/{Guid.NewGuid()}/open", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Start_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.PostAsync($"/admin/game-events/{Guid.NewGuid()}/start", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Close_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.PostAsync($"/admin/game-events/{Guid.NewGuid()}/close", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_Returns_GameEventSummary()
    {
        var req = BuildCreateRequest();

        var resp = await _http.PostAsJsonAsync("/admin/game-events/", req);

        resp.IsSuccessStatusCode.Should().BeTrue();

        var summary = await resp.Content.ReadFromJsonAsync<GameEventSummaryDto>();
        summary.Should().NotBeNull();
        summary!.Id.Should().NotBeEmpty();
        summary.Kind.Should().Be(req.Kind);
        summary.TierId.Should().Be(req.TierId);
        summary.Status.Should().Be(GameEventStatus.Scheduled);
        summary.EntryFeeCoins.Should().Be(req.EntryFeeCoins);
        summary.MaxParticipants.Should().Be(req.MaxParticipants);
    }

    [Fact]
    public async Task Create_Then_Open_Transitions_Status_To_Open()
    {
        var eventId = await CreateEventAsync();

        var resp = await _http.PostAsync($"/admin/game-events/{eventId}/open", null);

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Open");
    }

    [Fact]
    public async Task Create_Then_Open_Then_Start_Transitions_Status_To_Live()
    {
        var eventId = await CreateEventAsync();

        var openResp = await _http.PostAsync($"/admin/game-events/{eventId}/open", null);
        openResp.IsSuccessStatusCode.Should().BeTrue();

        var startResp = await _http.PostAsync($"/admin/game-events/{eventId}/start", null);
        startResp.IsSuccessStatusCode.Should().BeTrue();

        var body = await startResp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("status").GetString().Should().Be("Live");
    }

    [Fact]
    public async Task Open_Unknown_Event_Returns_NotFound()
    {
        var resp = await _http.PostAsync($"/admin/game-events/{Guid.NewGuid()}/open", null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task Start_Unknown_Event_Returns_NotFound()
    {
        var resp = await _http.PostAsync($"/admin/game-events/{Guid.NewGuid()}/start", null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static CreateGameEventRequest BuildCreateRequest() => new(
        Kind: "battle-royale",
        TierId: 1,
        ScheduledAtUtc: DateTimeOffset.UtcNow.AddHours(1),
        OpenAtUtc: null,
        EntryFeeCoins: 100,
        ReviveCostGems: 10,
        MaxParticipants: 50
    );

    private async Task<Guid> CreateEventAsync()
    {
        var resp = await _http.PostAsJsonAsync("/admin/game-events/", BuildCreateRequest());
        resp.EnsureSuccessStatusCode();
        var summary = await resp.Content.ReadFromJsonAsync<GameEventSummaryDto>();
        return summary!.Id;
    }
}
