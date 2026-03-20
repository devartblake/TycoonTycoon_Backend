using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminAudit;

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
}
