using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Enforcement;
using Tycoon.Backend.Application.Matches;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Social
{
    public sealed class PartyMatchmakingService(
        IAppDb db,
        EnforcementService enforcement,
        IPartyMatchmakingNotifier notifier,
        IMediator mediator)
    {
        private static readonly TimeSpan TicketTtl = TimeSpan.FromMinutes(2);

        public async Task<PartyQueueResultDto> EnqueuePartyAsync(Guid partyId, Guid leaderPlayerId, string mode, int tier, CancellationToken ct)
        {
            if (partyId == Guid.Empty || leaderPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            mode = string.IsNullOrWhiteSpace(mode) ? "ranked" : mode.Trim();
            tier = tier <= 0 ? 1 : tier;

            // Load party + members
            var party = await db.Parties.FirstOrDefaultAsync(x => x.Id == partyId, ct);
            if (party is null)
                throw new InvalidOperationException("Party not found.");

            if (party.LeaderPlayerId != leaderPlayerId)
                throw new InvalidOperationException("Only the party leader can enqueue.");

            if (party.Status != "Open")
                return new PartyQueueResultDto("PartyNotReady", null, partyId, null, null);

            var members = await db.PartyMembers.AsNoTracking()
                .Where(x => x.PartyId == partyId)
                .OrderBy(x => x.JoinedAtUtc)
                .Select(x => x.PlayerId)
                .ToListAsync(ct);

            if (members.Count < 2)
                return new PartyQueueResultDto("PartyNotReady", null, partyId, null, null);

            // Enforcement across members:
            // - If ANY member cannot start => Forbidden
            // - Scope is the "most restrictive" from member decisions (TierOnly overrides Global)
            string scope = "Global";
            foreach (var pid in members)
            {
                var d = await enforcement.EvaluateAsync(pid, ct);
                if (!d.CanStartMatch)
                    return new PartyQueueResultDto("Forbidden", null, partyId, null, null);

                // If any restricted => TierOnly
                if (string.Equals(d.QueueScope, "TierOnly", StringComparison.OrdinalIgnoreCase))
                    scope = "TierOnly";
            }

            // Cleanup expired tickets
            await db.PartyMatchmakingTickets
                .Where(t => t.Status == "Queued" && t.ExpiresAtUtc <= DateTimeOffset.UtcNow)
                .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Cancelled"), ct);

            // Idempotent: only one queued ticket per party
            var existing = await db.PartyMatchmakingTickets
                .FirstOrDefaultAsync(t => t.PartyId == partyId && t.Status == "Queued", ct);

            if (existing is not null)
                return new PartyQueueResultDto("Queued", existing.Id, partyId, null, null);

            // Mark party queued + create ticket
            party.MarkQueued();

            var ticket = new PartyMatchmakingTicket(
                partyId: partyId,
                leaderPlayerId: leaderPlayerId,
                mode: mode,
                tier: tier,
                scope: scope,
                partySize: members.Count,
                ttl: TicketTtl);

            db.PartyMatchmakingTickets.Add(ticket);

            await db.SaveChangesAsync(ct);

            // Try match (retry once on concurrency)
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var matched = await TryMatchAsync(ticket.Id, members, mode, tier, scope, ct);
                if (matched is not null)
                    return matched;
            }

            return new PartyQueueResultDto("Queued", ticket.Id, partyId, null, null);
        }

        public async Task CancelPartyQueueAsync(Guid partyId, Guid leaderPlayerId, CancellationToken ct)
        {
            var party = await db.Parties.FirstOrDefaultAsync(x => x.Id == partyId, ct);
            if (party is null) return;

            if (party.LeaderPlayerId != leaderPlayerId)
                throw new InvalidOperationException("Only the party leader can cancel queue.");

            var ticket = await db.PartyMatchmakingTickets
                .FirstOrDefaultAsync(x => x.PartyId == partyId && x.Status == "Queued", ct);

            if (ticket is not null)
                ticket.Cancel();

            // move party back to Open if it was queued
            if (party.Status == "Queued")
            {
                // no direct "reopen" method; keep simple with a small state edit
                typeof(Party).GetProperty(nameof(Party.Status))!.SetValue(party, "Open");
            }

            await db.SaveChangesAsync(ct);
        }

        private async Task<PartyQueueResultDto?> TryMatchAsync(
            Guid ticketId,
            IReadOnlyList<Guid> selfMembers,
            string mode,
            int tier,
            string scope,
            CancellationToken ct)
        {
            await using var tx = await ((DbContext)db).Database.BeginTransactionAsync(ct);

            var selfTicket = await db.PartyMatchmakingTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

            if (selfTicket is null || selfTicket.Status != "Queued")
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            // Find opponent ticket FIFO
            var oppTicket = await db.PartyMatchmakingTickets
                .OrderBy(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(t =>
                    t.Status == "Queued" &&
                    t.Id != ticketId &&
                    t.Mode == mode &&
                    t.Scope == scope &&
                    t.PartySize == selfTicket.PartySize &&
                    (scope == "TierOnly" ? t.Tier == tier : true), ct);

            if (oppTicket is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            // Lock party rows and move state to Matched
            var selfParty = await db.Parties.FirstOrDefaultAsync(p => p.Id == selfTicket.PartyId, ct);
            var oppParty = await db.Parties.FirstOrDefaultAsync(p => p.Id == oppTicket.PartyId, ct);

            if (selfParty is null || oppParty is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            if (selfParty.Status != "Queued" || oppParty.Status != "Queued")
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            selfTicket.MarkMatched();
            oppTicket.MarkMatched();

            selfParty.MarkMatched();
            oppParty.MarkMatched();

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // Notify both parties (to all members via player groups)
                var oppMembers = await db.PartyMembers.AsNoTracking()
                    .Where(x => x.PartyId == oppTicket.PartyId)
                    .OrderBy(x => x.JoinedAtUtc)
                    .Select(x => x.PlayerId)
                    .ToListAsync(ct);

                // Create ONE server match for both parties.
                // Deterministic host: choose lower Leader GUID to stabilize.
                var hostLeader = selfParty.LeaderPlayerId.CompareTo(oppParty.LeaderPlayerId) <= 0
                    ? selfParty.LeaderPlayerId
                    : oppParty.LeaderPlayerId;

                var startMode = $"{mode}-party"; // e.g. ranked-party
                var started = await mediator.Send(new StartMatch(hostLeader, startMode), ct);
                var matchId = started.MatchId;

                // Notify both parties (to all members via player groups)
                await notifier.NotifyPartyMatchedAsync(
                    partyId: selfTicket.PartyId,
                    opponentPartyId: oppTicket.PartyId,
                    matchId: matchId,
                    memberPlayerIds: selfMembers,
                    mode: mode,
                    tier: tier,
                    scope: scope,
                    ticketId: selfTicket.Id,
                    ct: ct);

                await notifier.NotifyPartyMatchedAsync(
                    partyId: oppTicket.PartyId,
                    opponentPartyId: selfTicket.PartyId,
                    matchId: matchId,
                    memberPlayerIds: oppMembers,
                    mode: mode,
                    tier: tier,
                    scope: scope,
                    ticketId: oppTicket.Id,
                    ct: ct);

                // Return matched response including MatchId (for the enqueuer)
                return new PartyQueueResultDto("Matched", selfTicket.Id, selfTicket.PartyId, oppTicket.PartyId, matchId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                return null;
            }
        }
    }
}
