using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Entitlements.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Store;

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

        var response = await client.GetAsync($"/api/v1/store/inventory/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Inventory_WithGrantedEntitlements_ReturnsItems()
    {
        var client = _factory.CreateClient();
        var email = $"inventory-{Guid.NewGuid():N}@example.com";

        var signupResp = await client.PostAsJsonAsync("/api/v1/auth/signup", new SignupRequest(
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

            var txId = Guid.NewGuid();
            db.PlayerEntitlements.Add(PlayerEntitlement.Grant(playerId, "cosmetic:neon-border", "cosmetic", 1, txId));
            db.PlayerEntitlements.Add(PlayerEntitlement.Grant(playerId, "powerup:skip", "powerup", 3, Guid.NewGuid()));

            await db.SaveChangesAsync();
        }

        var response = await client.GetFromJsonAsync<PlayerInventoryDto>($"/api/v1/store/inventory/{playerId}");
        response.Should().NotBeNull();
        response!.PlayerId.Should().Be(playerId);
        response.Count.Should().Be(2);
        response.Items.Should().Contain(i => i.ItemType == "cosmetic:neon-border" && i.Quantity == 1);
        response.Items.Should().Contain(i => i.ItemType == "powerup:skip" && i.Quantity == 3);
    }
}
