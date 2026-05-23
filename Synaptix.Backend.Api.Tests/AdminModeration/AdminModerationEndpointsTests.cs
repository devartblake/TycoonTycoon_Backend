using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminModeration;

public sealed class AdminModerationEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminModerationEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync($"/admin/moderation/profile/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task GetProfile_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.GetAsync($"/admin/moderation/profile/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SetStatus_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var req = new SetModerationStatusRequest(Guid.NewGuid(), 0, null, null, null, null);
        var resp = await noKey.PostAsJsonAsync("/admin/moderation/set-status", req);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task GetLogs_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync("/admin/moderation/logs?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task GetProfile_Returns_DefaultProfile_For_Unknown_Player()
    {
        var playerId = Guid.NewGuid();

        var resp = await _http.GetAsync($"/admin/moderation/profile/{playerId}");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var profile = await resp.Content.ReadFromJsonAsync<ModerationProfileDto>();
        profile.Should().NotBeNull();
        profile!.PlayerId.Should().Be(playerId);
        // Default status is Normal (ModerationStatus.Normal = 1)
        profile.Status.Should().Be(1);
        profile.Reason.Should().BeNull();
    }

    [Fact]
    public async Task SetStatus_Ban_And_GetProfile_Reflects_Ban()
    {
        var playerId = Guid.NewGuid();

        // Ban the player (status=2 = Banned)
        var banReq = new SetModerationStatusRequest(
            PlayerId: playerId,
            Status: 2,
            Reason: "cheating",
            Notes: "caught speed-hacking",
            ExpiresAtUtc: null,
            RelatedFlagId: null
        );

        var banResp = await _http.PostAsJsonAsync("/admin/moderation/set-status", banReq);
        banResp.IsSuccessStatusCode.Should().BeTrue();

        var banProfile = await banResp.Content.ReadFromJsonAsync<ModerationProfileDto>();
        banProfile.Should().NotBeNull();
        banProfile!.PlayerId.Should().Be(playerId);
        banProfile.Status.Should().Be(2);
        banProfile.Reason.Should().Be("cheating");
        banProfile.SetByAdmin.Should().Be("test-admin");

        // Get profile confirms persistence
        var getResp = await _http.GetAsync($"/admin/moderation/profile/{playerId}");
        getResp.IsSuccessStatusCode.Should().BeTrue();

        var fetchedProfile = await getResp.Content.ReadFromJsonAsync<ModerationProfileDto>();
        fetchedProfile!.Status.Should().Be(2);
        fetchedProfile.Reason.Should().Be("cheating");
    }

    [Fact]
    public async Task SetStatus_Creates_Log_Entry()
    {
        var playerId = Guid.NewGuid();

        var req = new SetModerationStatusRequest(
            PlayerId: playerId,
            Status: 1,   // Restricted
            Reason: "suspicious activity",
            Notes: null,
            ExpiresAtUtc: DateTimeOffset.UtcNow.AddDays(7),
            RelatedFlagId: null
        );

        var setResp = await _http.PostAsJsonAsync("/admin/moderation/set-status", req);
        setResp.IsSuccessStatusCode.Should().BeTrue();

        // Check logs
        var logsResp = await _http.GetAsync($"/admin/moderation/logs?page=1&pageSize=50&playerId={playerId}");
        logsResp.IsSuccessStatusCode.Should().BeTrue();

        var logs = await logsResp.Content.ReadFromJsonAsync<ModerationLogListResponseDto>();
        logs.Should().NotBeNull();
        logs!.Items.Should().Contain(l => l.PlayerId == playerId && l.NewStatus == 1 && l.Reason == "suspicious activity");
    }

    [Fact]
    public async Task GetLogById_Returns_Single_Log()
    {
        var playerId = Guid.NewGuid();

        var req = new SetModerationStatusRequest(
            PlayerId: playerId,
            Status: 1,
            Reason: "detail lookup",
            Notes: "detail notes",
            ExpiresAtUtc: null,
            RelatedFlagId: null
        );

        var setResp = await _http.PostAsJsonAsync("/admin/moderation/set-status", req);
        setResp.IsSuccessStatusCode.Should().BeTrue();

        var logsResp = await _http.GetAsync($"/admin/moderation/logs?page=1&pageSize=50&playerId={playerId}");
        var logs = await logsResp.Content.ReadFromJsonAsync<ModerationLogListResponseDto>();
        var id = logs!.Items.Should().ContainSingle(x => x.Reason == "detail lookup").Subject.Id;

        var detailResp = await _http.GetAsync($"/admin/moderation/logs/{id}");

        detailResp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await detailResp.Content.ReadFromJsonAsync<ModerationLogItemDto>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(id);
        detail.PlayerId.Should().Be(playerId);
        detail.Notes.Should().Be("detail notes");
    }

    [Fact]
    public async Task GetLogById_Returns_NotFound_For_Unknown_Id()
    {
        var resp = await _http.GetAsync($"/admin/moderation/logs/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task GetLogs_Returns_Paged_Response()
    {
        var resp = await _http.GetAsync("/admin/moderation/logs?page=1&pageSize=25");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<ModerationLogListResponseDto>();
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(25);
        body.Total.Should().BeGreaterThanOrEqualTo(0);
        body.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SetStatus_WithExpiresAt_Stores_Expiry()
    {
        var playerId = Guid.NewGuid();
        var expiresAt = DateTimeOffset.UtcNow.AddDays(3);

        var req = new SetModerationStatusRequest(
            PlayerId: playerId,
            Status: 1,
            Reason: "temp restrict",
            Notes: null,
            ExpiresAtUtc: expiresAt,
            RelatedFlagId: null
        );

        var setResp = await _http.PostAsJsonAsync("/admin/moderation/set-status", req);
        setResp.IsSuccessStatusCode.Should().BeTrue();

        var getResp = await _http.GetAsync($"/admin/moderation/profile/{playerId}");
        var profile = await getResp.Content.ReadFromJsonAsync<ModerationProfileDto>();

        profile!.ExpiresAtUtc.Should().NotBeNull();
        profile.ExpiresAtUtc!.Value.Should().BeCloseTo(expiresAt, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GetLogs_Can_Filter_By_Status()
    {
        var bannedPlayer = Guid.NewGuid();
        var restrictedPlayer = Guid.NewGuid();

        var banReq = new SetModerationStatusRequest(
            PlayerId: bannedPlayer,
            Status: 2,   // Banned
            Reason: "banned for test",
            Notes: null,
            ExpiresAtUtc: null,
            RelatedFlagId: null
        );
        var restrictedReq = new SetModerationStatusRequest(
            PlayerId: restrictedPlayer,
            Status: 1,   // Restricted
            Reason: "restricted for test",
            Notes: null,
            ExpiresAtUtc: null,
            RelatedFlagId: null
        );

        (await _http.PostAsJsonAsync("/admin/moderation/set-status", banReq)).IsSuccessStatusCode.Should().BeTrue();
        (await _http.PostAsJsonAsync("/admin/moderation/set-status", restrictedReq)).IsSuccessStatusCode.Should().BeTrue();

        var logsResp = await _http.GetAsync("/admin/moderation/logs?page=1&pageSize=100&status=2");
        logsResp.IsSuccessStatusCode.Should().BeTrue();
        var logs = await logsResp.Content.ReadFromJsonAsync<ModerationLogListResponseDto>();
        logs.Should().NotBeNull();
        logs!.Items.Should().OnlyContain(i => i.NewStatus == 2);
    }

    [Fact]
    public async Task GetLogs_Invalid_Status_Filter_Returns_BadRequest()
    {
        var resp = await _http.GetAsync("/admin/moderation/logs?page=1&pageSize=25&status=not-a-valid-status");
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }
}
