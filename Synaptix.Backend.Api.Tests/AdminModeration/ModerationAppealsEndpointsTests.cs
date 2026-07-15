using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminModeration;

// #413: moderation appeals — player submit/track (v1) + admin review (/admin/moderation/appeals).
public sealed class ModerationAppealsEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _admin;

    public ModerationAppealsEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    private HttpClient PlayerClient(Guid playerId) =>
        _factory.CreateClient().AuthenticateAsPlayer(_factory, playerId);

    private async Task<ModerationAppealDto> SubmitAppealAsync(Guid playerId, string reason)
    {
        using var player = PlayerClient(playerId);
        var resp = await player.PostAsJsonAsync("/api/v1/moderation/appeals", new SubmitAppealRequest(reason));
        resp.IsSuccessStatusCode.Should().BeTrue();
        return (await resp.Content.ReadFromJsonAsync<ModerationAppealDto>())!;
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAppeal_Requires_PlayerAuth()
    {
        using var anon = _factory.CreateClient();

        var resp = await anon.PostAsJsonAsync("/api/v1/moderation/appeals", new SubmitAppealRequest("please"));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task AdminListAppeals_Requires_OpsKey()
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.GetAsync("/admin/moderation/appeals?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task AdminListAppeals_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.GetAsync("/admin/moderation/appeals?page=1&pageSize=10");

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    // ── Happy path ────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitAppeal_Creates_Pending_Appeal_Visible_To_Admin()
    {
        var playerId = Guid.NewGuid();

        var appeal = await SubmitAppealAsync(playerId, "I did not cheat");

        appeal.PlayerId.Should().Be(playerId);
        appeal.Status.Should().Be(1); // Pending
        appeal.Reason.Should().Be("I did not cheat");

        var listResp = await _admin.GetAsync($"/admin/moderation/appeals?page=1&pageSize=50&playerId={playerId}&status=1");
        listResp.IsSuccessStatusCode.Should().BeTrue();
        var list = await listResp.Content.ReadFromJsonAsync<ModerationAppealListResponseDto>();
        list!.Items.Should().ContainSingle(a => a.Id == appeal.Id);
    }

    [Fact]
    public async Task SubmitAppeal_Duplicate_Pending_Returns_Conflict()
    {
        var playerId = Guid.NewGuid();
        await SubmitAppealAsync(playerId, "first appeal");

        using var player = PlayerClient(playerId);
        var resp = await player.PostAsJsonAsync("/api/v1/moderation/appeals", new SubmitAppealRequest("second appeal"));

        resp.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await resp.HasErrorCodeAsync("APPEAL_PENDING");
    }

    [Fact]
    public async Task SubmitAppeal_Empty_Reason_Returns_BadRequest()
    {
        using var player = PlayerClient(Guid.NewGuid());

        var resp = await player.PostAsJsonAsync("/api/v1/moderation/appeals", new SubmitAppealRequest("  "));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task ReviewAppeal_Approve_Restores_Moderation_And_Logs()
    {
        var playerId = Guid.NewGuid();

        // Ban the player through the normal admin pipeline (Banned = 4).
        var banResp = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "cheating", null, null, null));
        banResp.IsSuccessStatusCode.Should().BeTrue();

        var appeal = await SubmitAppealAsync(playerId, "false positive");

        var reviewResp = await _admin.PostAsJsonAsync(
            $"/admin/moderation/appeals/{appeal.Id}/review",
            new ReviewAppealRequest("approve", "verified innocence"));
        reviewResp.IsSuccessStatusCode.Should().BeTrue();

        var reviewed = await reviewResp.Content.ReadFromJsonAsync<ModerationAppealDto>();
        reviewed!.Status.Should().Be(2); // Approved
        reviewed.ReviewerNotes.Should().Be("verified innocence");
        reviewed.ReviewedBy.Should().Be("test-admin");
        reviewed.ReviewedAtUtc.Should().NotBeNull();

        // Approval lifts the sanction via the moderation pipeline (Normal = 1)...
        var profileResp = await _admin.GetAsync($"/admin/moderation/profile/{playerId}");
        var profile = await profileResp.Content.ReadFromJsonAsync<ModerationProfileDto>();
        profile!.Status.Should().Be(1);
        profile.Reason.Should().Be("appeal approved");

        // ...and writes a ModerationActionLog entry.
        var logsResp = await _admin.GetAsync($"/admin/moderation/logs?page=1&pageSize=50&playerId={playerId}");
        var logs = await logsResp.Content.ReadFromJsonAsync<ModerationLogListResponseDto>();
        logs!.Items.Should().Contain(l => l.Reason == "appeal approved" && l.NewStatus == 1);
    }

    [Fact]
    public async Task ReviewAppeal_Reject_Keeps_Moderation_Status()
    {
        var playerId = Guid.NewGuid();

        var banResp = await _admin.PostAsJsonAsync("/admin/moderation/set-status",
            new SetModerationStatusRequest(playerId, 4, "cheating", null, null, null));
        banResp.IsSuccessStatusCode.Should().BeTrue();

        var appeal = await SubmitAppealAsync(playerId, "let me back in");

        var reviewResp = await _admin.PostAsJsonAsync(
            $"/admin/moderation/appeals/{appeal.Id}/review",
            new ReviewAppealRequest("reject", "evidence stands"));
        reviewResp.IsSuccessStatusCode.Should().BeTrue();

        var reviewed = await reviewResp.Content.ReadFromJsonAsync<ModerationAppealDto>();
        reviewed!.Status.Should().Be(3); // Rejected

        // The ban must remain in place.
        var profileResp = await _admin.GetAsync($"/admin/moderation/profile/{playerId}");
        var profile = await profileResp.Content.ReadFromJsonAsync<ModerationProfileDto>();
        profile!.Status.Should().Be(4);
    }

    [Fact]
    public async Task ReviewAppeal_Twice_Returns_Conflict()
    {
        var appeal = await SubmitAppealAsync(Guid.NewGuid(), "double review");

        (await _admin.PostAsJsonAsync(
            $"/admin/moderation/appeals/{appeal.Id}/review",
            new ReviewAppealRequest("reject", null))).IsSuccessStatusCode.Should().BeTrue();

        var second = await _admin.PostAsJsonAsync(
            $"/admin/moderation/appeals/{appeal.Id}/review",
            new ReviewAppealRequest("approve", null));

        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await second.HasErrorCodeAsync("ALREADY_REVIEWED");
    }

    [Fact]
    public async Task ReviewAppeal_Invalid_Verdict_Returns_BadRequest()
    {
        var appeal = await SubmitAppealAsync(Guid.NewGuid(), "bad verdict");

        var resp = await _admin.PostAsJsonAsync(
            $"/admin/moderation/appeals/{appeal.Id}/review",
            new ReviewAppealRequest("maybe", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GetAppeal_Unknown_Returns_NotFound()
    {
        var resp = await _admin.GetAsync($"/admin/moderation/appeals/{Guid.NewGuid()}");

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task ListAppeals_Invalid_Status_Returns_BadRequest()
    {
        var resp = await _admin.GetAsync("/admin/moderation/appeals?page=1&pageSize=10&status=nonsense");

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task GetMine_Returns_Only_Callers_Appeals()
    {
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();
        var appealA = await SubmitAppealAsync(playerA, "appeal A");
        await SubmitAppealAsync(playerB, "appeal B");

        using var player = PlayerClient(playerA);
        var resp = await player.GetAsync("/api/v1/moderation/appeals/mine");

        resp.IsSuccessStatusCode.Should().BeTrue();
        var mine = await resp.Content.ReadFromJsonAsync<List<ModerationAppealDto>>();
        mine!.Should().ContainSingle(a => a.Id == appealA.Id);
        mine.Should().OnlyContain(a => a.PlayerId == playerA);
    }
}
