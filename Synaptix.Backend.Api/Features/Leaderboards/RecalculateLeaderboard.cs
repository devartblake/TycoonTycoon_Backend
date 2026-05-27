using Mediator;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Leaderboards
{
    public sealed record RecalculateLeaderboard() : IRequest<LeaderboardRecalcResultDto>;

    public sealed class RecalculateLeaderboardHandler(LeaderboardRecalculator recalculator)
        : IRequestHandler<RecalculateLeaderboard, LeaderboardRecalcResultDto>
    {
        public async ValueTask<LeaderboardRecalcResultDto> Handle(RecalculateLeaderboard r, CancellationToken ct)
            => await recalculator.RecalculateAsync(ct);
    }
}
