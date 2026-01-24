using MediatR;
using Microsoft.EntityFrameworkCore;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Backend.Domain.Entities; // Assuming MissionClaim is here

namespace Tycoon.Backend.Application.Missions
{
    public sealed class ClaimMissionHandler : IRequestHandler<ClaimMission, ClaimMissionResult>
    {
        private readonly IAppDb _db;

        public ClaimMissionHandler(IAppDb db)
        {
            _db = db;
        }

        public async Task<ClaimMissionResult> Handle(ClaimMission request, CancellationToken ct)
        {
            // Load claim (progress record)
            var claim = await _db.MissionClaims
                .SingleOrDefaultAsync(x => x.PlayerId == request.PlayerId && x.MissionId == request.MissionId, ct);

            // Fix CS8602: Handle null claim safely
            if (claim is null)
            {
                return CreateEmptyResult(request, ClaimMissionStatus.NotFound);
            }

            // Load mission definition
            var mission = await _db.Missions.FindAsync(new object[] { request.MissionId }, ct);

            // Fix CS8602: Handle null mission safely
            if (mission is null)
            {
                return CreateEmptyResult(request, ClaimMissionStatus.NotFound);
            }

            if (!claim.Completed || claim.Claimed)
            {
                return new ClaimMissionResult(
                    Status: !claim.Completed ? ClaimMissionStatus.NotCompleted : ClaimMissionStatus.AlreadyClaimed,
                    PlayerId: request.PlayerId,
                    MissionId: request.MissionId,
                    MissionType: mission.Type,
                    MissionKey: mission.Key,
                    RewardXp: mission.RewardXp,
                    RewardCoins: mission.RewardCoins,
                    RewardDiamonds: mission.RewardDiamonds,
                    ClaimedAtUtcUtc: DateTime.UtcNow,
                    Completed: claim.Completed,
                    Claimed: claim.Claimed,
                    Progress: claim.Progress,
                    Goal: mission.Goal,
                    UpdatedMissions: await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct)
                );
            }

            // Fix CS0200: Property 'Claimed' is read-only. 
            // Most domain-driven entities use a method to update state.
            // If 'MarkAsClaimed()' is not the correct name, check MissionClaim.cs for the setter method.
            claim.MarkClaimed();

            await _db.SaveChangesAsync(ct);

            var updatedMissions = await LoadMissionListAsync(request.PlayerId, request.TypeFilter, ct);

            return new ClaimMissionResult(
                Status: ClaimMissionStatus.Claimed,
                PlayerId: request.PlayerId,
                MissionId: request.MissionId,
                MissionType: mission.Type,
                MissionKey: mission.Key,
                RewardXp: mission.RewardXp,
                RewardCoins: mission.RewardCoins,
                RewardDiamonds: mission.RewardDiamonds,
                ClaimedAtUtcUtc: DateTime.UtcNow,
                Completed: claim.Completed,
                Claimed: claim.Claimed,
                Progress: claim.Progress,
                Goal: mission.Goal,
                UpdatedMissions: updatedMissions
            );
        }

        private ClaimMissionResult CreateEmptyResult(ClaimMission request, ClaimMissionStatus status)
        {
            return new ClaimMissionResult(
                status, request.PlayerId, request.MissionId,
                "", "", 0, 0, 0, DateTime.UtcNow, false, false, 0, 0, Array.Empty<MissionListItem>()
            );
        }

        private async Task<IReadOnlyList<MissionListItem>> LoadMissionListAsync(Guid playerId, string typeFilter, CancellationToken ct)
        {
            var missionsQuery = _db.Missions.AsNoTracking().Where(m => m.Active);
            if (!string.IsNullOrWhiteSpace(typeFilter))
                missionsQuery = missionsQuery.Where(m => m.Type == typeFilter);

            var missions = await missionsQuery.ToListAsync(ct);
            var claims = await _db.MissionClaims.AsNoTracking()
                .Where(c => c.PlayerId == playerId)
                .ToListAsync(ct);

            var byMissionId = claims.ToDictionary(x => x.MissionId, x => x);

            return missions.Select(m =>
            {
                byMissionId.TryGetValue(m.Id, out var c);
                return new MissionListItem(
                    MissionId: m.Id,
                    Type: m.Type,
                    Key: m.Key,
                    Goal: m.Goal,
                    Active: m.Active,
                    Progress: c?.Progress ?? 0,
                    Completed: c?.Completed ?? false,
                    Claimed: c?.Claimed ?? false // Safe navigation used here
                );
            }).ToList();
        }
    }
}