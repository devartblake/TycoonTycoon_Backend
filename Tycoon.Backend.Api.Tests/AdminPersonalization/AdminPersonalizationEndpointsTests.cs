using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminPersonalization;

public sealed class AdminPersonalizationEndpointsTests : IClassFixture<TycoonApiFactory>
{
    private readonly TycoonApiFactory _factory;
    private readonly HttpClient _http;

    public AdminPersonalizationEndpointsTests(TycoonApiFactory factory)
    {
        _factory = factory;
        _http = factory.CreateClient().WithAdminOpsKey();
    }

    // ── Security: ops key required ────────────────────────────────────────

    [Fact]
    public async Task AdminRoutes_Reject_Wrong_OpsKey()
    {
        using var wrongKey = _factory.CreateClient().WithAdminOpsKey("wrong-key");
        var r = await wrongKey.GetAsync("/admin/personalization/summary");
        r.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await r.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task AdminRoutes_Require_OpsKey()
    {
        using var noKey = _factory.CreateClient();
        var r = await noKey.GetAsync("/admin/personalization/summary");
        r.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await r.HasErrorCodeAsync("UNAUTHORIZED");
    }

    // ── GET /admin/personalization/summary ────────────────────────────────

    [Fact]
    public async Task GetSummary_Returns200_WithArchetypeCountsAndRiskBands()
    {
        var resp = await _http.GetAsync("/admin/personalization/summary");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await resp.Content.ReadFromJsonAsync<PersonalizationSummaryDto>();
        summary.Should().NotBeNull();
        summary!.ArchetypeCounts.Should().NotBeNull();
        summary.TotalProfiles.Should().BeGreaterThanOrEqualTo(0);
        summary.HighChurnRiskCount.Should().BeGreaterThanOrEqualTo(0);
        summary.HighFrustrationRiskCount.Should().BeGreaterThanOrEqualTo(0);
        summary.GeneratedAt.Should().NotBe(default);
    }

    // ── GET /admin/personalization/archetypes ─────────────────────────────

    [Fact]
    public async Task GetArchetypes_Returns200_WithList()
    {
        var resp = await _http.GetAsync("/admin/personalization/archetypes");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /admin/personalization/recommendations/performance ────────────

    [Fact]
    public async Task GetRecommendationPerformance_Returns200_WithList()
    {
        var resp = await _http.GetAsync("/admin/personalization/recommendations/performance");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ── GET /admin/personalization/player/{playerId} ──────────────────────

    [Fact]
    public async Task GetPlayerProfile_UnknownPlayer_Returns200_WithNewProfile()
    {
        var playerId = Guid.NewGuid();
        var resp = await _http.GetAsync($"/admin/personalization/player/{playerId}");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await resp.Content.ReadFromJsonAsync<PlayerMindProfileDto>();
        profile.Should().NotBeNull();
        profile!.PlayerId.Should().Be(playerId);
    }

    // ── POST /admin/personalization/player/{playerId}/recalculate ─────────

    [Fact]
    public async Task RecalculatePlayer_Returns200_WithProfile()
    {
        var playerId = Guid.NewGuid();
        var resp = await _http.PostAsync($"/admin/personalization/player/{playerId}/recalculate", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var profile = await resp.Content.ReadFromJsonAsync<PlayerMindProfileDto>();
        profile.Should().NotBeNull();
        profile!.PlayerId.Should().Be(playerId);
    }

    // ── POST /admin/personalization/player/{playerId}/reset ───────────────

    [Fact]
    public async Task ResetPlayer_UnknownPlayer_Returns404()
    {
        var playerId = Guid.NewGuid();
        var resp = await _http.PostAsync($"/admin/personalization/player/{playerId}/reset", content: null);

        resp.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ResetPlayer_ExistingPlayer_Returns200()
    {
        var playerId = Guid.NewGuid();

        // Create a profile first via recalculate
        var recalcResp = await _http.PostAsync($"/admin/personalization/player/{playerId}/recalculate", content: null);
        recalcResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Now reset
        var resp = await _http.PostAsync($"/admin/personalization/player/{playerId}/reset", content: null);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        body.GetProperty("reset").GetBoolean().Should().BeTrue();
    }

    // ── GET /admin/personalization/rules ──────────────────────────────────

    [Fact]
    public async Task GetRules_Returns200_WithList()
    {
        var resp = await _http.GetAsync("/admin/personalization/rules");

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await resp.Content.ReadFromJsonAsync<List<PersonalizationRuleDto>>();
        rules.Should().NotBeNull();
    }

    // ── PUT /admin/personalization/rules (bulk) ───────────────────────────

    [Fact]
    public async Task BulkPutRules_CreatesNewRules_Returns200_WithUpsertedList()
    {
        var ruleKey1 = $"test_rule_{Guid.NewGuid():N}";
        var ruleKey2 = $"test_rule_{Guid.NewGuid():N}";

        var request = new BulkUpdatePersonalizationRulesRequest(
        [
            new BulkRuleUpdateItem(ruleKey1, IsEnabled: true, Rule: new Dictionary<string, object> { ["threshold"] = 0.5 }),
            new BulkRuleUpdateItem(ruleKey2, IsEnabled: false, Rule: null)
        ]);

        var resp = await _http.PutAsJsonAsync("/admin/personalization/rules", request);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rules = await resp.Content.ReadFromJsonAsync<List<PersonalizationRuleDto>>();
        rules.Should().NotBeNull();
        rules!.Should().HaveCount(2);
        rules.Should().Contain(r => r.RuleKey == ruleKey1 && r.IsEnabled);
        rules.Should().Contain(r => r.RuleKey == ruleKey2 && !r.IsEnabled);
    }

    [Fact]
    public async Task BulkPutRules_UpdatesExistingRule_Returns200()
    {
        var ruleKey = $"test_rule_{Guid.NewGuid():N}";

        // Create via individual PUT
        var createReq = new BulkUpdatePersonalizationRulesRequest(
        [
            new BulkRuleUpdateItem(ruleKey, IsEnabled: true, Rule: null)
        ]);
        var createResp = await _http.PutAsJsonAsync("/admin/personalization/rules", createReq);
        createResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Update via bulk PUT
        var updateReq = new BulkUpdatePersonalizationRulesRequest(
        [
            new BulkRuleUpdateItem(ruleKey, IsEnabled: false, Rule: null)
        ]);
        var updateResp = await _http.PutAsJsonAsync("/admin/personalization/rules", updateReq);
        updateResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var rules = await updateResp.Content.ReadFromJsonAsync<List<PersonalizationRuleDto>>();
        rules.Should().NotBeNull();
        rules!.Should().ContainSingle(r => r.RuleKey == ruleKey && !r.IsEnabled);
    }

    // ── PUT /admin/personalization/rules/{ruleKey} (individual) ──────────

    [Fact]
    public async Task PutRule_CreatesNewRule_Returns200_WithRuleDto()
    {
        var ruleKey = $"test_rule_{Guid.NewGuid():N}";
        var request = new UpdatePersonalizationRuleRequest(IsEnabled: true, Rule: new Dictionary<string, object> { ["value"] = 42 });

        var resp = await _http.PutAsJsonAsync($"/admin/personalization/rules/{ruleKey}", request);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await resp.Content.ReadFromJsonAsync<PersonalizationRuleDto>();
        rule.Should().NotBeNull();
        rule!.RuleKey.Should().Be(ruleKey);
        rule.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task PutRule_UpdatesExistingRule_Returns200_WithUpdatedDto()
    {
        var ruleKey = $"test_rule_{Guid.NewGuid():N}";

        // Create
        var createReq = new UpdatePersonalizationRuleRequest(IsEnabled: true, Rule: null);
        await _http.PutAsJsonAsync($"/admin/personalization/rules/{ruleKey}", createReq);

        // Update
        var updateReq = new UpdatePersonalizationRuleRequest(IsEnabled: false, Rule: null);
        var resp = await _http.PutAsJsonAsync($"/admin/personalization/rules/{ruleKey}", updateReq);

        resp.StatusCode.Should().Be(HttpStatusCode.OK);
        var rule = await resp.Content.ReadFromJsonAsync<PersonalizationRuleDto>();
        rule.Should().NotBeNull();
        rule!.IsEnabled.Should().BeFalse();
    }
}
