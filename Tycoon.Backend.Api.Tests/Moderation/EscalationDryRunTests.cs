using System.Net;
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
    public async Task Escalation_Run_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/moderation/escalation/run",
            new RunEscalationRequest(WindowHours: 24, MaxPlayers: 500, DryRun: true));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Escalation_Run_Requires_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();

        var resp = await noKey.PostAsJsonAsync("/admin/moderation/escalation/run",
            new RunEscalationRequest(WindowHours: 24, MaxPlayers: 500, DryRun: true));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task Escalation_Run_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/moderation/escalation/run",
            new RunEscalationRequest(WindowHours: 24, MaxPlayers: 500, DryRun: true));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task Escalation_Run_Requires_OpsKey()
    {
        using var noKey = new TycoonApiFactory().CreateClient();

        var resp = await noKey.PostAsJsonAsync("/admin/moderation/escalation/run",
            new RunEscalationRequest(WindowHours: 24, MaxPlayers: 500, DryRun: true));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
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
