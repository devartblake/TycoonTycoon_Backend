using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminAudit;

// #413: rich admin action audit — before/after snapshots written by AdminAuditLogger and
// read back via /admin/audit/logs.
public sealed class AdminAuditLogEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _admin;

    public AdminAuditLogEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task GetLogs_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync("/admin/audit/logs?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task GetLogs_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.GetAsync("/admin/audit/logs?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SetModerationStatus_Writes_AuditLog_With_BeforeAfter()
    {
        var playerId = Guid.NewGuid();

        var setResp = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "audit trail test", null, null, null));
        setResp.IsSuccessStatusCode.Should().BeTrue();

        var logsResp = await _admin.GetAsync("/admin/audit/logs?page=1&pageSize=100&action=moderation.set_status");
        logsResp.IsSuccessStatusCode.Should().BeTrue();
        var logs = await logsResp.Content.ReadFromJsonAsync<AdminAuditLogListResponseDto>();

        var entry = logs!.Items.Should()
            .ContainSingle(x => x.ResourceId == playerId.ToString()).Subject;
        entry.Actor.Should().Be("test-admin");
        entry.ResourceType.Should().Be("player");
        entry.ChangesAfter.Should().NotBeNull();
        entry.ChangesAfter!.Should().ContainKey("status");
        entry.IpAddress.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task BatchBan_Writes_AuditLog_With_Outcome()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };

        var banResp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(ids, "audit batch", null));
        banResp.IsSuccessStatusCode.Should().BeTrue();

        var logsResp = await _admin.GetAsync("/admin/audit/logs?page=1&pageSize=100&action=batch.ban");
        var logs = await logsResp.Content.ReadFromJsonAsync<AdminAuditLogListResponseDto>();

        logs!.Items.Should().NotBeEmpty();
        var entry = logs.Items[0];
        entry.ResourceType.Should().Be("batch");
        entry.ChangesBefore.Should().NotBeNull();
        entry.ChangesAfter.Should().NotBeNull();
        entry.ChangesAfter!.Should().ContainKey("Succeeded");
    }

    [Fact]
    public async Task GetLogs_Filters_By_Action_And_Actor()
    {
        var playerId = Guid.NewGuid();
        (await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 3, "filter test", null, null, null)))
            .IsSuccessStatusCode.Should().BeTrue();

        var filtered = await _admin.GetAsync(
            "/admin/audit/logs?page=1&pageSize=100&action=moderation.set_status&actor=test-admin");
        var logs = await filtered.Content.ReadFromJsonAsync<AdminAuditLogListResponseDto>();

        logs!.Items.Should().NotBeEmpty();
        logs.Items.Should().OnlyContain(x => x.Action == "moderation.set_status" && x.Actor == "test-admin");

        // A non-matching actor filter returns nothing.
        var none = await _admin.GetAsync("/admin/audit/logs?page=1&pageSize=10&actor=nobody-here");
        var noneLogs = await none.Content.ReadFromJsonAsync<AdminAuditLogListResponseDto>();
        noneLogs!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLog_ById_Returns_Detail_And_404_For_Unknown()
    {
        var playerId = Guid.NewGuid();
        (await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 2, "detail test", null, null, null)))
            .IsSuccessStatusCode.Should().BeTrue();

        var logsResp = await _admin.GetAsync("/admin/audit/logs?page=1&pageSize=100&action=moderation.set_status");
        var logs = await logsResp.Content.ReadFromJsonAsync<AdminAuditLogListResponseDto>();
        var id = logs!.Items.First(x => x.ResourceId == playerId.ToString()).Id;

        var detailResp = await _admin.GetAsync($"/admin/audit/logs/{id}");
        detailResp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await detailResp.Content.ReadFromJsonAsync<AdminAuditLogItemDto>();
        detail!.Id.Should().Be(id);

        var missing = await _admin.GetAsync($"/admin/audit/logs/{Guid.NewGuid()}");
        missing.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Existing_Security_Audit_Channel_Unaffected()
    {
        // Regression guard: the pre-existing /admin/audit/security surface still works.
        var resp = await _admin.GetAsync("/admin/audit/security?page=1&pageSize=10");

        resp.IsSuccessStatusCode.Should().BeTrue();
    }
}
