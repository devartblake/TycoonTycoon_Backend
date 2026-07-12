using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Entitlements.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.GameEvents;

/// <summary>
/// Premium spectator layer for champion_vs_tier matches. The basic live view
/// (round question, alive count, jackpot — delivered over SignalR) is free for
/// everyone; a premium spectator pass entitlement additionally unlocks the
/// "elimination cam" feed. This keeps the audience open (Phase 3's goal) while
/// selling an enhanced experience on top.
/// </summary>
public sealed class ChampionSpectatorService(IAppDb db)
{
    public const string PremiumSku = "spectator_premium";

    /// <summary>Does the player hold an active premium spectator pass?</summary>
    public async Task<bool> HasPremiumAsync(Guid playerId, CancellationToken ct)
    {
        if (playerId == Guid.Empty)
            return false;
        var now = DateTimeOffset.UtcNow;
        return await db.PlayerEntitlements.AsNoTracking().AnyAsync(
            x => x.PlayerId == playerId
                 && x.Sku == PremiumSku
                 && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now),
            ct);
    }

    /// <summary>
    /// Build the spectator view for a match: basic counts for everyone, plus the
    /// elimination cam feed when the caller holds the premium pass.
    /// </summary>
    public async Task<ChampionSpectatorViewDto?> GetViewAsync(Guid gameEventId, Guid playerId, CancellationToken ct)
    {
        var ev = await db.GameEvents.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == gameEventId, ct);
        if (ev is null || ev.Kind != GameEvent.ChampionVsTierKind)
            return null;

        var aliveCount = await db.GameEventParticipants
            .CountAsync(x => x.GameEventId == gameEventId && x.EliminatedAt == null, ct);

        var isPremium = await HasPremiumAsync(playerId, ct);

        IReadOnlyList<ChampionEliminationDto> feed = [];
        if (isPremium)
        {
            var eliminated = await db.GameEventParticipants.AsNoTracking()
                .Where(x => x.GameEventId == gameEventId && x.EliminatedAt != null)
                .OrderBy(x => x.EliminatedAt)
                .Select(x => new { x.PlayerId, x.EliminatedAt, x.FinalRank })
                .ToListAsync(ct);

            var ids = eliminated.Select(e => e.PlayerId).ToList();
            var handles = await db.Users.AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .Select(u => new { u.Id, u.Handle })
                .ToDictionaryAsync(u => u.Id, u => u.Handle, ct);

            feed = eliminated.Select(e => new ChampionEliminationDto(
                e.PlayerId,
                handles.GetValueOrDefault(e.PlayerId, "unknown"),
                e.EliminatedAt!.Value,
                e.PlayerId == ev.ChampionPlayerId,
                e.FinalRank)).ToList();
        }

        return new ChampionSpectatorViewDto(
            gameEventId,
            ev.Status == GameEventStatus.Live,
            isPremium,
            aliveCount,
            ev.JackpotPool,
            feed);
    }

    /// <summary>Grant the premium spectator pass (admin comp/support path).</summary>
    public async Task GrantPassAsync(Guid playerId, int? days, CancellationToken ct)
    {
        var expiresAt = days is > 0 ? DateTimeOffset.UtcNow.AddDays(days.Value) : (DateTimeOffset?)null;
        var entitlement = PlayerEntitlement.Grant(
            playerId,
            PremiumSku,
            itemType: "spectator_pass",
            quantity: 1,
            sourceTransactionId: Guid.NewGuid(),
            scope: expiresAt is null ? "permanent" : "seasonal",
            expiresAt: expiresAt);
        db.PlayerEntitlements.Add(entitlement);
        await db.SaveChangesAsync(ct);
    }
}
