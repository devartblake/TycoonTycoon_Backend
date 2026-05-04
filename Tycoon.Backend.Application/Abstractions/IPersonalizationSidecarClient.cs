using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Application.Abstractions;

public interface IPersonalizationSidecarClient
{
    Task<SidecarPlayerScoresDto> ScorePlayerAsync(SidecarPlayerScoringRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
        SidecarRecommendationRequest request,
        CancellationToken ct = default);

    Task<SidecarNotificationScoreDto> GetNotificationScoreAsync(
        SidecarNotificationScoreRequest request,
        CancellationToken ct = default);
}
