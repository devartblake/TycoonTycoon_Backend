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

    private sealed record HealthMetricDto(DateTimeOffset Timestamp, double Value);
    private sealed record ServiceHistoryDto(string ServiceId, IReadOnlyList<HealthMetricDto> Metrics);

    [Fact]
    public async Task ServicesHistory_ReturnsArrayOfSeries()
    {
        // The sampler runs in the background; history may legitimately be empty
        // right after startup — assert the contract shape, not sample counts.
        var resp = await _admin.GetAsync("/admin/dashboard/services/history?hours=24");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var histories = await resp.Content.ReadFromJsonAsync<List<ServiceHistoryDto>>();
        histories.Should().NotBeNull();
        histories!.Should().OnlyContain(h => !string.IsNullOrWhiteSpace(h.ServiceId) && h.Metrics != null);
    }

    [Fact]
    public async Task SingleServiceHistory_UnknownService_ReturnsEmptySeries()
    {
        var resp = await _admin.GetAsync("/admin/dashboard/services/no-such-check/history");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var history = await resp.Content.ReadFromJsonAsync<ServiceHistoryDto>();
        history!.ServiceId.Should().Be("no-such-check");
        history.Metrics.Should().BeEmpty();
    }

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
