using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Powerups;

public sealed class PowerupsFlowTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public PowerupsFlowTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task Grant_Then_Use_Powerup_Works_And_IsIdempotent()
    {
        var playerId = Guid.NewGuid();

        // Grant 2
        var grant = new GrantPowerupRequest(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            Type: PowerupType.Skip,
            Quantity: 2,
            Reason: "test-grant"
        );

        var g = await _admin.PostAsJsonAsync("/admin/powerups/grant", grant);
        g.EnsureSuccessStatusCode();

        // Check state
        var s1 = await _admin.GetAsync($"/admin/powerups/state/{playerId}");
        s1.EnsureSuccessStatusCode();
        var state1 = await s1.Content.ReadFromJsonAsync<PowerupStateDto>();

        state1!.Powerups.Should().Contain(p => p.Type == PowerupType.Skip && p.Quantity >= 2);

        // Use once
        var useEventId = Guid.NewGuid();
        var useReq = new UsePowerupRequest(useEventId, playerId, PowerupType.Skip);

        var u1 = await _public.PostAsJsonAsync("/powerups/use", useReq);
        u1.EnsureSuccessStatusCode();
        var used1 = await u1.Content.ReadFromJsonAsync<UsePowerupResultDto>();

        used1!.Status.Should().Be("Used");
        used1.Remaining.Should().BeGreaterThanOrEqualTo(1);

        // Same use request again => Duplicate (idempotent)
        var u2 = await _public.PostAsJsonAsync("/powerups/use", useReq);
        u2.EnsureSuccessStatusCode();
        var used2 = await u2.Content.ReadFromJsonAsync<UsePowerupResultDto>();

        used2!.Status.Should().Be("Duplicate");
    }

    [Fact]
    public async Task Cooldown_PreventsImmediateReuse()
    {
        var playerId = Guid.NewGuid();

        // Grant 1
        var grant = new GrantPowerupRequest(Guid.NewGuid(), playerId, PowerupType.FiftyFifty, 1, "cooldown");
        var g = await _admin.PostAsJsonAsync("/admin/powerups/grant", grant);
        g.EnsureSuccessStatusCode();

        // Use => Used
        var use1 = await _public.PostAsJsonAsync("/powerups/use",
            new UsePowerupRequest(Guid.NewGuid(), playerId, PowerupType.FiftyFifty));
        use1.EnsureSuccessStatusCode();
        var r1 = await use1.Content.ReadFromJsonAsync<UsePowerupResultDto>();
        r1!.Status.Should().Be("Used");

        // Grant another so quantity >0, but cooldown still active
        var grant2 = new GrantPowerupRequest(Guid.NewGuid(), playerId, PowerupType.FiftyFifty, 1, "cooldown-2");
        var g2 = await _admin.PostAsJsonAsync("/admin/powerups/grant", grant2);
        g2.EnsureSuccessStatusCode();

        // Use immediately => Cooldown
        var use2 = await _public.PostAsJsonAsync("/powerups/use",
            new UsePowerupRequest(Guid.NewGuid(), playerId, PowerupType.FiftyFifty));
        use2.EnsureSuccessStatusCode();
        var r2 = await use2.Content.ReadFromJsonAsync<UsePowerupResultDto>();
        r2!.Status.Should().Be("Cooldown");
        r2.CooldownUntilUtc.Should().NotBeNull();
    }
}
