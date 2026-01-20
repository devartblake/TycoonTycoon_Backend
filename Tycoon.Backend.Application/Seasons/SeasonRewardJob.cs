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

        var rows = await _db.SeasonRankSnapshots
        .AsNoTracking()
        .Where(r => r.SeasonId == seasonId)
        .ToListAsync(ct);

        foreach (var r in rows)
        {
            var rule = rules.FirstOrDefault(x => x.Tier == r.Tier && r.TierRank <= x.MaxTierRank);
            if (rule is null) continue;

            await _economy.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: DeterministicGuid(seasonId, r.PlayerId),
                PlayerId: r.PlayerId,
                Kind: "season-reward",
                Lines: new[]
                {
            new EconomyLineDto(CurrencyType.Xp, rule.RewardXp),
            new EconomyLineDto(CurrencyType.Coins, rule.RewardCoins),
                },
                Note: $"season:{seasonId}:final"
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
