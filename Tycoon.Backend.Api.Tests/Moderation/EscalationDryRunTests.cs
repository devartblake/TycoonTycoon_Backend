using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.Moderation;

public sealed class EscalationDryRunTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;

    public EscalationDryRunTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task Escalation_DryRun_ReturnsDecisions()
    {
        // Assumes your anti-cheat flags can be created by submitting a severe-bad match
        // (already exists in your test suite). Here we just run escalation and expect it to be stable.

        var resp = await _admin.PostAsJsonAsync("/admin/moderation/escalation/run",
            new RunEscalationRequest(WindowHours: 24, MaxPlayers: 500, DryRun: true));

        resp.EnsureSuccessStatusCode();

        var res = await resp.Content.ReadFromJsonAsync<RunEscalationResponse>();
        res.Should().NotBeNull();
        res!.DryRun.Should().BeTrue();
        res.EvaluatedPlayers.Should().BeGreaterThanOrEqualTo(0);
        res.Decisions.Should().NotBeNull();
    }
}
