using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Economy;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Seasons;

public sealed class SeasonRewardJob
{
    private readonly IAppDb _db;
    private readonly EconomyService _economy;
    private readonly IOptions<SeasonRewardOptions> _rewardOptions;

    public SeasonRewardJob(IAppDb db, EconomyService economy, IOptions<SeasonRewardOptions> rewardOptions)
    {
        _db = db;
        _economy = economy;
        _rewardOptions = rewardOptions;
    }

    public async Task RunAsync(Guid seasonId, CancellationToken ct)
    {
        var rules = _rewardOptions.Value.Rules;
        if (rules is null || rules.Count == 0)
            return;

        var profiles = await _db.PlayerSeasonProfiles
            .Where(p => p.SeasonId == seasonId)
            .ToListAsync(ct);

        foreach (var p in profiles)
        {
            var rule = rules.FirstOrDefault(r => r.Tier == p.Tier && p.TierRank <= r.MaxTierRank);
            if (rule is null)
                continue;

            await _economy.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: DeterministicGuid(seasonId, p.PlayerId),
                PlayerId: p.PlayerId,
                Kind: "season-reward",
                Lines: new[]
                {
                    new EconomyLineDto(CurrencyType.Xp, rule.RewardXp),
                    new EconomyLineDto(CurrencyType.Coins, rule.RewardCoins),
                },
                Note: $"season:{seasonId}"
            ), ct);
        }
    }

    // Stable per (seasonId, playerId) so re-runs are idempotent.
    private static Guid DeterministicGuid(Guid a, Guid b)
    {
        Span<byte> bytes = stackalloc byte[32];
        a.TryWriteBytes(bytes[..16]);
        b.TryWriteBytes(bytes[16..]);

        Span<byte> folded = stackalloc byte[16];
        for (var i = 0; i < 16; i++)
            folded[i] = (byte)(bytes[i] ^ bytes[i + 16]);

        return new Guid(folded);
    }
}
