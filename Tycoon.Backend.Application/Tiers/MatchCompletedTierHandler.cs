using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Domain.Events;
using Tycoon.Backend.Application.Abstractions;

namespace Tycoon.Backend.Application.Tiers
{
    /// <summary>
    /// Synchronous tier progression for MatchCompletedEvent.
    /// </summary>
    public sealed class MatchCompletedTierHandler : INotificationHandler<MatchCompletedEvent>
    {
        private readonly IAppDb _db;
        private readonly TierResolver _tierResolver;

        public MatchCompletedTierHandler(IAppDb db, TierResolver tierResolver)
        {
            _db = db;
            _tierResolver = tierResolver;
        }

        public async Task Handle(MatchCompletedEvent notification, CancellationToken ct)
        {
            // Load player in the same transaction boundary as other sync updates.
            var player = await _db.Players.SingleOrDefaultAsync(p => p.Id == notification.PlayerId, ct);

            if (player is null)
                return;

            // Apply score + xp from match payload
            player.ApplyMatchResult(notification.ScoreDelta, notification.XpEarned);

            // Resolve tier based on updated score
            var tier = await _tierResolver.ResolveForScoreAsync(player.Score, ct);
            if (tier is not null)
            {
                player.SetTier(tier.Id);
            }

            await _db.SaveChangesAsync(ct);
        }
    }
}
