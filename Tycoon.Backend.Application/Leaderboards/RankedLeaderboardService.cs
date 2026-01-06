using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Seasons;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Leaderboards;

public sealed class RankedLeaderboardService(
    IAppDb db,
    IOptions<RankedSeasonOptions> rankedOptions)
{
    public async Task<RankedLeaderboardGridResponseDto> GetAsync(
        RankedLeaderboardQueryDto q,
        CancellationToken ct)
    {
        var season = await ResolveSeasonAsync(q.SeasonId, ct);

        if (season is null)
            return new RankedLeaderboardGridResponseDto(
                Page: q.Page,
                PageSize: q.PageSize,
                Total: 0,
                Scope: q.Scope,
                SeasonId: q.SeasonId ?? Guid.Empty,
                Tier: q.Tier,
                Items: Array.Empty<RankedLeaderboardItemDto>(),
                Meta: new Dictionary<string, string> { ["error"] = "No active season." },
                Columns: DefaultColumns());

        var seasonId = season.Id;
        var o = rankedOptions.Value;

        var page = Math.Max(1, q.Page);
        var pageSize = Math.Clamp(q.PageSize, 1, 100);

        var baseQuery = db.PlayerSeasonProfiles.AsNoTracking()
            .Where(x => x.SeasonId == seasonId);

        if (q.Scope.Equals("tier", StringComparison.OrdinalIgnoreCase) && q.Tier.HasValue)
            baseQuery = baseQuery.Where(x => x.Tier == q.Tier.Value);

        IOrderedQueryable<PlayerSeasonProfile> ordered = q.Sort?.ToLowerInvariant() switch
        {
            "tierrank" => baseQuery.OrderBy(x => x.TierRank).ThenByDescending(x => x.RankPoints),
            "seasonrank" => baseQuery.OrderBy(x => x.SeasonRank).ThenByDescending(x => x.RankPoints),
            _ => baseQuery.OrderByDescending(x => x.RankPoints).ThenBy(x => x.TierRank)
        };

        var total = await ordered.CountAsync(ct);

        // IMPORTANT: construct DTO positionally to avoid named-arg mismatch.
        var items = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new RankedLeaderboardItemDto(
                x.PlayerId,
                x.RankPoints,     // SeasonPoints (DTO)
                x.TierRank,
                x.SeasonRank,     // GlobalRank (DTO)
                x.Tier,
                x.Wins,
                x.Losses,
                x.Draws,
                x.PlacementMatchesCompleted,
                x.PlacementMatchesCompleted < o.PlacementMatchesRequired,
                x.TierRank > 0 && x.TierRank <= o.PromotionEligibleRank,
                x.TierRank > 0 && x.TierRank <= o.DailyRewardRank,
                x.UpdatedAtUtc
            ))
            .ToListAsync(ct);

        var meta = new Dictionary<string, string>
        {
            ["seasonName"] = season.Name ?? "Season",
            ["usersPerTier"] = o.UsersPerTier.ToString(),
            ["placementMatchesRequired"] = o.PlacementMatchesRequired.ToString(),
            ["promotionEligibleRank"] = o.PromotionEligibleRank.ToString(),
            ["dailyRewardRank"] = o.DailyRewardRank.ToString()
        };

        return new RankedLeaderboardGridResponseDto(
            Page: page,
            PageSize: pageSize,
            Total: total,
            Scope: q.Scope,
            SeasonId: seasonId,
            Tier: q.Tier,
            Items: items,
            Meta: meta,
            Columns: DefaultColumns()
        );
    }

    private async Task<Season?> ResolveSeasonAsync(Guid? seasonId, CancellationToken ct)
    {
        if (seasonId.HasValue)
        {
            return await db.Seasons.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == seasonId.Value, ct);
        }

        var now = DateTimeOffset.UtcNow;

        return await db.Seasons.AsNoTracking()
            .Where(s => s.StartsAtUtc <= now && s.EndsAtUtc > now)
            .OrderByDescending(s => s.StartsAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    private static IReadOnlyList<string> DefaultColumns() => new[]
    {
        "PlayerId",

        // Canonical (if you expose them via aliases in DTO)
        "RankPoints", "SeasonRank",

        // Compatibility aliases
        "SeasonPoints","GlobalRank",

        "Tier","TierRank",
        "Wins","Losses","Draws",
        "PlacementMatchesCompleted","IsPlacement",
        "EligibleForPromotion","EligibleForDailyReward",
        "LastUpdatedUtc"
    };
}
