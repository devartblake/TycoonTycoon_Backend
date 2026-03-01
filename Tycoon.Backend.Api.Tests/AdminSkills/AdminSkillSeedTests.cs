using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Tycoon.Backend.Api.Tests.TestHost;
using Tycoon.Shared.Contracts.Dtos;
using Xunit;

namespace Tycoon.Backend.Api.Tests.AdminSkills;

public sealed class AdminSkillSeedTests : IClassFixture<TycoonApiFactory>
{
    private readonly HttpClient _admin;

    public AdminSkillSeedTests(TycoonApiFactory factory)
    {
        _admin = factory.CreateClient().WithAdminOpsKey();
    }

    [Fact]
    public async Task SeedSkills_Rejects_Wrong_AdminOpsKey()
    {
        using var wrongKey = new TycoonApiFactory().CreateClient().WithAdminOpsKey("wrong-key");

        var resp = await wrongKey.PostAsJsonAsync("/admin/skills/seed",
            new SkillTreeCatalogDto(Array.Empty<SkillNodeDto>()));

        resp.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await resp.HasErrorCodeAsync("FORBIDDEN");
    }

    [Fact]
    public async Task SeedSkills_Requires_AdminOpsKey()
    {
        var noKey = new TycoonApiFactory().CreateClient();

        var resp = await noKey.PostAsJsonAsync("/admin/skills/seed",
            new SkillTreeCatalogDto(Array.Empty<SkillNodeDto>()));

        resp.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        await resp.HasErrorCodeAsync("UNAUTHORIZED");
    }

    [Fact]
    public async Task SeedSkills_Upserts_Nodes()
    {
        var req = new SkillTreeCatalogDto(new[]
        {
            new SkillNodeDto(
                Key: "test.skill.one",
                Branch: SkillBranch.Knowledge,
                Tier: 1,
                Title: "Test Skill One",
                Description: "First skill",
                PrereqKeys: Array.Empty<string>(),
                Costs: new[] { new SkillCostDto(CurrencyType.Coins, 50) },
                Effects: new Dictionary<string,double> { ["xp_mult"] = 0.1 }
            )
        });

        var r = await _admin.PostAsJsonAsync("/admin/skills/seed", req);
        r.EnsureSuccessStatusCode();

        var body = await r.Content.ReadFromJsonAsync<dynamic>();
        ((int)body!.upserted).Should().BeGreaterThanOrEqualTo(1);
    }
}
