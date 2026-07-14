using System.Net;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.Matches;

public sealed class MatchQueryNotFoundContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly HttpClient _http;

    public MatchQueryNotFoundContractTests(SynaptixApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task GetMatch_UnknownMatchId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.GetAsync($"/api/v1/matches/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
