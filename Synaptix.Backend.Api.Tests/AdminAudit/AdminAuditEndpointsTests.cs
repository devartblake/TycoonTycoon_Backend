using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminAudit;

public sealed class AdminAuditEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminAuditEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task SecurityAudit_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync("/admin/audit/security?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task SecurityAudit_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.GetAsync("/admin/audit/security?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SecurityAudit_Returns_Paged_Response()
    {
        var resp = await _http.GetAsync("/admin/audit/security?page=1&pageSize=25");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(25);
        body.TotalItems.Should().BeGreaterThanOrEqualTo(0);
        body.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task SecurityAudit_Accepts_DateRange_Filter()
    {
        var from = DateTimeOffset.UtcNow.AddDays(-7).ToString("O");
        var to   = DateTimeOffset.UtcNow.ToString("O");

        var resp = await _http.GetAsync($"/admin/audit/security?page=1&pageSize=10&from={Uri.EscapeDataString(from)}&to={Uri.EscapeDataString(to)}");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        body.Should().NotBeNull();
        body!.TotalItems.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task SecurityAudit_Accepts_Status_Filter()
    {
        var resp = await _http.GetAsync("/admin/audit/security?page=1&pageSize=10&status=sent");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        body.Should().NotBeNull();
    }

    [Fact]
    public async Task SecurityAudit_Detail_Returns_Single_Event()
    {
        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("nouser@example.com", "wrong"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var listResp = await _http.GetAsync("/admin/audit/security?page=1&pageSize=25");
        listResp.IsSuccessStatusCode.Should().BeTrue();
        var list = await listResp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        var item = list!.Items.Should().ContainSingle(x =>
            x.ChannelKey == "admin_security" &&
            x.Title == "admin_auth_login" &&
            x.Status == "unauthorized").Subject;

        var detailResp = await _http.GetAsync($"/admin/audit/security/{item.Id}");

        detailResp.IsSuccessStatusCode.Should().BeTrue();
        var detail = await detailResp.Content.ReadFromJsonAsync<AdminNotificationHistoryItemDto>();
        detail.Should().NotBeNull();
        detail!.Id.Should().Be(item.Id);
        detail.ChannelKey.Should().Be("admin_security");
        detail.Title.Should().Be(item.Title);
    }

    [Fact]
    public async Task SecurityAudit_Detail_Returns_NotFound_For_Unknown_Id()
    {
        var resp = await _http.GetAsync($"/admin/audit/security/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
