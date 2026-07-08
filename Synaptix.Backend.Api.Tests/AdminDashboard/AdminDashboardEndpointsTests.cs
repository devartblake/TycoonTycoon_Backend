using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.AdminDashboard;

// Covers the operator overview route added for #418: GET /admin/dashboard/stats,
// which projects the registered ASP.NET health checks into the dashboard shape.
public sealed class AdminDashboardEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;

    public AdminDashboardEndpointsTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    private sealed record ServiceHealthDto(string Id, string Name, string DisplayName, string Status, double ResponseTime);
    private sealed record DashboardStatsDto(IReadOnlyList<ServiceHealthDto> Services, int ChecksPerformed, int AlertsActive);

    [Fact]
    public async Task Stats_ReturnsHealthReportShapedResponse()
    {
        var resp = await _admin.GetAsync("/admin/dashboard/stats");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await resp.Content.ReadFromJsonAsync<DashboardStatsDto>();
        dto.Should().NotBeNull();

        // The service list mirrors the health report; the summary counters derive from it.
        dto!.ChecksPerformed.Should().Be(dto.Services.Count);
        dto.AlertsActive.Should().Be(dto.Services.Count(s => s.Status != "healthy"));
        dto.Services.Should().OnlyContain(s =>
            s.Status == "healthy" || s.Status == "degraded" || s.Status == "offline");
    }
}
