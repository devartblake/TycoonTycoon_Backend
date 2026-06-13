using System.Net;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;

namespace Synaptix.Backend.Api.Tests.Missions;

public sealed class MissionClaimErrorEnvelopeContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MissionClaimErrorEnvelopeContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task ClaimMission_UnknownMission_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.PostAsync($"/api/v1/missions/{Guid.NewGuid()}/claim?playerId={Guid.NewGuid()}&type=daily", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
