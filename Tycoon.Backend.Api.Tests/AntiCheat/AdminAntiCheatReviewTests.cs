using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Backend.Infrastructure.Persistence;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminAntiCheat;

public sealed class AdminAntiCheatReviewTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    
    public AdminAntiCheatReviewTests(TycoonApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PutReview_MarksFlagReviewed_AndAppearsOnList()
    {
        // Arrange
        var flagId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var flag = new AntiCheatFlag(
                matchId: matchId,
                playerId: null,
                ruleKey: "party-member-missing-from-submit",
                severity: AntiCheatSeverity.Warning,
                action: AntiCheatAction.Warn,
                message: "Missing party member(s).",
                evidenceJson: "{\"partyId\":\"" + Guid.NewGuid() + "\"}",
                createdAtUtc: DateTimeOffset.UtcNow
            );

            // Force Id so we can target it deterministically
            typeof(AntiCheatFlag).GetProperty("Id")!.SetValue(flag, flagId);

            db.AntiCheatFlags.Add(flag);
            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key"); // align to your factory config

        // Act: review
        var put = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/flags/{flagId}/review",
            new ReviewAntiCheatFlagRequestDto("devart", "reviewed ok"));

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert DB updated
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var saved = await db.AntiCheatFlags.AsNoTracking().FirstAsync(x => x.Id == flagId);
            saved.ReviewedAtUtc.Should().NotBeNull();
            saved.ReviewedBy.Should().Be("devart");
            saved.ReviewNote.Should().Be("reviewed ok");
        }

        // Assert list projection includes review fields
        var list = await admin.GetFromJsonAsync<AntiCheatFlagListResponseDto>(
            "/admin/anti-cheat/flags?page=1&pageSize=25");

        list.Should().NotBeNull();
        list!.Items.Should().Contain(x => x.Id == flagId);

        var dto = list.Items.First(x => x.Id == flagId);
        dto.ReviewedAtUtc.Should().NotBeNull();
        dto.ReviewedBy.Should().Be("devart");
        dto.ReviewNote.Should().Be("reviewed ok");
    }

    [Fact]
    public async Task PutReview_IsIdempotent_DoesNotOverwriteFirstReview()
    {
        var flagId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var flag = new AntiCheatFlag(
                matchId: matchId,
                playerId: null,
                ruleKey: "test-flag",
                severity: AntiCheatSeverity.Info,
                action: AntiCheatAction.LogOnly,
                message: "test",
                evidenceJson: null,
                createdAtUtc: DateTimeOffset.UtcNow
            );

            typeof(AntiCheatFlag).GetProperty("Id")!.SetValue(flag, flagId);

            db.AntiCheatFlags.Add(flag);
            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key");

        var first = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/flags/{flagId}/review",
            new ReviewAntiCheatFlagRequestDto("first", "first note"));
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/flags/{flagId}/review",
            new ReviewAntiCheatFlagRequestDto("second", "second note"));
        second.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var saved = await db.AntiCheatFlags.AsNoTracking().FirstAsync(x => x.Id == flagId);

            saved.ReviewedBy.Should().Be("first");
            saved.ReviewNote.Should().Be("first note");
        }
    }

    [Fact]
    public async Task PutReview_PartyAliasRoute_WorksSameAsPrimary()
    {
        var flagId = Guid.NewGuid();
        var matchId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();

            var flag = new AntiCheatFlag(
                matchId: matchId,
                playerId: null,
                ruleKey: "party-member-missing-from-submit",
                severity: AntiCheatSeverity.Warning,
                action: AntiCheatAction.Warn,
                message: "Missing party member(s).",
                evidenceJson: "{\"partyId\":\"" + Guid.NewGuid() + "\"}",
                createdAtUtc: DateTimeOffset.UtcNow
            );

            typeof(AntiCheatFlag).GetProperty("Id")!.SetValue(flag, flagId);

            db.AntiCheatFlags.Add(flag);
            await db.SaveChangesAsync();
        }

        var admin = _factory.CreateClient();
        admin.DefaultRequestHeaders.Add("X-Admin-Ops-Key", "test-admin-ops-key");

        var put = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/party/flags/{flagId}/review",
            new ReviewAntiCheatFlagRequestDto("alias", "ok"));

        put.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDb>();
            var saved = await db.AntiCheatFlags.AsNoTracking().FirstAsync(x => x.Id == flagId);

            saved.ReviewedBy.Should().Be("alias");
            saved.ReviewNote.Should().Be("ok");
            saved.ReviewedAtUtc.Should().NotBeNull();
        }
    }

    [Fact]
    public async Task AntiCheat_Flags_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.GetAsync("/admin/anti-cheat/flags?page=1&pageSize=25");
        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task PutReview_Rejects_Wrong_OpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PutAsJsonAsync(
            $"/admin/anti-cheat/flags/{Guid.NewGuid()}/review",
            new ReviewAntiCheatFlagRequestDto("devart", "wrong key"));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task PutReview_PartyAlias_UnknownFlag_ReturnsNotFoundEnvelope()
    {
        var admin = _factory.CreateClient().WithAdminOpsKey();

        var resp = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/party/flags/{Guid.NewGuid()}/review",
            new ReviewAntiCheatFlagRequestDto("devart", "missing flag"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

    [Fact]
    public async Task PutReview_UnknownFlag_ReturnsNotFoundEnvelope()
    {
        var admin = _factory.CreateClient().WithAdminOpsKey();

        var resp = await admin.PutAsJsonAsync(
            $"/admin/anti-cheat/flags/{Guid.NewGuid()}/review",
            new ReviewAntiCheatFlagRequestDto("devart", "missing flag"));

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
        await resp.HasErrorCodeAsync("NOT_FOUND");
    }

}
