using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Synaptix.Backend.Api.Features.AdminDashboard;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminDashboard;

// Unit-tests the Prometheus instant-query parsing/fallback in isolation
// (no WebApplicationFactory) — the client must never throw, only return 0.
public sealed class PrometheusQueryClientTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _code;
        private readonly string _body;
        public StubHandler(string body, HttpStatusCode code = HttpStatusCode.OK) { _body = body; _code = code; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(_code) { Content = new StringContent(_body, Encoding.UTF8, "application/json") });
    }

    private static PrometheusQueryClient Build(string body, string? url = "http://prometheus:9090", HttpStatusCode code = HttpStatusCode.OK)
    {
        var cfg = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Dashboard:PrometheusUrl"] = url })
            .Build();
        return new PrometheusQueryClient(new HttpClient(new StubHandler(body, code)), cfg, NullLogger<PrometheusQueryClient>.Instance);
    }

    [Fact]
    public async Task QueryScalar_ParsesInstantVectorValue()
    {
        var client = Build("""{"status":"success","data":{"resultType":"vector","result":[{"metric":{},"value":[1700000000,"42.5"]}]}}""");
        (await client.QueryScalarAsync("up", CancellationToken.None)).Should().Be(42.5);
    }

    [Fact]
    public async Task QueryScalar_EmptyResult_ReturnsZero()
    {
        var client = Build("""{"status":"success","data":{"resultType":"vector","result":[]}}""");
        (await client.QueryScalarAsync("up", CancellationToken.None)).Should().Be(0d);
    }

    [Fact]
    public async Task QueryScalar_ErrorStatus_ReturnsZero()
    {
        var client = Build("""{"status":"error","errorType":"bad_data","error":"parse error"}""");
        (await client.QueryScalarAsync("bad(", CancellationToken.None)).Should().Be(0d);
    }

    [Fact]
    public async Task QueryScalar_Non200_ReturnsZero()
    {
        var client = Build("boom", code: HttpStatusCode.InternalServerError);
        (await client.QueryScalarAsync("up", CancellationToken.None)).Should().Be(0d);
    }

    [Fact]
    public async Task NotConfigured_SkipsHttpAndReturnsZero()
    {
        var client = Build("""{"status":"success","data":{"resultType":"vector","result":[{"value":[0,"9"]}]}}""", url: "");
        client.IsConfigured.Should().BeFalse();
        (await client.QueryScalarAsync("up", CancellationToken.None)).Should().Be(0d);
    }
}
