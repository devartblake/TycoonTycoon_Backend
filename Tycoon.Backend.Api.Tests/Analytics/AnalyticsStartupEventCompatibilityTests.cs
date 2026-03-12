using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Analytics;

public sealed class AnalyticsStartupEventCompatibilityTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AnalyticsStartupEventCompatibilityTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task StartupEvent_RootAnalyticsRoute_ReturnsAccepted()
    {
        var resp = await _http.PostAsJsonAsync("/analytics/startup_event", new
        {
            eventName = "startup",
            appVersion = "1.0.0",
            platform = "web"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }

    [Fact]
    public async Task StartupEvent_V1AnalyticsRoute_ReturnsAccepted()
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/analytics/startup_event", new
        {
            eventName = "startup",
            appVersion = "1.0.0",
            platform = "web"
        });

        resp.StatusCode.Should().Be(HttpStatusCode.Accepted);
    }
}
