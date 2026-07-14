using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using System.Text.Json;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminMatches;

public sealed class AdminMatchesEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public AdminMatchesEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task ListMatches_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync("/admin/matches?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task ListMatches_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.GetAsync("/admin/matches?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task ListMatches_Returns_Paged_Response_With_No_Data()
    {
        var resp = await _http.GetAsync("/admin/matches?page=1&pageSize=10");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<MatchListResponseDto>(TestJson.Default);
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
        body.PageSize.Should().Be(10);
        body.Total.Should().BeGreaterThanOrEqualTo(0);
        body.Items.Should().NotBeNull();
    }

    [Fact]
    public async Task ListMatches_Clamps_PageSize_To_Max_100()
    {
        var resp = await _http.GetAsync("/admin/matches?page=1&pageSize=9999");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<MatchListResponseDto>(TestJson.Default);
        body.Should().NotBeNull();
        body!.PageSize.Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task ListMatches_Defaults_Page_To_1_When_Zero()
    {
        var resp = await _http.GetAsync("/admin/matches?page=0&pageSize=5");

        resp.IsSuccessStatusCode.Should().BeTrue();

        var body = await resp.Content.ReadFromJsonAsync<MatchListResponseDto>(TestJson.Default);
        body.Should().NotBeNull();
        body!.Page.Should().Be(1);
    }
}
