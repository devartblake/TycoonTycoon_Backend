using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Personalization;

public interface IPersonalizationService
{
    Task<PlayerHomePersonalizationDto> GetHomeAsync(Guid playerId, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(Guid playerId, CancellationToken ct = default);

    Task AcceptRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);

    Task DismissRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);
}
