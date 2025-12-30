using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Referrals
{
    public sealed record GetReferralCode(string Code) : IRequest<ReferralCodeDto?>;

    public sealed class GetReferralCodeHandler(IAppDb db)
        : IRequestHandler<GetReferralCode, ReferralCodeDto?>
    {
        public async Task<ReferralCodeDto?> Handle(GetReferralCode r, CancellationToken ct)
        {
            var rc = await db.ReferralCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == r.Code, ct);
            if (rc is null) return null;

            var count = await db.ReferralRedemptions.CountAsync(x => x.ReferralCodeId == rc.Id, ct);
            return new ReferralCodeDto(rc.Id, rc.Code, rc.OwnerPlayerId, rc.CreatedAtUtc, count);
        }
    }
}
