using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Store;

public sealed class StoreInventoryEndpointTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;

    public StoreInventoryEndpointTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Inventory_WithoutAuth_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync($"/store/inventory/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inventory_WithAppliedTransactions_ReturnsAggregatedItems()
    {
        var client = _factory.CreateClient();
        var email = $"inventory-{Guid.NewGuid():N}@example.com";

        var signupResp = await client.PostAsJsonAsync("/auth/signup", new SignupRequest(
            Email: email,
            Password: "Passw0rd!",
            DeviceId: "ios-sim",
            Username: $"inventory_user_{Guid.NewGuid():N}"));
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
            tx.AddItemChange("cosmetic:neon-border", 2, ItemOperation.Grant);
            tx.AddItemChange("cosmetic:neon-border", 1, ItemOperation.Revoke);
            tx.AddItemChange("powerup:skip", 3, ItemOperation.Grant);
            tx.MarkApplied();
            db.PlayerTransactions.Add(tx);

            var pending = new PlayerTransaction(Guid.NewGuid(), "store-purchase");
            pending.AddActor(playerId, PlayerTransactionActorRole.Buyer);
            pending.AddItemChange("cosmetic:pending-item", 10, ItemOperation.Grant);
            db.PlayerTransactions.Add(pending);

            await db.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<PlayerInventoryDto>($"/store/inventory/{playerId}");
        response.Should().NotBeNull();
        response!.PlayerId.Should().Be(playerId);
        response.Count.Should().Be(2);
        response.Items.Should().Contain(i => i.ItemType == "cosmetic:neon-border" && i.Quantity == 1);
        response.Items.Should().Contain(i => i.ItemType == "powerup:skip" && i.Quantity == 3);
        response.Items.Should().NotContain(i => i.ItemType == "cosmetic:pending-item");
    }
}
