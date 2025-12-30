using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Referrals
{
    public sealed record CreateReferralCode(CreateReferralCodeRequest Req) : IRequest<ReferralCodeDto>;

    public sealed class CreateReferralCodeHandler(IAppDb db)
        : IRequestHandler<CreateReferralCode, ReferralCodeDto>
    {
        public async Task<ReferralCodeDto> Handle(CreateReferralCode r, CancellationToken ct)
        {
            // One active code per owner (simple rule; can be changed later)
            var existing = await db.ReferralCodes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.OwnerPlayerId == r.Req.OwnerPlayerId, ct);

            if (existing is not null)
            {
                var existingCount = await db.ReferralRedemptions.CountAsync(x => x.ReferralCodeId == existing.Id, ct);
                return new ReferralCodeDto(existing.Id, existing.Code, existing.OwnerPlayerId, existing.CreatedAtUtc, existingCount);
            }

            for (int attempt = 0; attempt < 10; attempt++)
            {
                var code = ReferralCodeGenerator.Generate(8);

                var taken = await db.ReferralCodes.AsNoTracking().AnyAsync(x => x.Code == code, ct);
                if (taken) continue;

                var entity = new ReferralCode(r.Req.OwnerPlayerId, code);
                db.ReferralCodes.Add(entity);
                await db.SaveChangesAsync(ct);

                return new ReferralCodeDto(entity.Id, entity.Code, entity.OwnerPlayerId, entity.CreatedAtUtc, 0);
            }

            throw new InvalidOperationException("Unable to generate a unique referral code after multiple attempts.");
        }
    }
}
