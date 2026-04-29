using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Personalization;
using Tycoon.Backend.Domain.Personalization;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Features.AdminPersonalization;

public static class AdminPersonalizationEndpoints
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public static void Map(RouteGroupBuilder admin)
    {
        var g = admin.MapGroup("/personalization").WithTags("Admin/Personalization").WithOpenApi();

        g.MapGet("/summary", GetSummary);
        g.MapGet("/archetypes", GetArchetypes);
        g.MapGet("/recommendations/performance", GetRecommendationPerformance);
        g.MapGet("/player/{playerId:guid}", GetPlayerProfile);
        g.MapPost("/player/{playerId:guid}/recalculate", RecalculatePlayer);
        g.MapPost("/player/{playerId:guid}/reset", ResetPlayer);
        g.MapGet("/rules", GetRules);
        g.MapPut("/rules/{ruleKey}", UpsertRule);
    }

    private static async Task<IResult> GetSummary(IAppDb db, CancellationToken ct)
    {
        var profiles = await db.PlayerMindProfiles.AsNoTracking().ToListAsync(ct);

        var archetypeCounts = profiles
            .GroupBy(p => p.Archetype)
            .ToDictionary(g => g.Key, g => g.Count());

        var summary = new PersonalizationSummaryDto(
            ArchetypeCounts: archetypeCounts,
            HighChurnRiskCount: profiles.Count(p => p.ChurnRiskScore >= 0.65m),
            HighFrustrationRiskCount: profiles.Count(p => p.FrustrationRiskScore >= 0.65m),
            TotalProfiles: profiles.Count,
            GeneratedAt: DateTimeOffset.UtcNow);

        return Results.Ok(summary);
    }

    private static async Task<IResult> GetArchetypes(IAppDb db, CancellationToken ct)
    {
        var counts = await db.PlayerMindProfiles
            .AsNoTracking()
            .GroupBy(p => p.Archetype)
            .Select(g => new { Archetype = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToListAsync(ct);

        return Results.Ok(counts);
    }

    private static async Task<IResult> GetRecommendationPerformance(IAppDb db, CancellationToken ct)
    {
        var recs = await db.PersonalizationRecommendations
            .AsNoTracking()
            .ToListAsync(ct);

        var performance = recs
            .GroupBy(r => r.RecommendationType)
            .Select(g => new
            {
                Type = g.Key,
                Total = g.Count(),
                Accepted = g.Count(r => r.AcceptedAt.HasValue),
                Dismissed = g.Count(r => r.DismissedAt.HasValue),
                Pending = g.Count(r => r.AcceptedAt == null && r.DismissedAt == null),
                AcceptanceRate = g.Count() == 0 ? 0.0 : Math.Round((double)g.Count(r => r.AcceptedAt.HasValue) / g.Count(), 4),
                DismissalRate = g.Count() == 0 ? 0.0 : Math.Round((double)g.Count(r => r.DismissedAt.HasValue) / g.Count(), 4)
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        return Results.Ok(performance);
    }

    private static async Task<IResult> GetPlayerProfile(
        Guid playerId, IPlayerMindProfileService profiles, CancellationToken ct)
    {
        var profile = await profiles.GetOrCreateAsync(playerId, ct);
        return Results.Ok(profile);
    }

    private static async Task<IResult> RecalculatePlayer(
        Guid playerId, IPlayerMindProfileService profiles, CancellationToken ct)
    {
        var profile = await profiles.RecalculateAsync(playerId, ct);
        return Results.Ok(profile);
    }

    private static async Task<IResult> ResetPlayer(Guid playerId, IAppDb db, CancellationToken ct)
    {
        var profile = await db.PlayerMindProfiles.FirstOrDefaultAsync(p => p.PlayerId == playerId, ct);
        if (profile is null)
            return Results.NotFound();

        profile.ConfidenceLevel = 0.50m;
        profile.RiskTolerance = 0.50m;
        profile.PreferredPace = "balanced";
        profile.LearningStyle = "mixed";
        profile.CompetitivePreference = "balanced";
        profile.SocialPreference = "solo";
        profile.ChurnRiskScore = 0.00m;
        profile.FrustrationRiskScore = 0.00m;
        profile.RewardSensitivityScore = 0.50m;
        profile.StoreAffinityScore = 0.50m;
        profile.NotificationFatigueScore = 0.00m;
        profile.Archetype = "new_player";
        profile.CategoryStrengthsJson = "{}";
        profile.CategoryWeaknessesJson = "{}";
        profile.PreferenceJson = "{}";
        profile.GuardrailJson = "{}";
        profile.SidecarScoresJson = "{}";
        profile.LastCalculatedAt = null;
        profile.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Results.Ok(new { reset = true, playerId });
    }

    private static async Task<IResult> GetRules(IAppDb db, CancellationToken ct)
    {
        var rules = await db.PersonalizationRules
            .AsNoTracking()
            .OrderBy(r => r.RuleKey)
            .ToListAsync(ct);

        var dtos = rules.Select(r => new PersonalizationRuleDto(
            r.Id, r.RuleKey, r.Description, r.IsEnabled,
            ParseJson(r.RuleJson),
            r.UpdatedAt)).ToList();

        return Results.Ok(dtos);
    }

    private static async Task<IResult> UpsertRule(
        string ruleKey,
        [FromBody] UpdatePersonalizationRuleRequest request,
        IAppDb db,
        CancellationToken ct)
    {
        var rule = await db.PersonalizationRules
            .FirstOrDefaultAsync(r => r.RuleKey == ruleKey, ct);

        if (rule is null)
        {
            rule = new PersonalizationRule
            {
                Id = Guid.NewGuid(),
                RuleKey = ruleKey,
                Description = "",
                IsEnabled = request.IsEnabled ?? true,
                RuleJson = request.Rule is not null
                    ? JsonSerializer.Serialize(request.Rule, _json)
                    : "{}",
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };
            db.PersonalizationRules.Add(rule);
        }
        else
        {
            if (request.IsEnabled.HasValue)
                rule.IsEnabled = request.IsEnabled.Value;
            if (request.Rule is not null)
                rule.RuleJson = JsonSerializer.Serialize(request.Rule, _json);
            rule.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(ct);

        return Results.Ok(new PersonalizationRuleDto(
            rule.Id, rule.RuleKey, rule.Description, rule.IsEnabled,
            ParseJson(rule.RuleJson), rule.UpdatedAt));
    }

    private static Dictionary<string, object> ParseJson(string json)
    {
        try { return JsonSerializer.Deserialize<Dictionary<string, object>>(json, _json) ?? []; }
        catch { return []; }
    }
}
