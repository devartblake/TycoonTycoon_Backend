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
    public async Task SecurityAudit_Contains_AdminAuthLoginUnauthorized_Event()
    {
        var loginResp = await _http.PostAsJsonAsync("/admin/auth/login", new AdminLoginRequest("nouser@example.com", "wrong"));
        loginResp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

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
