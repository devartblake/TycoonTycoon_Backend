using MediatR;
using Microsoft.EntityFrameworkCore;
using Synaptix.Backend.Application.Abstractions;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.EventStats
{
    public sealed record GetGameEventLeaderboard(Guid GameEventId, int Page = 1, int PageSize = 50)
        : IRequest<List<EventLeaderboardEntryDto>>;

    public sealed class GetGameEventLeaderboardHandler(IAppDb db)
        : IRequestHandler<GetGameEventLeaderboard, List<EventLeaderboardEntryDto>>
    {
        public async Task<List<EventLeaderboardEntryDto>> Handle(GetGameEventLeaderboard r, CancellationToken ct)
        {
            var page = Math.Max(1, r.Page);
            var pageSize = Math.Clamp(r.PageSize, 1, 200);

            // Participants with optional prize claim
            var participants = await db.GameEventParticipants
                .AsNoTracking()
                .Where(p => p.GameEventId == r.GameEventId && p.FinalRank.HasValue)
                .OrderBy(p => p.FinalRank)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var playerIds = participants.Select(p => p.PlayerId).ToList();

            var claims = await db.GameEventPrizeClaims
                .AsNoTracking()
                .Where(c => c.GameEventId == r.GameEventId && playerIds.Contains(c.PlayerId))
                .ToDictionaryAsync(c => c.PlayerId, ct);

            return participants.Select(p =>
            {
                claims.TryGetValue(p.PlayerId, out var claim);
                return new EventLeaderboardEntryDto(
                    p.PlayerId,
                    p.FinalRank!.Value,
                    claim?.AwardedXp ?? 0,
                    claim?.AwardedCoins ?? 0,
                    p.EliminatedAt);
            }).ToList();
        }
    }
}
