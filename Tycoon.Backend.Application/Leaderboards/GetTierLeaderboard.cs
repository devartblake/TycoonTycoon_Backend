using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Leaderboards
{
    public sealed record GetTierLeaderboard(
        int TierId,
        int Page = 1,
        int PageSize = 50
    ) : IRequest<TierLeaderboardDto>;

    public sealed class GetTierLeaderboardHandler(IAppDb db)
        : IRequestHandler<GetTierLeaderboard, TierLeaderboardDto>
    {
        public async Task<TierLeaderboardDto> Handle(GetTierLeaderboard r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 100);

            var q =
                from e in db.LeaderboardEntries.AsNoTracking()
                join p in db.Players.AsNoTracking() on e.PlayerId equals p.Id
                where e.TierId == r.TierId
                orderby e.TierRank
                select new TierLeaderboardEntryDto(
                    p.Id,
                    p.Username,
                    p.CountryCode,
                    p.Level,
                    e.Score,
                    e.GlobalRank,
                    e.TierRank,
                    e.XpProgress
                );

            var total = await q.CountAsync(ct);

            var entries = await q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            return new TierLeaderboardDto(r.TierId, page, pageSize, total, entries);
        }
    }
}
