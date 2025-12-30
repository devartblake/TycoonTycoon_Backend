using MediatR;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Leaderboards
{
    public sealed record RecalculateLeaderboard() : IRequest<LeaderboardRecalcResultDto>;

    public sealed class RecalculateLeaderboardHandler(LeaderboardRecalculator recalculator)
        : IRequestHandler<RecalculateLeaderboard, LeaderboardRecalcResultDto>
    {
        public async Task<LeaderboardRecalcResultDto> Handle(RecalculateLeaderboard r, CancellationToken ct)
            => await recalculator.RecalculateAsync(ct);
    }
}
