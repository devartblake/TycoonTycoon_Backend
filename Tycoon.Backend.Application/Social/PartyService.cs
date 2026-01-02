using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Realtime;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Social
{
    public sealed class PartyService(IAppDb db, IPartyMatchmakingNotifier notifier, IPresenceReader presence)
    {
        // MVP: 2-player party (duos). Raise later as needed.
        private const int MaxPartySize = 2;

        public async Task<PartyRosterDto> CreatePartyAsync(Guid leaderPlayerId, CancellationToken ct)
        {
            if (leaderPlayerId == Guid.Empty)
                throw new ArgumentException("leaderPlayerId cannot be empty.");

            // Enforce: player cannot already be in an active party
            var inActiveParty = await IsInActivePartyAsync(leaderPlayerId, ct);
            if (inActiveParty)
                throw new InvalidOperationException("Player is already in an active party.");

            var party = new Party(leaderPlayerId);
            db.Parties.Add(party);

            // Leader is always a member
            db.PartyMembers.Add(new PartyMember(party.Id, leaderPlayerId));

            await db.SaveChangesAsync(ct);

            return await GetRosterAsync(party.Id, ct)
                   ?? throw new InvalidOperationException("Failed to load party roster after creation.");
        }

        public async Task<PartyRosterDto?> GetRosterAsync(Guid partyId, CancellationToken ct)
        {
            if (partyId == Guid.Empty)
                throw new ArgumentException("partyId cannot be empty.");

            var party = await db.Parties.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == partyId, ct);

            if (party is null)
                return null;

            var members = await db.PartyMembers.AsNoTracking()
                .Where(x => x.PartyId == partyId)
                .OrderBy(x => x.JoinedAtUtc)
                .Select(x => new PartyMemberDto(x.PlayerId, x.JoinedAtUtc))
                .ToListAsync(ct);

            return new PartyRosterDto(
                PartyId: party.Id,
                LeaderPlayerId: party.LeaderPlayerId,
                Status: party.Status,
                Members: members
            );
        }

        public async Task<PartyInviteDto> InviteAsync(Guid partyId, Guid fromPlayerId, Guid toPlayerId, CancellationToken ct)
        {
            if (partyId == Guid.Empty || fromPlayerId == Guid.Empty || toPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            if (fromPlayerId == toPlayerId)
                throw new InvalidOperationException("Cannot invite yourself.");

            await using var tx = await ((DbContext)db).Database.BeginTransactionAsync(ct);

            var party = await db.Parties.FirstOrDefaultAsync(x => x.Id == partyId, ct);
            if (party is null)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Party not found.");
            }

            if (party.Status != "Open")
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException($"Cannot invite when party status is '{party.Status}'.");
            }

            if (party.LeaderPlayerId != fromPlayerId)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Only the party leader can invite.");
            }

            // Friends-only gate (lightweight MVP rule)
            var areFriends = await db.FriendEdges.AsNoTracking()
                .AnyAsync(x => x.PlayerId == fromPlayerId && x.FriendPlayerId == toPlayerId, ct);

            if (!areFriends)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Can only invite players who are friends.");
            }

            // Party size cap
            var memberCount = await db.PartyMembers.CountAsync(x => x.PartyId == partyId, ct);
            if (memberCount >= MaxPartySize)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Party is already full.");
            }

            // Invitee cannot already be in an active party
            var inActiveParty = await IsInActivePartyAsync(toPlayerId, ct);
            if (inActiveParty)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Invitee is already in an active party.");
            }

            // Invitee cannot already be a member of this party
            var alreadyMember = await db.PartyMembers.AnyAsync(x => x.PartyId == partyId && x.PlayerId == toPlayerId, ct);
            if (alreadyMember)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Invitee is already a party member.");
            }

            // Prevent duplicate pending invites to same user for same party
            var pending = await db.PartyInvites
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x =>
                    x.PartyId == partyId &&
                    x.ToPlayerId == toPlayerId &&
                    x.Status == "Pending", ct);

            if (pending is not null)
            {
                await tx.CommitAsync(ct);
                return ToDto(pending);
            }

            var invite = new PartyInvite(partyId, fromPlayerId, toPlayerId);
            db.PartyInvites.Add(invite);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ToDto(invite);
        }

        public async Task<PartyInviteDto?> AcceptInviteAsync(Guid inviteId, Guid actingPlayerId, CancellationToken ct)
        {
            if (inviteId == Guid.Empty || actingPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            await using var tx = await ((DbContext)db).Database.BeginTransactionAsync(ct);

            var invite = await db.PartyInvites.FirstOrDefaultAsync(x => x.Id == inviteId, ct);
            if (invite is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            if (invite.ToPlayerId != actingPlayerId)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Only the invite recipient can accept.");
            }

            if (invite.Status == "Accepted")
            {
                // Idempotency: ensure membership exists
                await EnsureMemberAsync(invite.PartyId, invite.ToPlayerId, ct);
                await tx.CommitAsync(ct);
                return ToDto(invite);
            }

            if (invite.Status != "Pending")
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException($"Cannot accept invite in status '{invite.Status}'.");
            }

            var party = await db.Parties.FirstOrDefaultAsync(x => x.Id == invite.PartyId, ct);
            if (party is null)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Party not found.");
            }

            if (party.Status != "Open")
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException($"Cannot accept invite when party status is '{party.Status}'.");
            }

            // Invitee cannot already be in an active party
            var inActiveParty = await IsInActivePartyAsync(invite.ToPlayerId, ct);
            if (inActiveParty)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Player is already in an active party.");
            }

            // Party size cap
            var memberCount = await db.PartyMembers.CountAsync(x => x.PartyId == invite.PartyId, ct);
            if (memberCount >= MaxPartySize)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Party is already full.");
            }

            invite.Accept();
            db.PartyMembers.Add(new PartyMember(invite.PartyId, invite.ToPlayerId));

            // Cancel any other pending invites for this player (optional but prevents confusion)
            await db.PartyInvites
                .Where(x => x.ToPlayerId == invite.ToPlayerId && x.Status == "Pending" && x.Id != invite.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Cancelled"), ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyRosterAsync(invite.PartyId, ct);

            return ToDto(invite);
        }

        public async Task<PartyInviteDto?> DeclineInviteAsync(Guid inviteId, Guid actingPlayerId, CancellationToken ct)
        {
            if (inviteId == Guid.Empty || actingPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            var invite = await db.PartyInvites.FirstOrDefaultAsync(x => x.Id == inviteId, ct);
            if (invite is null)
                return null;

            if (invite.ToPlayerId != actingPlayerId)
                throw new InvalidOperationException("Only the invite recipient can decline.");

            if (invite.Status == "Declined")
                return ToDto(invite);

            if (invite.Status != "Pending")
                throw new InvalidOperationException($"Cannot decline invite in status '{invite.Status}'.");

            invite.Decline();
            await db.SaveChangesAsync(ct);

            return ToDto(invite);
        }

        public async Task LeavePartyAsync(Guid partyId, Guid playerId, CancellationToken ct)
        {
            if (partyId == Guid.Empty || playerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            await using var tx = await ((DbContext)db).Database.BeginTransactionAsync(ct);

            var party = await db.Parties.FirstOrDefaultAsync(x => x.Id == partyId, ct);
            if (party is null)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Party not found.");
            }

            // Remove member row (if present)
            var member = await db.PartyMembers.FirstOrDefaultAsync(x => x.PartyId == partyId && x.PlayerId == playerId, ct);
            if (member is not null)
                db.PartyMembers.Remove(member);

            // If leader leaves OR party becomes empty => close party and clear membership
            if (party.LeaderPlayerId == playerId)
            {
                party.Close();

                var remaining = await db.PartyMembers.Where(x => x.PartyId == partyId).ToListAsync(ct);
                if (remaining.Count > 0)
                    db.PartyMembers.RemoveRange(remaining);

                // Cancel any pending invites
                await db.PartyInvites
                    .Where(x => x.PartyId == partyId && x.Status == "Pending")
                    .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Cancelled"), ct);
            }
            else
            {
                // if no members remain, close
                var count = await db.PartyMembers.CountAsync(x => x.PartyId == partyId, ct);
                if (count == 0)
                {
                    party.Close();
                    await db.PartyInvites
                        .Where(x => x.PartyId == partyId && x.Status == "Pending")
                        .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Cancelled"), ct);
                }
            }

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await NotifyRosterAsync(partyId, ct);
        }

        public async Task<PartyInvitesListResponseDto> ListInvitesAsync(Guid playerId, string box, int page, int pageSize, CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId cannot be empty.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

            box = (box ?? "incoming").Trim().ToLowerInvariant();

            var q = db.PartyInvites.AsNoTracking();

            q = box switch
            {
                "incoming" => q.Where(x => x.ToPlayerId == playerId),
                "outgoing" => q.Where(x => x.FromPlayerId == playerId),
                _ => q.Where(x => x.ToPlayerId == playerId || x.FromPlayerId == playerId)
            };

            q = q.OrderBy(x => x.Status == "Pending" ? 0 : 1)
                 .ThenByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new PartyInviteDto(
                    x.Id,
                    x.PartyId,
                    x.FromPlayerId,
                    x.ToPlayerId,
                    x.Status,
                    x.CreatedAtUtc,
                    x.RespondedAtUtc))
                .ToListAsync(ct);

            return new PartyInvitesListResponseDto(page, pageSize, total, items);
        }

        private async Task<bool> IsInActivePartyAsync(Guid playerId, CancellationToken ct)
        {
            // Active party == Open or Queued
            // (Later: add Matched if you want parties to persist through match start)
            var q = from m in db.PartyMembers.AsNoTracking()
                    join p in db.Parties.AsNoTracking() on m.PartyId equals p.Id
                    where m.PlayerId == playerId && (p.Status == "Open" || p.Status == "Queued")
                    select p.Id;

            return await q.AnyAsync(ct);
        }

        private async Task EnsureMemberAsync(Guid partyId, Guid playerId, CancellationToken ct)
        {
            var exists = await db.PartyMembers.AnyAsync(x => x.PartyId == partyId && x.PlayerId == playerId, ct);
            if (!exists)
                db.PartyMembers.Add(new PartyMember(partyId, playerId));
        }

        private static PartyInviteDto ToDto(PartyInvite i)
            => new(
                InviteId: i.Id,
                PartyId: i.PartyId,
                FromPlayerId: i.FromPlayerId,
                ToPlayerId: i.ToPlayerId,
                Status: i.Status,
                CreatedAtUtc: i.CreatedAtUtc,
                RespondedAtUtc: i.RespondedAtUtc
            );

        private async Task NotifyRosterAsync(Guid partyId, CancellationToken ct)
        {
            var roster = await GetRosterAsync(partyId, ct);
            if (roster is null) return;

            var memberIds = roster.Members.Select(m => m.PlayerId).ToList();
            var onlineIds = await presence.GetOnlineAsync(memberIds, ct);

            await notifier.NotifyRosterUpdatedAsync(
                roster: roster,
                memberPlayerIds: memberIds,
                onlinePlayerIds: onlineIds,
                ct: ct);
        }

    }
}
