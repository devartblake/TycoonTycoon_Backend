using System.Net;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Users;

public sealed class UserSearchEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public UserSearchEndpointTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Search_WithoutAuth_Returns401()
    {
        var response = await _http.GetAsync("/users/search?handle=ab");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
