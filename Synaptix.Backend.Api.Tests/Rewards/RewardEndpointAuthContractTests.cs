using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Xunit;

namespace Synaptix.Backend.Api.Tests.Rewards;

public sealed class RewardEndpointAuthContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public RewardEndpointAuthContractTests(SynaptixApiFactory factory) => _factory = factory;

    [Theory]
    [InlineData("/api/v1/rewards/daily-config")]
    [InlineData("/api/v1/rewards/weekly-schedule")]
    [InlineData("/api/v1/rewards/spin-reward-steps")]
    public async Task Public_Reward_Config_Endpoints_Allow_Anonymous(string path)
    {
        using var http = _factory.CreateClient();

        var response = await http.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Account_Rewards_Status_Requires_Bearer()
    {
        using var http = _factory.CreateClient();

        var response = await http.GetAsync("/api/v1/account/rewards/status");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Account_Rewards_Status_Allows_Device_Guest_Bearer()
    {
        using var http = _factory.CreateClient();
        var bootstrap = await http.PostAsJsonAsync(
            "/api/v1/auth/device/bootstrap",
            new
            {
                deviceId = $"test-device-{Guid.NewGuid():N}",
                deviceType = "android",
            });
        bootstrap.EnsureSuccessStatusCode();
        var session = await bootstrap.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = session.GetProperty("accessToken").GetString();
        accessToken.Should().NotBeNullOrWhiteSpace();
        http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await http.GetAsync("/api/v1/account/rewards/status");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.TryGetProperty("canClaimDaily", out _).Should().BeTrue();
        body.TryGetProperty("dailyCoins", out _).Should().BeTrue();
        body.TryGetProperty("weeklySchedule", out _).Should().BeTrue();
    }
}
