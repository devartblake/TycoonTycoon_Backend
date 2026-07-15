using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Backend.Domain.Entities;

namespace Synaptix.Backend.Application.Moderation
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
        /// Submits an appeal for the player. Returns null when the player already
        /// has a pending appeal (callers translate that to a conflict).
        /// </summary>
        public async Task<ModerationAppeal?> SubmitAppealAsync(Guid playerId, string reason, CancellationToken ct)
        {
            var hasPending = await db.ModerationAppeals
                .AsNoTracking()
                .AnyAsync(x => x.PlayerId == playerId && x.Status == ModerationAppealStatus.Pending, ct);
            if (hasPending) return null;

            var appeal = new ModerationAppeal(playerId, reason);
            db.ModerationAppeals.Add(appeal);
            await db.SaveChangesAsync(ct);
            return appeal;
        }

        /// <summary>
        /// Reviews a pending appeal. Returns null when the appeal does not exist.
        /// Throws InvalidOperationException when it was already reviewed.
        /// On approval the sanction is lifted through the normal moderation pipeline
        /// (profile update + ModerationActionLog entry).
        /// </summary>
        public async Task<ModerationAppeal?> ReviewAppealAsync(
            Guid appealId,
            bool approve,
            string? reviewerNotes,
            string? reviewedBy,
            CancellationToken ct)
        {
            var appeal = await db.ModerationAppeals.FirstOrDefaultAsync(x => x.Id == appealId, ct);
            if (appeal is null) return null;

            appeal.Review(
                approve ? ModerationAppealStatus.Approved : ModerationAppealStatus.Rejected,
                reviewerNotes,
                reviewedBy);
            await db.SaveChangesAsync(ct);

            if (approve)
            {
                await SetStatusAsync(
                    appeal.PlayerId,
                    ModerationStatus.Normal,
                    reason: "appeal approved",
                    notes: reviewerNotes,
                    setByAdmin: reviewedBy,
                    expiresAtUtc: null,
                    relatedFlagId: null,
                    ct);
            }

            return appeal;
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
