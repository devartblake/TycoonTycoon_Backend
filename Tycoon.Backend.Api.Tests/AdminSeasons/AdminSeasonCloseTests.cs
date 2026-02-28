using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminSeasons;

public sealed class AdminSeasonCloseTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminSeasonCloseTests(TycoonApiFactory factory) => _factory = factory;

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
    public async Task CloseSeason_CreatesSnapshot_ClosesSeason_IsIdempotent()
    {
        // Arrange: create active season + a few profiles
        var seasonId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var season = new Season(
                seasonNumber: 99,
                name: "Test Season",
                startsAtUtc: now.AddDays(-1),
                endsAtUtc: now.AddDays(7));

            // Force deterministic id
            typeof(Season).GetProperty("Id")!.SetValue(season, seasonId);
            season.Activate();
            db.Seasons.Add(season);

            // Create a few season profiles
            var p1 = new PlayerSeasonProfile(seasonId, Guid.NewGuid(), initialPoints: 120);
            var p2 = new PlayerSeasonProfile(seasonId, Guid.NewGuid(), initialPoints: 80);
            var p3 = new PlayerSeasonProfile(seasonId, Guid.NewGuid(), initialPoints: 200);

            // Ensure ranks exist for snapshot
            p1.SetRanks(tier: 1, tierRank: 2, seasonRank: 2);
            p2.SetRanks(tier: 1, tierRank: 3, seasonRank: 3);
            p3.SetRanks(tier: 1, tierRank: 1, seasonRank: 1);

            db.PlayerSeasonProfiles.AddRange(p1, p2, p3);

            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key");

        // Act: close season (1st call)
        var first = await admin.PostAsync($"/admin/seasons/{seasonId}/close", content: null);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: season closed + snapshots created
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var season = await db.Seasons.AsNoTracking().FirstAsync(x => x.Id == seasonId);
            season.Status.Should().Be(SeasonStatus.Closed);

            var snaps = await db.SeasonRankSnapshots.AsNoTracking()
                .Where(x => x.SeasonId == seasonId)
                .ToListAsync();

            snaps.Count.Should().Be(3);
            snaps.Should().OnlyHaveUniqueItems(x => x.PlayerId);

            // sanity: ordering data present
            snaps.Should().Contain(x => x.SeasonRank == 1);
            snaps.Should().Contain(x => x.SeasonRank == 2);
            snaps.Should().Contain(x => x.SeasonRank == 3);
        }

        // Act: close season (2nd call) -> idempotent
        var second = await admin.PostAsync($"/admin/seasons/{seasonId}/close", content: null);
        second.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: still 3 snapshots (no duplicates)
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var snaps = await db.SeasonRankSnapshots.AsNoTracking()
                .Where(x => x.SeasonId == seasonId)
                .ToListAsync();

            snaps.Count.Should().Be(3);
        }
    }
}
