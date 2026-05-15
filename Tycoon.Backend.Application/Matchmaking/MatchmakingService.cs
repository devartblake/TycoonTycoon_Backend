using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Application.Enforcement;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Matchmaking
{
    public sealed record QueueResultDto(string Status, Guid? TicketId, Guid? MatchId, Guid? OpponentId);

    public sealed class MatchmakingService(
        IAppDb db,
        EnforcementService enforcement,
        IMatchmakingNotifier notifier)
    {
        private static readonly TimeSpan TicketTtl = TimeSpan.FromMinutes(2);

        public async Task<QueueResultDto> EnqueueAsync(Guid playerId, string mode, int tier, CancellationToken ct)
        {
            // Enforcement (restricted => TierOnly, banned => Forbidden)
            var decision = await enforcement.EvaluateAsync(playerId, ct);
            if (!decision.CanStartMatch)
                return new QueueResultDto("Forbidden", null, null, null);

            mode = string.IsNullOrWhiteSpace(mode) ? "duel" : mode.Trim();
            var scope = decision.QueueScope;

            // Cleanup expired tickets
            await CancelExpiredTicketsAsync(ct);

            // Idempotent enqueue
            var existing = await db.MatchmakingTickets
                .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.Status == "Queued", ct);

            if (existing is not null)
                return new QueueResultDto("Queued", existing.Id, null, null);

            // Create ticket
            var ticket = new MatchmakingTicket(playerId, mode, tier, scope, TicketTtl);
            db.MatchmakingTickets.Add(ticket);
            await db.SaveChangesAsync(ct);

            // Try to match (retry once on concurrency)
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var matched = await TryMatchAsync(ticket.Id, mode, tier, scope, ct);
                if (matched is not null)
                    return matched;
            }

            return new QueueResultDto("Queued", ticket.Id, null, null);
        }

        public async Task CancelAsync(Guid playerId, CancellationToken ct)
        {
            var existing = await db.MatchmakingTickets
                .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.Status == "Queued", ct);

            if (existing is null) return;

            existing.Cancel();
            await db.SaveChangesAsync(ct);
        }

        public async Task<QueueResultDto> GetStatusAsync(Guid playerId, CancellationToken ct)
        {
            var ticket = await db.MatchmakingTickets.AsNoTracking()
                .OrderByDescending(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(t => t.PlayerId == playerId && t.Status != "Cancelled", ct);

            if (ticket is null)
                return new QueueResultDto("None", null, null, null);

            return new QueueResultDto(ticket.Status, ticket.Id, null, null);
        }

        private async Task<QueueResultDto?> TryMatchAsync(
            Guid ticketId,
            string mode,
            int tier,
            string scope,
            CancellationToken ct)
        {
            var context = (DbContext)db;
            if (IsInMemoryProvider(context))
                return await TryMatchCoreAsync(ticketId, mode, tier, scope, ct);

            await using var tx = await context.Database.BeginTransactionAsync(ct);

            var self = await db.MatchmakingTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

            if (self is null || self.Status != "Queued")
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            var opponent = await db.MatchmakingTickets
                .OrderBy(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(t =>
                    t.Status == "Queued" &&
                    t.Id != ticketId &&
                    t.Mode == mode &&
                    t.Scope == scope &&
                    (scope == "TierOnly" ? t.Tier == tier : true), ct);

            if (opponent is null)
            {
                await tx.RollbackAsync(ct);
                return null;
            }

            self.MarkMatched();
            opponent.MarkMatched();

            try
            {
                await db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);

                // 🔔 Real-time notifications
                await notifier.NotifyMatchedAsync(
                    self.PlayerId,
                    opponent.PlayerId,
                    mode,
                    tier,
                    scope,
                    self.Id,
                    ct);

                await notifier.NotifyMatchedAsync(
                    opponent.PlayerId,
                    self.PlayerId,
                    mode,
                    tier,
                    scope,
                    opponent.Id,
                    ct);

                return new QueueResultDto("Matched", self.Id, null, opponent.PlayerId);
            }
            catch (DbUpdateConcurrencyException)
            {
                await tx.RollbackAsync(ct);
                return null;
            }
        }

        private async Task<QueueResultDto?> TryMatchCoreAsync(
            Guid ticketId,
            string mode,
            int tier,
            string scope,
            CancellationToken ct)
        {
            var self = await db.MatchmakingTickets
                .FirstOrDefaultAsync(t => t.Id == ticketId, ct);

            if (self is null || self.Status != "Queued")
                return null;

            var opponent = await db.MatchmakingTickets
                .OrderBy(t => t.CreatedAtUtc)
                .FirstOrDefaultAsync(t =>
                    t.Status == "Queued" &&
                    t.Id != ticketId &&
                    t.Mode == mode &&
                    t.Scope == scope &&
                    (scope == "TierOnly" ? t.Tier == tier : true), ct);

            if (opponent is null)
                return null;

            self.MarkMatched();
            opponent.MarkMatched();

            try
            {
                await db.SaveChangesAsync(ct);

                await notifier.NotifyMatchedAsync(
                    self.PlayerId,
                    opponent.PlayerId,
                    mode,
                    tier,
                    scope,
                    self.Id,
                    ct);

                await notifier.NotifyMatchedAsync(
                    opponent.PlayerId,
                    self.PlayerId,
                    mode,
                    tier,
                    scope,
                    opponent.Id,
                    ct);

                return new QueueResultDto("Matched", self.Id, null, opponent.PlayerId);
            }
            catch (DbUpdateConcurrencyException)
            {
                return null;
            }
        }

        private async Task CancelExpiredTicketsAsync(CancellationToken ct)
        {
            var expiredQuery = db.MatchmakingTickets
                .Where(t => t.Status == "Queued" && t.ExpiresAtUtc <= DateTimeOffset.UtcNow);

            var expired = await expiredQuery.ToListAsync(ct);
            foreach (var ticket in expired)
                ticket.Cancel();

            if (expired.Count > 0)
                await db.SaveChangesAsync(ct);
        }

        private static bool IsInMemoryProvider(DbContext context) =>
            string.Equals(context.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal);
    }
}
