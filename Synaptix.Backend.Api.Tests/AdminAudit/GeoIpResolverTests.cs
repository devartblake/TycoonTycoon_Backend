using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Synaptix.Backend.Api.Features.AdminAnalytics;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminAudit;

// Unit-tests the ip-api geo resolver's IP filtering + batch parsing with a
// stubbed HTTP handler (no network). Behavior under test: private/loopback IPs
// are never sent upstream, and a batch response maps to GeoLocationDto.
public sealed class GeoIpResolverTests
{
    private sealed class CountingHandler : HttpMessageHandler
    {
        private readonly string _body;
        public int Calls { get; private set; }
        public CountingHandler(string body) => _body = body;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Calls++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_body, Encoding.UTF8, "application/json")
            });
        }
    }

    private static IpApiGeoIpResolver Build(CountingHandler handler)
    {
        var cfg = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();
        return new IpApiGeoIpResolver(
            new HttpClient(handler),
            new MemoryCache(new MemoryCacheOptions()),
            cfg,
            NullLogger<IpApiGeoIpResolver>.Instance);
    }

    [Fact]
    public async Task Resolve_FiltersPrivateIps_AndMapsPublicResult()
    {
        var handler = new CountingHandler("""[{"status":"success","country":"United States","countryCode":"US","city":"Ashburn","lat":39.04,"lon":-77.47,"isp":"Google","proxy":false,"query":"8.8.8.8"}]""");
        var resolver = Build(handler);

        var results = await resolver.ResolveAsync(new[] { "8.8.8.8", "10.0.0.5", "127.0.0.1", "192.168.1.9", "unknown" }, CancellationToken.None);

        results.Should().HaveCount(1);
        var g = results[0];
        g.Ip.Should().Be("8.8.8.8");
        g.Country.Should().Be("United States");
        g.Lat.Should().Be(39.04);
        g.Lon.Should().Be(-77.47);
    }

    [Fact]
    public async Task Resolve_OnlyPrivateIps_MakesNoHttpCall()
    {
        var handler = new CountingHandler("[]");
        var resolver = Build(handler);

        var results = await resolver.ResolveAsync(new[] { "10.0.0.1", "127.0.0.1", "192.168.0.2" }, CancellationToken.None);

        results.Should().BeEmpty();
        handler.Calls.Should().Be(0);
    }

    [Fact]
    public async Task Resolve_CachesResults_SecondCallSkipsHttp()
    {
        var handler = new CountingHandler("""[{"status":"success","country":"US","countryCode":"US","city":"X","lat":1.0,"lon":2.0,"isp":"i","proxy":false,"query":"8.8.8.8"}]""");
        var resolver = Build(handler);

        await resolver.ResolveAsync(new[] { "8.8.8.8" }, CancellationToken.None);
        await resolver.ResolveAsync(new[] { "8.8.8.8" }, CancellationToken.None);

        handler.Calls.Should().Be(1);
    }
}
