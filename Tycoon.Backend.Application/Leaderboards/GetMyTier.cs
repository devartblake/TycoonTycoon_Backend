using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Leaderboards
{
    public record GetMyTier(Guid PlayerId) : IRequest<MyTierDto?>;

    public sealed class GetMyTierHandler(IAppDb db)
        : IRequestHandler<GetMyTier, MyTierDto?>
    {
        public async Task<MyTierDto?> Handle(GetMyTier r, CancellationToken ct)
        {
            var e = await db.LeaderboardEntries.AsNoTracking()
                .FirstOrDefaultAsync(x => x.PlayerId == r.PlayerId, ct);

            return e is null
                ? null
                : new MyTierDto(e.PlayerId, e.TierId, e.TierRank, e.GlobalRank, e.Score, e.XpProgress);
        }
    }
}
