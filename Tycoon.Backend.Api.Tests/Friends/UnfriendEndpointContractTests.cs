using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Friends;

public sealed class UnfriendEndpointContractTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public UnfriendEndpointContractTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Unfriend_WithEmptyIds_ReturnsValidationEnvelope()
    {
        var resp = await _http.DeleteAsJsonAsync("/friends", new
        {
            PlayerId = Guid.Empty,
            FriendPlayerId = Guid.Empty
        });

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Unfriend_WithExistingFriendship_RemovesBothEdges()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        var send = await _http.PostAsJsonAsync("/friends/request", new { FromPlayerId = from, ToPlayerId = to });
        send.EnsureSuccessStatusCode();
        var req = await send.Content.ReadFromJsonAsync<FriendRequestDto>();
        req.Should().NotBeNull();

        var accept = await _http.PostAsJsonAsync($"/friends/request/{req!.RequestId}/accept", new { PlayerId = to });
        accept.EnsureSuccessStatusCode();

        var remove = await _http.DeleteAsJsonAsync("/friends", new { PlayerId = from, FriendPlayerId = to });
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var remaining = await db.FriendEdges
            .Where(x => (x.PlayerId == from && x.FriendPlayerId == to) || (x.PlayerId == to && x.FriendPlayerId == from))
            .CountAsync();

        remaining.Should().Be(0);
    }
}
