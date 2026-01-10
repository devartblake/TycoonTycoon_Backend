using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons;

public sealed class SeasonRewardsService(
    IAppDb db,
    EconomyService economy,
    IOptions<RankedSeasonOptions> rankedOptions)
{
    public async Task<RewardEligibilityDto> GetEligibilityAsync(Guid playerId, Guid? seasonId, CancellationToken ct)
    {
        var season = await ResolveSeasonAsync(seasonId, ct);
        if (season is null)
            return new RewardEligibilityDto(Guid.Empty, playerId, false, "NoActiveSeason", 0, 0, 0, 0, 0, null);

        var sid = season.Id;
        var o = rankedOptions.Value;

        var profile = await db.PlayerSeasonProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SeasonId == sid && x.PlayerId == playerId, ct);

        if (profile is null)
            return new RewardEligibilityDto(sid, playerId, false, "NoProfile", 0, 0, 0, 0, 0, null);

        if (profile.PlacementMatchesCompleted < o.PlacementMatchesRequired)
            return new RewardEligibilityDto(sid, playerId, false, "Placement", profile.Tier, profile.TierRank, profile.RankPoints, 0, 0, null);

        if (profile.TierRank <= 0 || profile.TierRank > o.DailyRewardRank)
            return new RewardEligibilityDto(sid, playerId, false, "NotInTopDailyReward", profile.Tier, profile.TierRank, profile.RankPoints, 0, 0, null);

        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        var already = await db.SeasonRewardClaims.AsNoTracking()
            .AnyAsync(x => x.SeasonId == sid && x.PlayerId == playerId && x.RewardDay == day, ct);

        if (already)
        {
            var next = DateTimeOffset.UtcNow.Date.AddDays(1);
            return new RewardEligibilityDto(sid, playerId, false, "AlreadyClaimed", profile.Tier, profile.TierRank, profile.RankPoints, 0, 0, next);
        }

        var (coins, xp) = ComputeDailyReward(profile.TierRank);

        return new RewardEligibilityDto(
            sid, playerId, true, "Eligible",
            profile.Tier, profile.TierRank, profile.RankPoints,
            coins, xp,
            null
        );
    }

    public async Task<ClaimSeasonRewardResponseDto> ClaimAsync(Guid playerId, ClaimSeasonRewardRequestDto req, CancellationToken ct)
    {
        var season = await ResolveSeasonAsync(req.SeasonId, ct);
        if (season is null)
            return new ClaimSeasonRewardResponseDto(req.EventId, Guid.Empty, playerId, "NotEligible", 0, 0);

        var sid = season.Id;

        var dup = await db.SeasonRewardClaims.AsNoTracking()
            .FirstOrDefaultAsync(x => x.EventId == req.EventId, ct);

        if (dup is not null)
            return new ClaimSeasonRewardResponseDto(req.EventId, sid, playerId, "Duplicate", dup.AwardedCoins, dup.AwardedXp);

        var eligibility = await GetEligibilityAsync(playerId, sid, ct);
        if (!eligibility.Eligible)
            return new ClaimSeasonRewardResponseDto(req.EventId, sid, playerId, "NotEligible", 0, 0);

        var day = DateOnly.FromDateTime(DateTime.UtcNow);

        var already = await db.SeasonRewardClaims.AsNoTracking()
            .AnyAsync(x => x.SeasonId == sid && x.PlayerId == playerId && x.RewardDay == day, ct);

        if (already)
            return new ClaimSeasonRewardResponseDto(req.EventId, sid, playerId, "NotEligible", 0, 0);

        await economy.ApplyAsync(new CreateEconomyTxnRequest(
            EventId: req.EventId,
            PlayerId: playerId,
            Kind: "season-daily-reward",
            Lines: new[]
            {
                new EconomyLineDto(CurrencyType.Coins, eligibility.RewardCoins),
                new EconomyLineDto(CurrencyType.Xp, eligibility.RewardXp),
            },
            Note: $"season:{sid}:day:{day}"
        ), ct);

        db.SeasonRewardClaims.Add(new SeasonRewardClaim(
            seasonId: sid,
            playerId: playerId,
            eventId: req.EventId,
            rewardDay: day,
            awardedCoins: eligibility.RewardCoins,
            awardedXp: eligibility.RewardXp
        ));

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            return new ClaimSeasonRewardResponseDto(req.EventId, sid, playerId, "NotEligible", 0, 0);
        }

        return new ClaimSeasonRewardResponseDto(req.EventId, sid, playerId, "Applied", eligibility.RewardCoins, eligibility.RewardXp);
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

    private static (int coins, int xp) ComputeDailyReward(int tierRank)
    {
        if (tierRank <= 1) return (100, 200);
        if (tierRank <= 5) return (60, 120);
        if (tierRank <= 10) return (35, 80);
        return (20, 50);
    }
}
