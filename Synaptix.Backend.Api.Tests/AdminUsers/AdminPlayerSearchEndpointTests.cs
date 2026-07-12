using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminUsers;

// Covers GET /admin/player-lookup/search — the multi-result search added for the
// operator dashboard (the /resolve route only returns a single exact match).
public sealed class AdminPlayerSearchEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public AdminPlayerSearchEndpointTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    private async Task<string> CreateUserAsync(string emailPrefix)
    {
        var email = $"{emailPrefix}-{Guid.NewGuid():N}@example.com";
        var resp = await _http.PostAsJsonAsync("/admin/users", new AdminCreateUserRequest(
            Username: $"srch_{Guid.NewGuid():N}"[..18],
            Email: email,
            Role: "user",
            AgeGroup: "adult",
            IsVerified: true,
            TemporaryPassword: "TempPass123!"));
        resp.StatusCode.Should().Be(HttpStatusCode.Created);
        return email;
    }

    [Fact]
    public async Task Search_ByEmailFragment_ReturnsMultipleMatches()
    {
        var prefix = $"multi{Guid.NewGuid():N}"[..16];
        await CreateUserAsync(prefix);
        await CreateUserAsync(prefix);

        var resp = await _http.GetAsync($"/admin/player-lookup/search?query={prefix}&limit=10");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<AdminPlayerSearchResponse>();
        result.Should().NotBeNull();
        result!.Items.Should().HaveCount(2);
        result.Items.Should().OnlyContain(i => i.Email!.Contains(prefix));
        result.Items.Should().OnlyContain(i => i.PlayerId != Guid.Empty);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmptyList()
    {
        var resp = await _http.GetAsync($"/admin/player-lookup/search?query=no-such-player-{Guid.NewGuid():N}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<AdminPlayerSearchResponse>();
        result!.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }

    [Fact]
    public async Task Search_EmptyQuery_ReturnsEmptyList()
    {
        var resp = await _http.GetAsync("/admin/player-lookup/search?query=");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await resp.Content.ReadFromJsonAsync<AdminPlayerSearchResponse>();
        result!.Items.Should().BeEmpty();
    }
}
