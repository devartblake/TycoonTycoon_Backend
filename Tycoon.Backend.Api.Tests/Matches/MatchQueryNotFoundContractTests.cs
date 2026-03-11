using System.Net;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Matches;

public sealed class MatchQueryNotFoundContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MatchQueryNotFoundContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatch_UnknownMatchId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.GetAsync($"/matches/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
