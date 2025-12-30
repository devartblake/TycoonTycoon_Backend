using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Missions
{
    public record ListMissions(string Type) : IRequest<IReadOnlyList<MissionDto>>;

    public sealed class ListMissionsHandler(IAppDb db)
        : IRequestHandler<ListMissions, IReadOnlyList<MissionDto>>
    {
        public async Task<IReadOnlyList<MissionDto>> Handle(ListMissions r, CancellationToken ct)
        {
            var type = r.Type?.Trim() ?? "";

            var q = db.Missions.AsNoTracking()
                .Where(m => m.Active && (type == "" || m.Type == type));

            var list = await q.OrderBy(m => m.Type).ThenBy(m => m.Key).ToListAsync(ct);

            return list
                .Select(m => new MissionDto(m.Id, m.Type, m.Key, m.Goal, m.RewardXp))
                .ToList();
        }
    }
}
