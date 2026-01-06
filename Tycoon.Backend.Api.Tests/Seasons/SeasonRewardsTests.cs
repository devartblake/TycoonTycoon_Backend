using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Seasons;

public sealed class SeasonRewardsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public SeasonRewardsTests(TycoonApiFactory factory) => _factory = factory;
    private static void SetPrivate<T>(object obj, string propName, T value)
    {
        var p = obj.GetType().GetProperty(propName);
        if (p is null)
            throw new InvalidOperationException($"Property '{propName}' not found on {obj.GetType().Name}.");

        p.SetValue(obj, value);
    }

    [Fact]
    public async Task ClaimReward_IsIdempotent_ByEventId()
    {
        var playerId = Guid.NewGuid();
        var seasonId = Guid.NewGuid();

        // Seed minimal season + profile in eligible state
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            // You likely have a Season constructor; adjust as needed
            var season = new Season(
                seasonNumber: 1,
                name: "Test Season",
                startsAtUtc: DateTimeOffset.UtcNow.AddDays(-1),
                endsAtUtc: DateTimeOffset.UtcNow.AddDays(7)
            );

            // force deterministic Id for the test
            typeof(Season).GetProperty("Id")!.SetValue(season, seasonId);

            // make it Active (your SeasonService uses Status==Active for GetActiveAsync) :contentReference[oaicite:1]{index=1}
            season.Activate();
            db.Seasons.Add(season);

            // Ensure placement done and in top 20
            var profile = new PlayerSeasonProfile(seasonId, playerId, initialPoints: 0);

            // set rank fields the rewards service checks:
            // - RankPoints
            // - Tier, TierRank
            // - PlacementMatchesCompleted
            // - optionally Wins/Losses/Draws and SeasonRank if your projections depend on them

            SetPrivate(profile, "RankPoints", 500);
            SetPrivate(profile, "Tier", 1);
            SetPrivate(profile, "TierRank", 10);
            SetPrivate(profile, "SeasonRank", 10);
            SetPrivate(profile, "PlacementMatchesCompleted", 10);
            SetPrivate(profile, "Wins", 5);
            SetPrivate(profile, "Losses", 2);
            SetPrivate(profile, "Draws", 1);
            SetPrivate(profile, "UpdatedAtUtc", DateTimeOffset.UtcNow);

            db.PlayerSeasonProfiles.Add(profile);


            await db.SaveChangesAsync();
        }

        var client = _factory.CreateClient();
        var eventId = Guid.NewGuid();

        var r1 = await client.PostAsJsonAsync(
            $"/seasons/rewards/claim/{playerId}",
            new ClaimSeasonRewardRequestDto(eventId, seasonId));

        r1.StatusCode.Should().Be(HttpStatusCode.OK);
        var dto1 = await r1.Content.ReadFromJsonAsync<ClaimSeasonRewardResponseDto>();
        dto1!.Status.Should().BeOneOf("Applied", "Duplicate");

        var r2 = await client.PostAsJsonAsync(
            $"/seasons/rewards/claim/{playerId}",
            new ClaimSeasonRewardRequestDto(eventId, seasonId));

        var dto2 = await r2.Content.ReadFromJsonAsync<ClaimSeasonRewardResponseDto>();
        dto2!.Status.Should().Be("Duplicate");
        dto2.AwardedCoins.Should().Be(dto1.AwardedCoins);
        dto2.AwardedXp.Should().Be(dto1.AwardedXp);

        // Verify claim row exists
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var claim = await db.SeasonRewardClaims.AsNoTracking()
                .FirstOrDefaultAsync(x => x.EventId == eventId);

            claim.Should().NotBeNull();
        }
    }
}
