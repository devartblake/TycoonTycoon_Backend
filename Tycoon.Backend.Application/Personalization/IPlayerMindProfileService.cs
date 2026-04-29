using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public interface IPlayerMindProfileService
{
    Task<PlayerMindProfileDto> GetOrCreateAsync(Guid playerId, CancellationToken ct = default);

    Task RecordEventAsync(Guid playerId, PlayerBehaviorEventDto behaviorEvent, CancellationToken ct = default);

    Task<PlayerMindProfileDto> RecalculateAsync(Guid playerId, CancellationToken ct = default);
}
