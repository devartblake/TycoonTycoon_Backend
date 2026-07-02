using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Leaderboards
{
    public sealed record SubmitArcadeScore(
        Guid PlayerId,
        string GameId,
        string Difficulty,
        int Score,
        int DurationMs
    ) : IRequest<bool>;

    public sealed class SubmitArcadeScoreHandler(IAppDb db)
        : IRequestHandler<SubmitArcadeScore, bool>
    {
        public async ValueTask<bool> Handle(SubmitArcadeScore r, CancellationToken ct)
        {
            // Check if player exists
            var playerExists = await db.Players
                .AsNoTracking()
                .AnyAsync(p => p.Id == r.PlayerId, ct);

            if (!playerExists)
                return false;

            // Try to get existing entry
            var existing = await db.ArcadeScores
                .FirstOrDefaultAsync(
                    e => e.PlayerId == r.PlayerId &&
                         e.GameId == r.GameId &&
                         e.Difficulty == r.Difficulty,
                    ct);

            var now = DateTimeOffset.UtcNow;

            if (existing is null)
            {
                // Create new entry
                var entry = new ArcadeScoreEntry(
                    r.PlayerId,
                    r.GameId,
                    r.Difficulty,
                    r.Score,
                    r.DurationMs,
                    now);
                db.ArcadeScores.Add(entry);
            }
            else if (existing.IsNewBest(r.Score, r.DurationMs))
            {
                // Update existing entry if it's a new personal best
                existing.UpdateScore(r.Score, r.DurationMs, now);
                db.ArcadeScores.Update(existing);
            }
            else
            {
                // Not a new personal best, skip update
                return false;
            }

            await db.SaveChangesAsync(ct);
            return true;
        }
    }
}
