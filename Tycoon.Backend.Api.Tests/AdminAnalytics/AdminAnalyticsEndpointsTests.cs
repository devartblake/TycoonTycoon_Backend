using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminAnalytics;

/// <summary>
/// Tests for POST /admin/analytics/rebuild-elastic-rollups.
///
/// Because IRollupRebuilder is only registered when Elastic is configured,
/// these tests inject a no-op stub via ConfigureTestServices.
/// </summary>
public sealed class AdminAnalyticsEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminAnalyticsEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    /// <summary>Creates a factory with a no-op IRollupRebuilder stub registered.</summary>
    private TycoonApiFactory FactoryWithStubRebuilder(Action<RebuildSpy>? configure = null)
    {
        var spy = new RebuildSpy();
        configure?.Invoke(spy);

        return (TycoonApiFactory)_factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IRollupRebuilder>();
                services.AddSingleton<IRollupRebuilder>(spy);
            }));
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Fact]
    public async Task Rebuild_Requires_OpsKey()
    {
        using var factory = FactoryWithStubRebuilder();
        using var noKey = factory.CreateClient();

        var resp = await noKey.PostAsync("/admin/analytics/rebuild-elastic-rollups", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Rebuild_Rejects_Wrong_OpsKey()
    {
        using var factory = FactoryWithStubRebuilder();
        using var wrongKey = factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.PostAsync("/admin/analytics/rebuild-elastic-rollups", null);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task Rebuild_Without_DateRange_Returns_Ok_With_NullDates()
    {
        using var factory = FactoryWithStubRebuilder();
        using var http = factory.CreateClient().WithAdminOpsKey();

        var resp = await http.PostAsync("/admin/analytics/rebuild-elastic-rollups", null);

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("message").GetString().Should().Be("Elastic rollups rebuild completed.");
        body.GetProperty("fromUtcDate").ValueKind.Should().Be(JsonValueKind.Null);
        body.GetProperty("toUtcDate").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task Rebuild_With_DateRange_Echoes_Dates_In_Response()
    {
        using var factory = FactoryWithStubRebuilder();
        using var http = factory.CreateClient().WithAdminOpsKey();

        var resp = await http.PostAsync(
            "/admin/analytics/rebuild-elastic-rollups?fromUtcDate=2025-01-01&toUtcDate=2025-01-31",
            null);

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("fromUtcDate").GetString().Should().Be("2025-01-01");
        body.GetProperty("toUtcDate").GetString().Should().Be("2025-01-31");
    }

    [Fact]
    public async Task Rebuild_Invokes_RebuildElasticFromMongo()
    {
        var spy = new RebuildSpy();
        using var factory = (TycoonApiFactory)_factory.WithWebHostBuilder(b =>
            b.ConfigureServices(services =>
            {
                services.RemoveAll<IRollupRebuilder>();
                services.AddSingleton<IRollupRebuilder>(spy);
            }));
        using var http = factory.CreateClient().WithAdminOpsKey();

        await http.PostAsync(
            "/admin/analytics/rebuild-elastic-rollups?fromUtcDate=2025-03-01&toUtcDate=2025-03-31",
            null);

        spy.WasCalled.Should().BeTrue();
        spy.CalledFrom.Should().Be(new DateOnly(2025, 3, 1));
        spy.CalledTo.Should().Be(new DateOnly(2025, 3, 31));
    }

    // ── Spy ───────────────────────────────────────────────────────────────

    private sealed class RebuildSpy : IRollupRebuilder
    {
        public bool WasCalled { get; private set; }
        public DateOnly? CalledFrom { get; private set; }
        public DateOnly? CalledTo { get; private set; }

        public Task RebuildDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct)
            => Task.CompletedTask;

        public Task RebuildPlayerDailyAsync(DateOnly fromUtcDate, DateOnly toUtcDate, CancellationToken ct)
            => Task.CompletedTask;

        public Task RebuildElasticFromMongoAsync(DateOnly? fromUtcDate, DateOnly? toUtcDate, CancellationToken ct)
        {
            WasCalled = true;
            CalledFrom = fromUtcDate;
            CalledTo = toUtcDate;
            return Task.CompletedTask;
        }
    }
}
