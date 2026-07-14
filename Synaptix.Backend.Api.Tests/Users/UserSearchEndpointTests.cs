using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Users;

public sealed class UserSearchEndpointTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public UserSearchEndpointTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Search_WithoutAuth_Returns401()
    {
        using var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/users/search?handle=ab");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Search_WithAuth_ReturnsPagedEnvelope()
    {
        using var client = _factory.CreateClient();

        var signupResp = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
            Email: $"search-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"search_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup!.AccessToken);

        var response = await client.GetFromJsonAsync<UserSearchResponseDto>("/api/v1/users/search?handle=search_user&page=1&pageSize=10");

        response.Should().NotBeNull();
        response!.Page.Should().Be(1);
        response.PageSize.Should().Be(10);
        response.Total.Should().BeGreaterThan(0);
        response.TotalPages.Should().BeGreaterThan(0);
        response.Items.Should().NotBeEmpty();

        var item = response.Items[0];
        item.Id.Should().NotBe(Guid.Empty);
        item.Handle.Should().NotBeNullOrWhiteSpace();
        item.DisplayName.Should().Be(item.Handle);
        item.Username.Should().Be(item.Handle);
    }
}
