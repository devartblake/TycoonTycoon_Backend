using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.AdminSeasons;

public sealed class AdminSeasonsNotFoundContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public AdminSeasonsNotFoundContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ActivateSeason_UnknownSeason_ReturnsNotFoundEnvelope()
    {
        var admin = _factory.CreateClient();
        admin.WithAdminOpsKey();

        var resp = await admin.PostAsJsonAsync("/admin/seasons/activate", new ActivateSeasonRequest(Guid.NewGuid()));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task CloseSeason_UnknownSeason_ReturnsNotFoundEnvelope()
    {
        var admin = _factory.CreateClient();
        admin.WithAdminOpsKey();

        var resp = await admin.PostAsJsonAsync("/admin/seasons/close", new CloseSeasonRequest(
            SeasonId: Guid.NewGuid(),
            CarryoverPercent: 30,
            CreateNextSeason: false,
            NextSeasonName: null));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
