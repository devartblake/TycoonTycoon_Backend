using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Synaptix.Backend.Api.Tests.TestHost;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Backend.Infrastructure.Persistence;
using Synaptix.Shared.Contracts.Dtos;
using Xunit;

namespace Synaptix.Backend.Api.Tests.AdminBatch;

// #413: bulk admin operations — per-player partial-failure semantics, idempotent retries.
public sealed class AdminBatchEndpointsTests : IClassFixture<SynaptixApiFactory>
{
    private readonly SynaptixApiFactory _factory;
    private readonly HttpClient _admin;

    public AdminBatchEndpointsTests(SynaptixApiFactory factory)
    {
        _factory = factory;
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    // ── Security contracts ────────────────────────────────────────────────

    [Theory]
    [InlineData("/admin/batch/ban")]
    [InlineData("/admin/batch/reward")]
    [InlineData("/admin/batch/reset-progress")]
    public async Task BatchRoutes_Require_OpsKey(string path)
    {
        using var noKey = _factory.CreateClient();

        var resp = await noKey.PostAsJsonAsync(path, new { });

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Theory]
    [InlineData("/admin/batch/ban")]
    [InlineData("/admin/batch/reward")]
    [InlineData("/admin/batch/reset-progress")]
    public async Task BatchRoutes_Reject_Wrong_OpsKey(string path)
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("bad-key");

        var resp = await wrongKey.PostAsJsonAsync(path, new { });

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    // ── Bulk ban ──────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkBan_Bans_All_Players()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var until = DateTimeOffset.UtcNow.AddDays(30);

        var resp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(ids, "bulk cheating wave", until));
        resp.IsSuccessStatusCode.Should().BeTrue();

        var result = await resp.Content.ReadFromJsonAsync<BatchOperationResultDto>();
        result!.Requested.Should().Be(3);
        result.Succeeded.Should().Be(3);
        result.Failed.Should().Be(0);

        foreach (var id in ids)
        {
            var profileResp = await _admin.GetAsync($"/admin/moderation/profile/{id}");
            var profile = await profileResp.Content.ReadFromJsonAsync<ModerationProfileDto>();
            profile!.Status.Should().Be(4); // Banned
            profile.Reason.Should().Be("bulk cheating wave");
            profile.SetByAdmin.Should().Be("test-admin");
            profile.ExpiresAtUtc!.Value.Should().BeCloseTo(until, TimeSpan.FromSeconds(5));
        }
    }

    [Fact]
    public async Task BulkBan_Partial_Failure_Reports_Per_Id()
    {
        var good = Guid.NewGuid();

        var resp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(new[] { good, Guid.Empty }, "partial batch", null));
        resp.IsSuccessStatusCode.Should().BeTrue();

        var result = await resp.Content.ReadFromJsonAsync<BatchOperationResultDto>();
        result!.Requested.Should().Be(2);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(1);
        result.Items.Should().ContainSingle(i => i.PlayerId == good && i.Success);
        result.Items.Should().ContainSingle(i => i.PlayerId == Guid.Empty && !i.Success && i.Error != null);
    }

    [Fact]
    public async Task BulkBan_Empty_Ids_Returns_BadRequest()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(Array.Empty<Guid>(), "no ids", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task BulkBan_Over_Cap_Returns_BadRequest()
    {
        var tooMany = Enumerable.Range(0, 501).Select(_ => Guid.NewGuid()).ToArray();

        var resp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(tooMany, "too many", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task BulkBan_Blank_Reason_Returns_BadRequest()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/batch/ban",
            new AdminBulkBanRequest(new[] { Guid.NewGuid() }, "  ", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    // ── Bulk reward ───────────────────────────────────────────────────────

    [Fact]
    public async Task BulkReward_Credits_Wallets_And_Is_Idempotent_Per_BatchId()
    {
        var ids = new[] { Guid.NewGuid(), Guid.NewGuid() };
        var batchId = Guid.NewGuid();
        var request = new AdminBulkRewardRequest(
            batchId, ids, new[] { new EconomyLineDto(CurrencyType.Coins, 250) }, "season compensation");

        var resp = await _admin.PostAsJsonAsync("/admin/batch/reward", request);
        resp.IsSuccessStatusCode.Should().BeTrue();
        var result = await resp.Content.ReadFromJsonAsync<BatchOperationResultDto>();
        result!.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);

        int BalanceOf(Guid playerId)
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            return db.PlayerWallets.AsNoTracking().Single(w => w.PlayerId == playerId).Coins;
        }

        foreach (var id in ids)
            BalanceOf(id).Should().Be(250);

        // Same BatchId retried => economy dedupe treats items as Duplicate (success), no double credit.
        var retryResp = await _admin.PostAsJsonAsync("/admin/batch/reward", request);
        retryResp.IsSuccessStatusCode.Should().BeTrue();
        var retry = await retryResp.Content.ReadFromJsonAsync<BatchOperationResultDto>();
        retry!.Succeeded.Should().Be(2, "retrying the same batch id must be idempotent");

        foreach (var id in ids)
            BalanceOf(id).Should().Be(250, "no double crediting on retry");
    }

    [Fact]
    public async Task BulkReward_Missing_BatchId_Returns_BadRequest()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/batch/reward",
            new AdminBulkRewardRequest(Guid.Empty, new[] { Guid.NewGuid() },
                new[] { new EconomyLineDto(CurrencyType.Coins, 10) }, null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    [Fact]
    public async Task BulkReward_Empty_Lines_Returns_BadRequest()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/batch/reward",
            new AdminBulkRewardRequest(Guid.NewGuid(), new[] { Guid.NewGuid() },
                Array.Empty<EconomyLineDto>(), null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }

    // ── Bulk reset progress (skills scope) ────────────────────────────────

    [Fact]
    public async Task BulkResetProgress_Skills_Removes_Unlocks()
    {
        var playerId = Guid.NewGuid();

        // Seed an unlocked skill directly (SkillNodes are seeded by TestBaselineDataSeeder).
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            db.PlayerSkillUnlocks.Add(new PlayerSkillUnlock(playerId, "str.steady_timer"));
            await db.SaveChangesAsync();
        }

        var resp = await _admin.PostAsJsonAsync("/admin/batch/reset-progress",
            new AdminBulkResetProgressRequest(Guid.NewGuid(), new[] { playerId }, "skills", 100));
        resp.IsSuccessStatusCode.Should().BeTrue();

        var result = await resp.Content.ReadFromJsonAsync<BatchOperationResultDto>();
        result!.Succeeded.Should().Be(1);
        result.Failed.Should().Be(0);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            (await db.PlayerSkillUnlocks.AsNoTracking().AnyAsync(x => x.PlayerId == playerId))
                .Should().BeFalse("the skills reset removes all unlocks");
        }
    }

    [Fact]
    public async Task BulkResetProgress_Unknown_Scope_Returns_BadRequest()
    {
        var resp = await _admin.PostAsJsonAsync("/admin/batch/reset-progress",
            new AdminBulkResetProgressRequest(Guid.NewGuid(), new[] { Guid.NewGuid() }, "economy", null));

        resp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await resp.HasErrorCodeAsync("VALIDATION_ERROR");
    }
}
