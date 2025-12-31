using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;

namespace Tycoon.Backend.Application.Moderation
{
    public sealed class ModerationService(IAppDb db)
    {
        public async Task<PlayerModerationProfile> GetOrCreateAsync(Guid playerId, CancellationToken ct)
        {
            var p = await db.PlayerModerationProfiles.FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
            if (p is not null) return p;

            var created = new PlayerModerationProfile(playerId);
            db.PlayerModerationProfiles.Add(created);
            await db.SaveChangesAsync(ct);
            return created;
        }

        public async Task<PlayerModerationProfile> SetStatusAsync(
            Guid playerId,
            ModerationStatus status,
            string? reason,
            string? notes,
            string? setByAdmin,
            DateTimeOffset? expiresAtUtc,
            Guid? relatedFlagId,
            CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            var profile = await GetOrCreateAsync(playerId, ct);
            profile.SetStatus(status, reason, notes, setByAdmin, expiresAtUtc);

            db.ModerationActionLogs.Add(new ModerationActionLog(
                playerId,
                status,
                reason,
                notes,
                setByAdmin,
                expiresAtUtc,
                relatedFlagId));

            await db.SaveChangesAsync(ct);
            return profile;
        }

        /// <summary>
        /// Returns the effective status, considering expiry.
        /// Expired Restricted/Banned automatically behave as Normal (but we do not mutate DB here).
        /// </summary>
        public async Task<ModerationStatus> GetEffectiveStatusAsync(Guid playerId, CancellationToken ct)
        {
            var p = await db.PlayerModerationProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.PlayerId == playerId, ct);
            if (p is null) return ModerationStatus.Normal;

            if ((p.Status == ModerationStatus.Restricted || p.Status == ModerationStatus.Banned) &&
                p.ExpiresAtUtc.HasValue &&
                p.ExpiresAtUtc.Value <= DateTimeOffset.UtcNow)
            {
                return ModerationStatus.Normal;
            }

            return p.Status;
        }
    }
}
