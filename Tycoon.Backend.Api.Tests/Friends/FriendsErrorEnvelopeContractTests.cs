using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;

namespace Tycoon.Backend.Api.Tests.Friends;

public sealed class FriendsErrorEnvelopeContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _http;

    public FriendsErrorEnvelopeContractTests(TycoonApiFactory factory)
    {
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task SendRequest_WithSameFromAndTo_ReturnsConflictEnvelope()
    {
        var playerId = Guid.NewGuid();

        var resp = await _http.PostAsJsonAsync("/friends/request", new { FromPlayerId = playerId, ToPlayerId = playerId });

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await resp.HasErrorCodeAsync("CONFLICT");
    }

    [Fact]
    public async Task ListFriends_WithEmptyPlayerId_ReturnsValidationEnvelope()
    {
        var resp = await _http.GetAsync("/friends?playerId=00000000-0000-0000-0000-000000000000&page=1&pageSize=25");

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task AcceptRequest_WithUnknownRequestId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.PostAsJsonAsync($"/friends/request/{Guid.NewGuid()}/accept", new { PlayerId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task DeclineRequest_WithUnknownRequestId_ReturnsNotFoundEnvelope()
    {
        var resp = await _http.PostAsJsonAsync($"/friends/request/{Guid.NewGuid()}/decline", new { PlayerId = Guid.NewGuid() });

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }
}
