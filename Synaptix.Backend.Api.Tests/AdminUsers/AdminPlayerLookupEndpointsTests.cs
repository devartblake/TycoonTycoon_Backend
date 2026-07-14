using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminUsers;

public sealed class AdminPlayerLookupEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public AdminPlayerLookupEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Resolve_ByUserEmail_CreatesStableShortCode()
    {
        using var http = _factory.CreateClient().WithAdminOpsKey();
        var email = $"{Guid.NewGuid():N}@example.com";
        var username = $"lookup_{Guid.NewGuid():N}"[..18];

        var createResp = await http.PostAsJsonAsync("/admin/users", new AdminCreateUserRequest(
            Username: username,
            Email: email,
            Role: "user",
            AgeGroup: "adult",
            IsVerified: true,
            TemporaryPassword: "TempPass123!"));
        createResp.StatusCode.Should().Be(HttpStatusCode.Created);

        var firstResp = await http.GetAsync($"/admin/player-lookup/resolve?query={Uri.EscapeDataString(email)}");
        firstResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var first = await firstResp.Content.ReadFromJsonAsync<AdminPlayerLookupResponse>();
        first.Should().NotBeNull();
        first!.ShortCode.Should().MatchRegex("^[A-Z2-9]{6}$");
        first.Created.Should().BeTrue();
        first.Email.Should().Be(email);
        first.Username.Should().Be(username);

        var secondResp = await http.GetAsync($"/admin/player-lookup/resolve?query={first.ShortCode}");
        secondResp.StatusCode.Should().Be(HttpStatusCode.OK);
        var second = await secondResp.Content.ReadFromJsonAsync<AdminPlayerLookupResponse>();
        second.Should().NotBeNull();
        second!.ShortCode.Should().Be(first.ShortCode);
        second.PlayerId.Should().Be(first.PlayerId);
        second.Created.Should().BeFalse();
    }

    [Fact]
    public async Task Resolve_UnknownQuery_ReturnsNotFoundEnvelope()
    {
        using var http = _factory.CreateClient().WithAdminOpsKey();

        var resp = await http.GetAsync($"/admin/player-lookup/resolve?query={Guid.NewGuid():N}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
