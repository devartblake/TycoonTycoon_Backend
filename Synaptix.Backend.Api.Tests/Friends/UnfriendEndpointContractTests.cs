using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;

namespace Synaptix.Backend.Api.Tests.Friends;

public sealed class UnfriendEndpointContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _http;

    public UnfriendEndpointContractTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient();
    }

    [Fact]
    public async Task Unfriend_WithEmptyIds_ReturnsValidationEnvelope()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/friends")
        {
            Content = JsonContent.Create(new
            {
                PlayerId = Guid.Empty,
                FriendPlayerId = Guid.Empty
            })
        };
        var resp = await _http.SendAsync(request);

        resp.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task Unfriend_WithExistingFriendship_RemovesBothEdges()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        await CreateFriendshipAsync(from, to);

        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/friends")
        {
            Content = JsonContent.Create(new { PlayerId = from, FriendPlayerId = to })
        };
        var remove = await _http.SendAsync(removeRequest);
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var remaining = await db.FriendEdges
            .Where(x => (x.PlayerId == from && x.FriendPlayerId == to) || (x.PlayerId == to && x.FriendPlayerId == from))
            .CountAsync();

        remaining.Should().Be(0);
    }

    [Fact]
    public async Task Unfriend_WithFriendIdAlias_RemovesBothEdges()
    {
        var from = Guid.NewGuid();
        var to = Guid.NewGuid();

        await CreateFriendshipAsync(from, to);

        var removeRequest = new HttpRequestMessage(HttpMethod.Delete, "/api/v1/friends")
        {
            Content = JsonContent.Create(new { PlayerId = from, FriendId = to })
        };
        var remove = await _http.SendAsync(removeRequest);
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        var remaining = await db.FriendEdges
            .Where(x => (x.PlayerId == from && x.FriendPlayerId == to) || (x.PlayerId == to && x.FriendPlayerId == from))
            .CountAsync();

        remaining.Should().Be(0);
    }

    private async Task CreateFriendshipAsync(Guid from, Guid to)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDb>();

        db.FriendEdges.AddRange(
            new FriendEdge(from, to),
            new FriendEdge(to, from));

        await db.SaveChangesAsync();
    }
}
