using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Application.Personalization;

public interface IPersonalizationService
{
    Task<PlayerHomePersonalizationDto> GetHomeAsync(Guid playerId, CancellationToken ct = default);

    Task<IReadOnlyList<PlayerRecommendationDto>> GetRecommendationsAsync(Guid playerId, CancellationToken ct = default);

    Task<StorePersonalizationDto> GetStoreRecommendationsAsync(Guid playerId, CancellationToken ct = default);

    Task<NotificationPersonalizationDto> GetNotificationRecommendationAsync(Guid playerId, CancellationToken ct = default);

    Task AcceptRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);

    Task DismissRecommendationAsync(Guid recommendationId, Guid playerId, CancellationToken ct = default);
}
