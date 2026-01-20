using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Seasons;

public sealed class SeasonRewardsPreviewTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public SeasonRewardsPreviewTests(TycoonApiFactory factory) => _factory = factory;

    private sealed record RewardPreviewDto(
        Guid SeasonId,
        Guid PlayerId,
        bool Eligible,
        int Tier,
        int TierRank,
        int RewardXp,
        int RewardCoins
    );

    [Fact]
    public async Task Preview_ReturnsEligibility_AndRewardValues()
    {
        // Arrange
        var seasonId = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var season = new Season(101, "Preview Season", now.AddDays(-1), now.AddDays(7));
            typeof(Season).GetProperty("Id")!.SetValue(season, seasonId);
            season.Activate();
            db.Seasons.Add(season);

            var profile = new PlayerSeasonProfile(seasonId, playerId, initialPoints: 100);
            profile.SetRanks(tier: 1, tierRank: 10, seasonRank: 10);
            db.PlayerSeasonProfiles.Add(profile);

            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();

        // Act
        var res = await client.GetAsync($"/seasons/rewards/preview/{playerId}?seasonId={seasonId}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await res.Content.ReadFromJsonAsync<RewardPreviewDto>();
        dto.Should().NotBeNull();
        dto!.PlayerId.Should().Be(playerId);
        dto.SeasonId.Should().Be(seasonId);

        // With your SeasonRewards config: ensure you return positive reward when eligible.
        dto.Eligible.Should().BeTrue();
        dto.RewardXp.Should().BeGreaterThan(0);
        dto.RewardCoins.Should().BeGreaterThanOrEqualTo(0);
    }
}
