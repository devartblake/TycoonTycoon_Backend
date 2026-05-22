using System.Net;
using System.Text.Json.Nodes;
using System.Linq;
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

    [Fact]
    public async Task SessionStart_FirstThreeSessions_Apply_Discount_In_Adjusted_Costs()
    {
        var playerId = Guid.NewGuid();

        var first = await _http.PostAsync($"/mobile/economy/session/start?playerId={playerId}", null);
        var second = await _http.PostAsync($"/mobile/economy/session/start?playerId={playerId}", null);
        var third = await _http.PostAsync($"/mobile/economy/session/start?playerId={playerId}", null);
        var fourth = await _http.PostAsync($"/mobile/economy/session/start?playerId={playerId}", null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        third.StatusCode.Should().Be(HttpStatusCode.OK);
        fourth.StatusCode.Should().Be(HttpStatusCode.OK);

        var firstJson = JsonNode.Parse(await first.Content.ReadAsStringAsync())!;
        var fourthJson = JsonNode.Parse(await fourth.Content.ReadAsStringAsync())!;

        firstJson["earlySessionDiscountApplied"]!.GetValue<bool>().Should().BeTrue();
        firstJson["energyDiscount"]!.GetValue<int>().Should().Be(1);

        // Casual base cost defaults to 3, discounted first sessions should be 2.
        var firstCasualAdjusted = firstJson["adjustedCosts"]!
            .AsArray()
            .First(x => x?["mode"]?.GetValue<string>() == "casual")?["adjustedCost"]?
            .GetValue<int>();
        firstCasualAdjusted.Should().Be(2);

        fourthJson["earlySessionDiscountApplied"]!.GetValue<bool>().Should().BeFalse();
        fourthJson["energyDiscount"]!.GetValue<int>().Should().Be(0);
        var fourthCasualAdjusted = fourthJson["adjustedCosts"]!
            .AsArray()
            .First(x => x?["mode"]?.GetValue<string>() == "casual")?["adjustedCost"]?
            .GetValue<int>();
        fourthCasualAdjusted.Should().Be(3);
    }

    [Fact]
    public async Task ReviveQuote_AlmostWin_Applies_Configured_Discount()
    {
        var playerId = Guid.NewGuid();

        var normal = await _http.PostAsync($"/mobile/economy/revive/quote?playerId={playerId}&almostWin=false", null);
        var almostWin = await _http.PostAsync($"/mobile/economy/revive/quote?playerId={playerId}&almostWin=true", null);

        normal.StatusCode.Should().Be(HttpStatusCode.OK);
        almostWin.StatusCode.Should().Be(HttpStatusCode.OK);

        var normalJson = JsonNode.Parse(await normal.Content.ReadAsStringAsync())!;
        var almostWinJson = JsonNode.Parse(await almostWin.Content.ReadAsStringAsync())!;

        normalJson["baseGemCost"]!.GetValue<int>().Should().Be(5);
        normalJson["discountPercent"]!.GetValue<int>().Should().Be(0);
        normalJson["finalGemCost"]!.GetValue<int>().Should().Be(5);

        almostWinJson["baseGemCost"]!.GetValue<int>().Should().Be(5);
        almostWinJson["discountPercent"]!.GetValue<int>().Should().Be(20);
        almostWinJson["finalGemCost"]!.GetValue<int>().Should().Be(4);
    }

    [Fact]
    public async Task PityLoss_Activates_At_Threshold_And_ReportWin_Resets_Streak()
    {
        var playerId = Guid.NewGuid();

        var loss1 = await _http.PostAsync($"/mobile/economy/pity/report-loss?playerId={playerId}", null);
        var loss2 = await _http.PostAsync($"/mobile/economy/pity/report-loss?playerId={playerId}", null);
        var loss3 = await _http.PostAsync($"/mobile/economy/pity/report-loss?playerId={playerId}", null);

        loss1.StatusCode.Should().Be(HttpStatusCode.OK);
        loss2.StatusCode.Should().Be(HttpStatusCode.OK);
        loss3.StatusCode.Should().Be(HttpStatusCode.OK);

        var loss1Json = JsonNode.Parse(await loss1.Content.ReadAsStringAsync())!;
        var loss3Json = JsonNode.Parse(await loss3.Content.ReadAsStringAsync())!;

        loss1Json["lossStreak"]!.GetValue<int>().Should().Be(1);
        loss1Json["pityActive"]!.GetValue<bool>().Should().BeFalse();

        loss3Json["lossStreak"]!.GetValue<int>().Should().Be(3);
        loss3Json["pityActive"]!.GetValue<bool>().Should().BeTrue();
        loss3Json["difficultyReductionPercent"]!.GetValue<decimal>().Should().Be(0.10m);

        var win = await _http.PostAsync($"/mobile/economy/pity/report-win?playerId={playerId}", null);
        win.StatusCode.Should().Be(HttpStatusCode.OK);

        var winJson = JsonNode.Parse(await win.Content.ReadAsStringAsync())!;
        winJson["lossStreak"]!.GetValue<int>().Should().Be(0);
        winJson["pityActive"]!.GetValue<bool>().Should().BeFalse();

        var lossAfterReset = await _http.PostAsync($"/mobile/economy/pity/report-loss?playerId={playerId}", null);
        var lossAfterResetJson = JsonNode.Parse(await lossAfterReset.Content.ReadAsStringAsync())!;
        lossAfterResetJson["lossStreak"]!.GetValue<int>().Should().Be(1);
        lossAfterResetJson["pityActive"]!.GetValue<bool>().Should().BeFalse();
    }
}
