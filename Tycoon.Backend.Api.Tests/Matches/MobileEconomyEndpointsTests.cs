using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Matches;

public sealed class MobileEconomyEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public MobileEconomyEndpointsTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task State_Returns_Config_Snapshot()
    {
        var resp = await _http.GetAsync("/mobile/economy/state");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await resp.Content.ReadAsStringAsync();
        json.Should().Contain("energy");
        json.Should().Contain("modes");
    }

    [Fact]
    public async Task SessionStart_InvalidPlayer_Returns_BadRequest()
    {
        var resp = await _http.PostAsync("/mobile/economy/session/start?playerId=00000000-0000-0000-0000-000000000000", null);
        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DailyTicket_Claim_Is_Limited_Per_Day()
    {
        var playerId = Guid.NewGuid();
        var first = await _http.PostAsync($"/mobile/economy/daily-jackpot-ticket/claim?playerId={playerId}", null);
        var second = await _http.PostAsync($"/mobile/economy/daily-jackpot-ticket/claim?playerId={playerId}", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstBody = await first.Content.ReadAsStringAsync();
        var secondBody = await second.Content.ReadAsStringAsync();
        firstBody.Should().Contain("\"granted\":true");
        secondBody.Should().Contain("\"granted\":false");
    }
}
