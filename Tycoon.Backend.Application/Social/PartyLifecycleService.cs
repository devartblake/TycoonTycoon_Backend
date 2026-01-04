using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Social
{
    /// <summary>
    /// Ensures party state remains consistent through the match lifecycle.
    /// </summary>
    public sealed class PartyLifecycleService(
        IAppDb db,
        IPartyMatchmakingNotifier notifier,
        IPresenceReader presence)
    {
        public async Task ClosePartiesForMatchAsync(Guid matchId, string reason, CancellationToken ct)
        {
            if (matchId == Guid.Empty) return;

            // Find active links
            var links = await db.PartyMatchLinks
                .Where(x => x.MatchId == matchId && x.Status != "Closed")
                .ToListAsync(ct);

            if (links.Count == 0) return;

            var partyIds = links.Select(x => x.PartyId).Distinct().ToList();

            // Load parties + members
            var parties = await db.Parties
                .Where(p => partyIds.Contains(p.Id))
                .ToListAsync(ct);

            var members = await db.PartyMembers.AsNoTracking()
                .Where(m => partyIds.Contains(m.PartyId))
                .ToListAsync(ct);

            // Close parties and links
            foreach (var link in links)
                link.MarkClosed();

            foreach (var p in parties)
            {
                // Prefer explicit method if you have it. If not, do minimal state change.
                // Status expected values: Open | Queued | Matched | Closed
                typeof(Party).GetProperty(nameof(Party.Status))!.SetValue(p, "Closed");
            }

            await db.SaveChangesAsync(ct);

            // Notify each party’s members
            foreach (var pid in partyIds)
            {
                var partyMemberIds = members.Where(m => m.PartyId == pid).Select(m => m.PlayerId).Distinct().ToList();

                // Build roster snapshot (use your existing PartyService roster logic if preferred).
                // Here: call PartyService-style query inline for minimal dependencies.
                var party = parties.First(x => x.Id == pid);
                var roster = new Tycoon.Shared.Contracts.Dtos.PartyRosterDto(
                    PartyId: party.Id,
                    LeaderPlayerId: party.LeaderPlayerId,
                    Status: "Closed",
                    Members: members.Where(m => m.PartyId == pid)
                        .OrderBy(m => m.JoinedAtUtc)
                        .Select(m => new Tycoon.Shared.Contracts.Dtos.PartyMemberDto(
                            PlayerId: m.PlayerId,
                            Role: m.Role.ToString(),
                            JoinedAtUtc: m.JoinedAtUtc
                        ))
                        .ToList()
                );

                var online = await presence.GetOnlineAsync(partyMemberIds, ct);

                await notifier.NotifyRosterUpdatedAsync(roster, partyMemberIds, online, ct);
                await notifier.NotifyPartyClosedAsync(pid, matchId, partyMemberIds, reason, ct);
            }
        }
    }
}
