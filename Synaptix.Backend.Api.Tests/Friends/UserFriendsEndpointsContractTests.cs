using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Friends;

public sealed class UserFriendsEndpointsContractTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;

    public UserFriendsEndpointsContractTests(SynaptixApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ListFriends_ReturnsTotalPagesInEnvelope()
    {
        using var primaryClient = _factory.CreateClient();
        using var friendOneClient = _factory.CreateClient();
        using var friendTwoClient = _factory.CreateClient();

        var primary = await SignupAsync(primaryClient, "friends-primary");
        var friendOne = await SignupAsync(friendOneClient, "friends-one");
        var friendTwo = await SignupAsync(friendTwoClient, "friends-two");

        primaryClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", primary.AccessToken);

        var primaryUserId = Guid.Parse(primary.UserId);
        var friendOneUserId = Guid.Parse(friendOne.UserId);
        var friendTwoUserId = Guid.Parse(friendTwo.UserId);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.FriendEdges.AddRange(
                new FriendEdge(primaryUserId, friendOneUserId),
                new FriendEdge(friendOneUserId, primaryUserId),
                new FriendEdge(primaryUserId, friendTwoUserId),
                new FriendEdge(friendTwoUserId, primaryUserId));
            await db.SaveChangesAsync();
        }

        var response = await primaryClient.GetFromJsonAsync<FriendsListResponseDto>("/api/v1/users/me/friends?page=1&pageSize=1");

        response.Should().NotBeNull();
        response!.Page.Should().Be(1);
        response.PageSize.Should().Be(1);
        response.Total.Should().Be(2);
        response.TotalPages.Should().Be(2);
        response.Items.Should().HaveCount(1);
    }

    private static async Task<SignupResponse> SignupAsync(HttpClient client, string prefix)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
            Email: $"{prefix}-{Guid.NewGuid():N}@example.com",
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"{prefix}_{Guid.NewGuid():N}"));

        response.EnsureSuccessStatusCode();

        var signup = await response.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        return signup!;
    }
}
