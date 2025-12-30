using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Referrals
{
    public sealed record RedeemReferralCode(string Code, RedeemReferralRequest Req) : IRequest<RedeemReferralResultDto>;

    public sealed class RedeemReferralCodeHandler(IAppDb db)
        : IRequestHandler<RedeemReferralCode, RedeemReferralResultDto>
    {
        // Tune these later via config
        private const int OwnerXp = 10;
        private const int OwnerCoins = 25;
        private const int RedeemerXp = 20;
        private const int RedeemerCoins = 50;

        public async Task<RedeemReferralResultDto> Handle(RedeemReferralCode r, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            // Idempotency: same EventId returns Duplicate
            var dup = await db.ReferralRedemptions.AsNoTracking().AnyAsync(x => x.EventId == r.Req.EventId, ct);
            if (dup)
            {
                // If duplicate, return stable response shape
                return new RedeemReferralResultDto(r.Code, Guid.Empty, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "Duplicate", now);
            }

            var code = await db.ReferralCodes.FirstOrDefaultAsync(x => x.Code == r.Code, ct);
            if (code is null)
            {
                return new RedeemReferralResultDto(r.Code, Guid.Empty, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "Invalid", now);
            }

            if (code.OwnerPlayerId == r.Req.RedeemerPlayerId)
            {
                return new RedeemReferralResultDto(r.Code, code.OwnerPlayerId, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "SelfRedeemNotAllowed", now);
            }

            // Optional: prevent same redeemer from redeeming the same code multiple times
            var already = await db.ReferralRedemptions.AsNoTracking()
                .AnyAsync(x => x.ReferralCodeId == code.Id && x.RedeemerPlayerId == r.Req.RedeemerPlayerId, ct);
            if (already)
            {
                return new RedeemReferralResultDto(r.Code, code.OwnerPlayerId, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "Duplicate", now);
            }

            // Load players (authoritative awarding)
            var owner = await db.Players.FirstOrDefaultAsync(x => x.Id == code.OwnerPlayerId, ct);
            var redeemer = await db.Players.FirstOrDefaultAsync(x => x.Id == r.Req.RedeemerPlayerId, ct);

            if (owner is null || redeemer is null)
            {
                return new RedeemReferralResultDto(r.Code, code.OwnerPlayerId, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "Invalid", now);
            }

            // Apply awards
            owner.AddCoins(OwnerCoins);
            owner.AddXp(OwnerXp);

            redeemer.AddCoins(RedeemerCoins);
            redeemer.AddXp(RedeemerXp);

            db.ReferralRedemptions.Add(new ReferralRedemption(
                r.Req.EventId,
                code.Id,
                owner.Id,
                redeemer.Id,
                OwnerXp,
                OwnerCoins,
                RedeemerXp,
                RedeemerCoins));

            try
            {
                await db.SaveChangesAsync(ct);
            }
            catch (DbUpdateException)
            {
                // EventId unique index can race
                return new RedeemReferralResultDto(r.Code, code.OwnerPlayerId, r.Req.RedeemerPlayerId,
                    0, 0, 0, 0, "Duplicate", now);
            }

            return new RedeemReferralResultDto(
                r.Code,
                owner.Id,
                redeemer.Id,
                OwnerXp,
                OwnerCoins,
                RedeemerXp,
                RedeemerCoins,
                "Redeemed",
                now);
        }
    }
}
