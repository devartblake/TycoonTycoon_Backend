using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.GameEvents;

public sealed class AdminGameEventsEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public AdminGameEventsEndpointsTests(SynaptixApiFactory factory)
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

        var summary = await resp.Content.ReadFromJsonAsync<GameEventSummaryDto>(TestJson.Default);
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

    // ── List endpoint ─────────────────────────────────────────────────────

    [Fact]
    public async Task List_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();
        var resp = await noKey.GetAsync("/admin/game-events");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task List_Returns_Paged_Response()
    {
        var resp = await _http.GetAsync("/admin/game-events?page=1&pageSize=10");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("page").GetInt32().Should().Be(1);
        body.GetProperty("pageSize").GetInt32().Should().Be(10);
        body.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(0);
        body.GetProperty("items").ValueKind.Should().Be(System.Text.Json.JsonValueKind.Array);
    }

    [Fact]
    public async Task List_Created_Event_Appears_In_Results()
    {
        await CreateEventAsync();

        var resp = await _http.GetAsync("/admin/game-events?page=1&pageSize=100");
        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("total").GetInt32().Should().BeGreaterThanOrEqualTo(1);
        body.GetProperty("items").GetArrayLength().Should().BeGreaterThanOrEqualTo(1);
    }

    [Fact]
    public async Task List_Filters_By_Status()
    {
        var eventId = await CreateEventAsync();
        await _http.PostAsync($"/admin/game-events/{eventId}/open", null);

        var resp = await _http.GetAsync("/admin/game-events?page=1&status=Open&pageSize=100");
        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        foreach (var item in body.GetProperty("items").EnumerateArray())
        {
            item.GetProperty("status").GetString().Should().Be("Open");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    // ── Cancel ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Cancel_SetsStatusCancelled()
    {
        var id = await CreateEventAsync();

        var resp = await _http.PostAsync($"/admin/game-events/{id}/cancel", null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await resp.Content.ReadFromJsonAsync<Dictionary<string, string>>(TestJson.Default);
        body!["status"].Should().Be("Cancelled");
    }

    [Fact]
    public async Task Cancel_Twice_ReturnsConflict()
    {
        var id = await CreateEventAsync();
        (await _http.PostAsync($"/admin/game-events/{id}/cancel", null)).EnsureSuccessStatusCode();

        var resp = await _http.PostAsync($"/admin/game-events/{id}/cancel", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await resp.HasErrorCodeAsync("CONFLICT");
    }

    [Fact]
    public async Task Cancel_UnknownEvent_Returns404()
    {
        var resp = await _http.PostAsync($"/admin/game-events/{Guid.NewGuid()}/cancel", null);
        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static CreateGameEventRequest BuildCreateRequest() => new(
        Kind: "millionaire",
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
        var summary = await resp.Content.ReadFromJsonAsync<GameEventSummaryDto>(TestJson.Default);
        return summary!.Id;
    }
}
