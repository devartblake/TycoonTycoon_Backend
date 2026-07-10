using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents;

public sealed record ChampionPredictionOptions
{
    public bool Enabled { get; init; } = true;

    /// <summary>Fixed coin pool split evenly among correct predictors (bounded operator spend).</summary>
    public int RewardCoinPool { get; init; } = 1000;

    /// <summary>Flat XP awarded to each correct predictor (a participation reward on top of the split).</summary>
    public int CorrectXp { get; init; } = 25;
}

/// <summary>
/// No-loss prediction on a champion_vs_tier match ("will the champion defend?").
/// Open to the whole player base while the event is in its Open window; correct
/// predictors share a fixed coin pool at close. Nothing is ever staked, so this
/// is an engagement hook, not gambling — safe for the app's minor users.
/// </summary>
public sealed class ChampionPredictionService(
    IAppDb db,
    IEconomyService economy,
    IOptions<ChampionPredictionOptions> options)
{
    public const string RewardKind = "champion-prediction-reward";

    /// <summary>Place or change a prediction. Allowed only while the event is Open.</summary>
    public async Task<string> PredictAsync(Guid gameEventId, Guid playerId, bool championDefends, CancellationToken ct)
    {
        if (!options.Value.Enabled)
            return "Disabled";

        var ev = await db.GameEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null || ev.Kind != GameEvent.ChampionVsTierKind)
            return "NotFound";
        if (ev.Status != GameEventStatus.Open)
            return "Closed"; // predictions lock once the match goes live

        var existing = await db.ChampionPredictions
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.PlayerId == playerId, ct);
        if (existing is null)
            db.ChampionPredictions.Add(new ChampionPrediction(gameEventId, playerId, championDefends));
        else if (!existing.Resolved)
            existing.Update(championDefends);
        else
            return "Closed";

        await db.SaveChangesAsync(ct);
        return "Accepted";
    }

    /// <summary>
    /// Resolve all outstanding predictions once the match's outcome is known.
    /// Idempotent: already-resolved predictions are skipped, and reward payouts
    /// dedupe on a deterministic economy event id.
    /// </summary>
    public async Task ResolveAsync(Guid gameEventId, bool championDefended, CancellationToken ct)
    {
        var predictions = await db.ChampionPredictions
            .Where(x => x.GameEventId == gameEventId && !x.Resolved)
            .ToListAsync(ct);
        if (predictions.Count == 0)
            return;

        var correct = predictions.Where(p => p.PredictedChampionDefends == championDefended).ToList();
        var perWinnerCoins = correct.Count > 0 ? options.Value.RewardCoinPool / correct.Count : 0;
        var correctXp = options.Value.CorrectXp;
        var now = DateTimeOffset.UtcNow;

        foreach (var p in predictions)
            p.Resolve(championDefended, perWinnerCoins, correctXp, now);

        await db.SaveChangesAsync(ct);

        foreach (var p in correct)
        {
            if (p.RewardCoins <= 0 && p.RewardXp <= 0)
                continue;
            await economy.ApplyAsync(new CreateEconomyTxnRequest(
                EventId: DeterministicGuid(gameEventId, p.PlayerId),
                PlayerId: p.PlayerId,
                Kind: RewardKind,
                Lines: new[]
                {
                    new EconomyLineDto(CurrencyType.Coins, p.RewardCoins),
                    new EconomyLineDto(CurrencyType.Xp, p.RewardXp),
                },
                Note: $"prediction:{gameEventId}"), ct);
        }
    }

    /// <summary>The caller's prediction state + live tally for the UI.</summary>
    public async Task<ChampionPredictionStateDto?> GetStateAsync(Guid gameEventId, Guid playerId, CancellationToken ct)
    {
        var ev = await db.GameEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null || ev.Kind != GameEvent.ChampionVsTierKind)
            return null;

        var mine = await db.ChampionPredictions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.GameEventId == gameEventId && x.PlayerId == playerId, ct);

        var defendCount = await db.ChampionPredictions
            .CountAsync(x => x.GameEventId == gameEventId && x.PredictedChampionDefends, ct);
        var dethroneCount = await db.ChampionPredictions
            .CountAsync(x => x.GameEventId == gameEventId && !x.PredictedChampionDefends, ct);

        return new ChampionPredictionStateDto(
            gameEventId,
            ev.Status == GameEventStatus.Open,
            mine?.PredictedChampionDefends,
            defendCount,
            dethroneCount,
            options.Value.RewardCoinPool,
            mine?.Resolved ?? false,
            mine?.WasCorrect,
            mine?.RewardCoins ?? 0,
            mine?.RewardXp ?? 0);
    }

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
