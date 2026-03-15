using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Guardians
{
    public sealed record GetGuardiansForTier(Guid SeasonId, int TierNumber) : IRequest<List<TierGuardianDto>>;

    public sealed class GetGuardiansForTierHandler(IAppDb db) : IRequestHandler<GetGuardiansForTier, List<TierGuardianDto>>
    {
        public async Task<List<TierGuardianDto>> Handle(GetGuardiansForTier r, CancellationToken ct)
        {
            var now = DateTimeOffset.UtcNow;

            return await db.TierGuardians.AsNoTracking()
                .Where(x => x.SeasonId == r.SeasonId && x.TierNumber == r.TierNumber && x.ExpiresAtUtc > now)
                .Select(x => new TierGuardianDto(x.Id, x.SeasonId, x.TierNumber, x.PlayerId, x.AssignedAtUtc, x.ExpiresAtUtc, x.DefencesWon, x.DefencesLost))
                .ToListAsync(ct);
        }
    }
}
