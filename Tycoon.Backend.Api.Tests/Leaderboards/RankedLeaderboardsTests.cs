using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Leaderboards;

public sealed class RankedLeaderboardsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public RankedLeaderboardsTests(TycoonApiFactory factory) => _factory = factory;

    [Fact]
    public async Task GetRankedLeaderboard_ReturnsGridShape()
    {
        var client = _factory.CreateClient();

        var resp = await client.GetAsync("/leaderboards/ranked?scope=global&page=1&pageSize=10&sort=points");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var dto = await resp.Content.ReadFromJsonAsync<RankedLeaderboardGridResponseDto>();
        dto.Should().NotBeNull();
        dto!.Columns.Should().NotBeEmpty();
        dto.Meta.Should().NotBeNull();
        // Items may be empty depending on seed, but response shape must hold
    }
}
