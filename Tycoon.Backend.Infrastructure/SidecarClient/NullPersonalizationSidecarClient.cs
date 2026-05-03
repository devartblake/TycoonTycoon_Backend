using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Infrastructure.SidecarClient;

/// <summary>
/// No-op implementation used when <see cref="SidecarPersonalizationOptions.Enabled"/> is <c>false</c>.
/// Both methods return empty results so the caller always falls back to local rules.
/// </summary>
public sealed class NullPersonalizationSidecarClient : IPersonalizationSidecarClient
{
    public Task<SidecarPlayerScoresDto> ScorePlayerAsync(
        SidecarPlayerScoringRequest request,
        CancellationToken ct = default) =>
        Task.FromResult(new SidecarPlayerScoresDto(
            ChurnRiskScore: 0m,
            FrustrationRiskScore: 0m,
            ConfidenceLevel: 0.50m,
            RecommendedArchetype: "new_player",
            CategoryStrengths: new Dictionary<string, decimal>(),
            CategoryWeaknesses: new Dictionary<string, decimal>(),
            Signals: new Dictionary<string, object>()));

    public Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
        SidecarRecommendationRequest request,
        CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<SidecarRecommendationCandidateDto>>(
            Array.Empty<SidecarRecommendationCandidateDto>());
}
