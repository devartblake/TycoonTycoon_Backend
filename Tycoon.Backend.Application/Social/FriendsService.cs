using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Social
{
    public sealed class FriendsService(IAppDb db)
    {
        public async Task<FriendRequestDto> SendRequestAsync(Guid fromPlayerId, Guid toPlayerId, CancellationToken ct)
        {
            if (fromPlayerId == Guid.Empty || toPlayerId == Guid.Empty)
                throw new ArgumentException("PlayerId cannot be empty.");

            if (fromPlayerId == toPlayerId)
                throw new InvalidOperationException("Cannot friend yourself.");

            // Already friends?
            var alreadyFriends = await db.FriendEdges.AsNoTracking()
                .AnyAsync(x => x.PlayerId == fromPlayerId && x.FriendPlayerId == toPlayerId, ct);

            if (alreadyFriends)
            {
                // Return a synthetic “accepted” DTO to keep clients simple
                return new FriendRequestDto(
                    RequestId: Guid.Empty,
                    FromPlayerId: fromPlayerId,
                    ToPlayerId: toPlayerId,
                    Status: "Accepted",
                    CreatedAtUtc: DateTimeOffset.UtcNow,
                    RespondedAtUtc: DateTimeOffset.UtcNow
                );
            }

            // Existing pending request either direction?
            var existing = await db.FriendRequests
                .OrderByDescending(x => x.CreatedAtUtc)
                .FirstOrDefaultAsync(x =>
                    ((x.FromPlayerId == fromPlayerId && x.ToPlayerId == toPlayerId) ||
                     (x.FromPlayerId == toPlayerId && x.ToPlayerId == fromPlayerId)) &&
                    x.Status == "Pending", ct);

            if (existing is not null)
            {
                return ToDto(existing);
            }

            var req = new FriendRequest(fromPlayerId, toPlayerId);
            db.FriendRequests.Add(req);
            await db.SaveChangesAsync(ct);

            return ToDto(req);
        }

        public async Task<FriendRequestDto?> AcceptRequestAsync(Guid requestId, Guid actingPlayerId, CancellationToken ct)
        {
            if (requestId == Guid.Empty || actingPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            await using var tx = await ((DbContext)db).Database.BeginTransactionAsync(ct);

            var req = await db.FriendRequests
                .FirstOrDefaultAsync(x => x.Id == requestId, ct);

            if (req is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            // Only the recipient can accept
            if (req.ToPlayerId != actingPlayerId)
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException("Only the recipient can accept this friend request.");
            }

            // Idempotency: if already accepted, ensure edges exist.
            if (req.Status == "Accepted")
            {
                await EnsureEdgesAsync(req.FromPlayerId, req.ToPlayerId, ct);
                await tx.CommitAsync(ct);
                return ToDto(req);
            }

            if (req.Status != "Pending")
            {
                await tx.RollbackAsync(ct);
                throw new InvalidOperationException($"Cannot accept a request in status '{req.Status}'.");
            }

            req.Accept();

            await EnsureEdgesAsync(req.FromPlayerId, req.ToPlayerId, ct);

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            return ToDto(req);
        }

        public async Task<FriendRequestDto?> DeclineRequestAsync(Guid requestId, Guid actingPlayerId, CancellationToken ct)
        {
            if (requestId == Guid.Empty || actingPlayerId == Guid.Empty)
                throw new ArgumentException("Ids cannot be empty.");

            var req = await db.FriendRequests
                .FirstOrDefaultAsync(x => x.Id == requestId, ct);

            if (req is null)
                return null;

            // Only recipient can decline (consistent with accept)
            if (req.ToPlayerId != actingPlayerId)
                throw new InvalidOperationException("Only the recipient can decline this friend request.");

            if (req.Status == "Declined")
                return ToDto(req);

            if (req.Status != "Pending")
                throw new InvalidOperationException($"Cannot decline a request in status '{req.Status}'.");

            reqDecline(req);
            await db.SaveChangesAsync(ct);

            return ToDto(req);

            static void reqDecline(FriendRequest r) => r.Decline();
        }

        public async Task<FriendRequestsListResponseDto> ListRequestsAsync(
            Guid playerId,
            string box, // "incoming" | "outgoing" | "all"
            int page,
            int pageSize,
            CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId cannot be empty.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

            var q = db.FriendRequests.AsNoTracking();

            box = (box ?? "all").Trim().ToLowerInvariant();
            q = box switch
            {
                "incoming" => q.Where(x => x.ToPlayerId == playerId),
                "outgoing" => q.Where(x => x.FromPlayerId == playerId),
                _ => q.Where(x => x.ToPlayerId == playerId || x.FromPlayerId == playerId)
            };

            // Default: show pending first, then newest
            q = q.OrderBy(x => x.Status == "Pending" ? 0 : 1)
                 .ThenByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FriendRequestDto(
                    x.Id,
                    x.FromPlayerId,
                    x.ToPlayerId,
                    x.Status,
                    x.CreatedAtUtc,
                    x.RespondedAtUtc))
                .ToListAsync(ct);

            return new FriendRequestsListResponseDto(page, pageSize, total, items);
        }

        public async Task<FriendsListResponseDto> ListFriendsAsync(Guid playerId, int page, int pageSize, CancellationToken ct)
        {
            if (playerId == Guid.Empty)
                throw new ArgumentException("playerId cannot be empty.");

            page = page <= 0 ? 1 : page;
            pageSize = pageSize is <= 0 or > 200 ? 50 : pageSize;

            var q = db.FriendEdges.AsNoTracking()
                .Where(x => x.PlayerId == playerId)
                .OrderByDescending(x => x.CreatedAtUtc);

            var total = await q.CountAsync(ct);

            var items = await q.Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(x => new FriendDto(x.FriendPlayerId, x.CreatedAtUtc))
                .ToListAsync(ct);

            return new FriendsListResponseDto(page, pageSize, total, items);
        }

        private async Task EnsureEdgesAsync(Guid a, Guid b, CancellationToken ct)
        {
            // Create A->B if missing
            var ab = await db.FriendEdges
                .AnyAsync(x => x.PlayerId == a && x.FriendPlayerId == b, ct);

            if (!ab)
                db.FriendEdges.Add(new FriendEdge(a, b));

            // Create B->A if missing
            var ba = await db.FriendEdges
                .AnyAsync(x => x.PlayerId == b && x.FriendPlayerId == a, ct);

            if (!ba)
                db.FriendEdges.Add(new FriendEdge(b, a));
        }

        private static FriendRequestDto ToDto(FriendRequest r)
            => new(
                RequestId: r.Id,
                FromPlayerId: r.FromPlayerId,
                ToPlayerId: r.ToPlayerId,
                Status: r.Status,
                CreatedAtUtc: r.CreatedAtUtc,
                RespondedAtUtc: r.RespondedAtUtc);
    }
}
