using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Application.Seasons;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.GameEvents;

/// <summary>
/// Seeds the tier's current #1 (from the active season's leaderboard) as the
/// champion of a champion_vs_tier event when it opens. The champion is invited
/// — enrolled as a participant with no entry fee — so leaderboard position
/// literally becomes event power. Idempotent: safe to call on every Open sweep.
/// </summary>
public sealed class TierChampionSeeder(IAppDb db, SeasonService seasons)
{
    public async Task SeedAsync(GameEvent ev, CancellationToken ct)
    {
        if (ev.Kind != GameEvent.ChampionVsTierKind || ev.ChampionPlayerId is not null)
            return;

        var active = await seasons.GetActiveAsync(ct);
        if (active is null)
            return;

        // Tier #1 by the same ordering the public leaderboard uses.
        var champion = await db.PlayerSeasonProfiles.AsNoTracking()
            .Where(x => x.SeasonId == active.SeasonId && x.Tier == ev.TierId)
            .OrderByDescending(x => x.RankPoints)
            .ThenByDescending(x => x.Wins)
            .ThenBy(x => x.MatchesPlayed)
            .ThenBy(x => x.PlayerId)
            .Select(x => x.PlayerId)
            .FirstOrDefaultAsync(ct);

        if (champion == Guid.Empty)
            return;

        ev.SeedChampion(champion);

        // Enrol the champion as a participant (entry fee waived — invited, not
        // a paying challenger). Deterministic entry id keeps this idempotent.
        var alreadyIn = await db.GameEventParticipants
            .AnyAsync(x => x.GameEventId == ev.Id && x.PlayerId == champion, ct);
        if (!alreadyIn)
        {
            var entryId = DeterministicGuid(ev.Id, champion);
            db.GameEventParticipants.Add(new GameEventParticipant(ev.Id, champion, entryId));
        }
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
