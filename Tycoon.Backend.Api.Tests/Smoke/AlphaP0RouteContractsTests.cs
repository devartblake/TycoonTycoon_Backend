using System.Net;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Smoke;

public sealed class AlphaP0RouteContractsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AlphaP0RouteContractsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Theory]
    [InlineData("/store/catalog")]
    [InlineData("/questions/set?count=3")]
    [InlineData("/leaderboards/tiers/1?page=1&pageSize=5")]
    [InlineData("/crypto/history/00000000-0000-0000-0000-000000000001?page=1&pageSize=1")]
    public async Task GetRoutes_Should_Be_Mapped(string route)
    {
        var resp = await _http.GetAsync(route);
        resp.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }

    [Theory]
    [InlineData("/store/iap/validate", "{\"playerId\":\"00000000-0000-0000-0000-000000000001\",\"platform\":\"apple\",\"receipt\":\"test\"}")]
    [InlineData("/crypto/link-wallet", "{\"playerId\":\"00000000-0000-0000-0000-000000000001\",\"walletAddress\":\"0xabc\"}")]
    [InlineData("/crypto/withdraw", "{\"playerId\":\"00000000-0000-0000-0000-000000000001\",\"amount\":1,\"currency\":\"USDT\"}")]
    public async Task PostRoutes_Should_Be_Mapped(string route, string json)
    {
        using var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync(route, content);
        resp.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}
