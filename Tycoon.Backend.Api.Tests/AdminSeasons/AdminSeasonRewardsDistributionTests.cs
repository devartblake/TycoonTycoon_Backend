using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminSeasons;

public sealed class AdminSeasonRewardsDistributionTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminSeasonRewardsDistributionTests(TycoonApiFactory factory) => _factory = factory;

    [Fact]
    public async Task CloseSeason_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");
        var resp = await wrongKey.PostAsync($"/admin/seasons/{Guid.NewGuid()}/close", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task CloseSeason_Requires_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();
        var resp = await noKey.PostAsync($"/admin/seasons/{Guid.NewGuid()}/close", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task CloseSeason_DistributesRewards_FromSnapshot()
    {
        // Arrange: active season + profiles with tierRank eligible for reward
        var seasonId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        Guid playerEligible;
        Guid playerNotEligible;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var season = new Season(
                seasonNumber: 100,
                name: "Reward Season",
                startsAtUtc: now.AddDays(-1),
                endsAtUtc: now.AddDays(7));

            typeof(Season).GetProperty("Id")!.SetValue(season, seasonId);
            season.Activate();
            db.Seasons.Add(season);

            playerEligible = Guid.NewGuid();
            playerNotEligible = Guid.NewGuid();

            var eligible = new PlayerSeasonProfile(seasonId, playerEligible, initialPoints: 999);
            eligible.SetRanks(tier: 1, tierRank: 5, seasonRank: 5);

            var notEligible = new PlayerSeasonProfile(seasonId, playerNotEligible, initialPoints: 10);
            notEligible.SetRanks(tier: 1, tierRank: 999, seasonRank: 999);

            db.PlayerSeasonProfiles.AddRange(eligible, notEligible);
            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key");

        // Act: close season (should trigger rewards)
        var resp = await admin.PostAsync($"/admin/seasons/{seasonId}/close", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: economy txns minted for eligible (depends on your Economy schema)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var eligibleTxns = await db.EconomyTransactions.AsNoTracking()
                .Where(x => x.PlayerId == playerEligible && x.Kind == "season-reward")
                .ToListAsync();

            eligibleTxns.Count.Should().Be(1, "reward should be idempotent per season/player");

            var notEligibleTxns = await db.EconomyTransactions.AsNoTracking()
                .Where(x => x.PlayerId == playerNotEligible && x.Kind == "season-reward")
                .ToListAsync();

            notEligibleTxns.Count.Should().Be(0);
        }
    }
}
