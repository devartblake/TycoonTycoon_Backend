using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Skills;

public sealed class SkillRespecTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public SkillRespecTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task Respec_Refunds_And_Clears_Skills()
    {
        var playerId = Guid.NewGuid();

        // Award coins
        await _admin.PostAsJsonAsync("/admin/economy/transactions",
            new CreateEconomyTxnRequest(
                Guid.NewGuid(),
                playerId,
                "award",
                new[] { new EconomyLineDto(CurrencyType.Coins, 300) }
            ));

        // Unlock skill
        await _public.PostAsJsonAsync("/skills/unlock",
            new UnlockSkillRequest(Guid.NewGuid(), playerId, "know.quick_learner"));

        // Respec
        var respec = new RespecSkillsRequest(Guid.NewGuid(), playerId, 80);
        var r = await _public.PostAsJsonAsync("/skills/respec", respec);
        r.EnsureSuccessStatusCode();

        var res = await r.Content.ReadFromJsonAsync<RespecSkillsResultDto>();
        res!.Status.Should().Be("Respecced");
        res.RefundedCoins.Should().BeGreaterThan(0);
        res.UnlockedKeys.Should().BeEmpty();
    }
}
