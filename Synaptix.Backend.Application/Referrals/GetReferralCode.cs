using Mediator;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Referrals
{
    public sealed record GetReferralCode(string Code) : IRequest<ReferralCodeDto?>;

    public sealed class GetReferralCodeHandler(IAppDb db)
        : IRequestHandler<GetReferralCode, ReferralCodeDto?>
    {
        public async ValueTask<ReferralCodeDto?> Handle(GetReferralCode r, CancellationToken ct)
        {
            var rc = await db.ReferralCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == r.Code, ct);
            if (rc is null) return null;

            var count = await db.ReferralRedemptions.CountAsync(x => x.ReferralCodeId == rc.Id, ct);
            return new ReferralCodeDto(rc.Id, rc.Code, rc.OwnerPlayerId, rc.CreatedAtUtc, count);
        }
    }
}
