using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Users;

public sealed class PlayerLoadoutEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public PlayerLoadoutEndpointTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Loadout_GetWithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/users/me/preferences/loadout");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Loadout_PutAndGet_WithOwnedItems_RoundTrips()
    {
        var client = _factory.CreateClient();
        var email = $"loadout-{Guid.NewGuid():N}@example.com";

        var signupResp = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"loadout_user_{Guid.NewGuid():N}"));
        signupResp.EnsureSuccessStatusCode();

        var signup = await signupResp.Content.ReadFromJsonAsync<SignupResponse>();
        signup.Should().NotBeNull();
        var playerId = Guid.Parse(signup!.UserId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", signup.AccessToken);

        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var tx = new PlayerTransaction(Guid.NewGuid(), "store-purchase");
            tx.AddActor(playerId, PlayerTransactionActorRole.Buyer);
            tx.AddItemChange("avatar:default", 1, ItemOperation.Grant);
            tx.AddItemChange("cosmetic:neon-border", 1, ItemOperation.Grant);
            tx.AddItemChange("cosmetic:glow-trail", 1, ItemOperation.Grant);
            tx.MarkApplied();
            db.PlayerTransactions.Add(tx);
            await db.SaveChangesAsync();
        }

        var put = await client.PutAsJsonAsync("/users/me/preferences/loadout", new UpdatePlayerLoadoutRequest(
            AvatarItemType: "avatar:default",
            EquippedCosmeticItemTypes: new[] { "cosmetic:neon-border", "cosmetic:glow-trail" }));
        put.StatusCode.Should().Be(HttpStatusCode.OK);

        var loadout = await client.GetFromJsonAsync<PlayerLoadoutDto>("/users/me/preferences/loadout");
        loadout.Should().NotBeNull();
        loadout!.AvatarItemType.Should().Be("avatar:default");
        loadout.EquippedCosmeticItemTypes.Should().Contain("cosmetic:neon-border");
        loadout.EquippedCosmeticItemTypes.Should().Contain("cosmetic:glow-trail");
    }
}
