using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminAuth;

public sealed class AdminSecurityAuditEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminSecurityAuditEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task SecurityAudit_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.GetAsync("/admin/audit/security?page=1&pageSize=50");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SecurityAudit_Requires_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();

        var resp = await noKey.GetAsync("/admin/audit/security?page=1&pageSize=50");
        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task SecurityAudit_StatusFilter_And_PagingFallbacks_Work()
    {
        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("nouser@example.com", "wrong"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await loginResp.HasErrorCodeAsync("UNAUTHORIZED");

        var auditResp = await _http.GetAsync("/admin/audit/security?status=unauthorized&page=0&pageSize=0");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await auditResp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        data.Should().NotBeNull();
        data!.Page.Should().Be(1);
        data.PageSize.Should().Be(25);
        data.Items.Should().OnlyContain(x => x.Status == "unauthorized");
    }

    [Fact]
    public async Task SecurityAudit_PageSize_IsClamped_To200()
    {
        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("nouser@example.com", "wrong"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await loginResp.HasErrorCodeAsync("UNAUTHORIZED");

        var auditResp = await _http.GetAsync("/admin/audit/security?page=1&pageSize=999");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await auditResp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        data.Should().NotBeNull();
        data!.PageSize.Should().Be(200);
    }

    [Fact]
    public async Task SecurityAudit_FutureWindow_ReturnsEmpty()
    {
        var from = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(1).ToString("O"));
        var to = Uri.EscapeDataString(DateTimeOffset.UtcNow.AddHours(2).ToString("O"));

        var auditResp = await _http.GetAsync($"/admin/audit/security?from={from}&to={to}&page=1&pageSize=25");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await auditResp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        data.Should().NotBeNull();
        data!.Items.Should().BeEmpty();
        data.TotalItems.Should().Be(0);
    }

    [Fact]
    public async Task SecurityAudit_Contains_AdminAuthLoginUnauthorized_Event()
    {
        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("nouser@example.com", "wrong"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await loginResp.HasErrorCodeAsync("UNAUTHORIZED");

        var auditResp = await _http.GetAsync("/admin/audit/security?page=1&pageSize=50");
        auditResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var data = await auditResp.Content.ReadFromJsonAsync<AdminNotificationHistoryResponse>();
        data.Should().NotBeNull();
        data!.Items.Should().Contain(x =>
            x.ChannelKey == "admin_security" &&
            x.Title == "admin_auth_login" &&
            x.Status == "unauthorized");
    }
}
