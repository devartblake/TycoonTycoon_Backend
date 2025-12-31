using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Skills;

public sealed class SkillTreeFlowTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;
    private readonly HttpClient _public;

    public SkillTreeFlowTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
        _public = factory.CreateClient();
    }

    [Fact]
    public async Task Unlock_Fails_When_Prereq_Missing()
    {
        var playerId = Guid.NewGuid();

        var unlock = new UnlockSkillRequest(
            EventId: Guid.NewGuid(),
            PlayerId: playerId,
            NodeKey: "str.combo_master" // requires str.steady_timer
        );

        var r = await _public.PostAsJsonAsync("/skills/unlock", unlock);
        r.EnsureSuccessStatusCode();

        var res = await r.Content.ReadFromJsonAsync<UnlockSkillResultDto>();
        res!.Status.Should().Be("MissingPrereq");
    }

    [Fact]
    public async Task Unlock_Succeeds_After_Prereq_And_Currency_Award()
    {
        var playerId = Guid.NewGuid();

        // Award coins
        await _admin.PostAsJsonAsync("/admin/economy/transactions",
            new CreateEconomyTxnRequest(
                Guid.NewGuid(),
                playerId,
                "award",
                new[] { new EconomyLineDto(CurrencyType.Coins, 500) }
            ));

        // Unlock prereq
        var prereq = new UnlockSkillRequest(Guid.NewGuid(), playerId, "str.steady_timer");
        var p = await _public.PostAsJsonAsync("/skills/unlock", prereq);
        (await p.Content.ReadFromJsonAsync<UnlockSkillResultDto>())!
            .Status.Should().Be("Unlocked");

        // Unlock dependent
        var unlock = new UnlockSkillRequest(Guid.NewGuid(), playerId, "str.combo_master");
        var r = await _public.PostAsJsonAsync("/skills/unlock", unlock);
        var res = await r.Content.ReadFromJsonAsync<UnlockSkillResultDto>();

        res!.Status.Should().Be("Unlocked");
        res.UnlockedKeys.Should().Contain("str.combo_master");
    }

    [Fact]
    public async Task Unlock_Is_Idempotent_By_EventId()
    {
        var playerId = Guid.NewGuid();

        await _admin.PostAsJsonAsync("/admin/economy/transactions",
            new CreateEconomyTxnRequest(
                Guid.NewGuid(),
                playerId,
                "award",
                new[] { new EconomyLineDto(CurrencyType.Coins, 200) }
            ));

        var eventId = Guid.NewGuid();
        var unlock = new UnlockSkillRequest(eventId, playerId, "know.quick_learner");

        var r1 = await _public.PostAsJsonAsync("/skills/unlock", unlock);
        var r2 = await _public.PostAsJsonAsync("/skills/unlock", unlock);

        (await r1.Content.ReadFromJsonAsync<UnlockSkillResultDto>())!
            .Status.Should().Be("Unlocked");

        (await r2.Content.ReadFromJsonAsync<UnlockSkillResultDto>())!
            .Status.Should().Be("Duplicate");
    }
}
