using System.Net.Http.Json;
using System.Text.Json;
using Tycoon.Backend.Application.Abstractions;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Infrastructure.SidecarClient;

public sealed class PersonalizationSidecarClient : IPersonalizationSidecarClient
{
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public PersonalizationSidecarClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<SidecarPlayerScoresDto> ScorePlayerAsync(
        SidecarPlayerScoringRequest request,
        CancellationToken ct = default)
    {
        var payload = new
        {
            playerId = request.PlayerId,
            recentEvents = request.RecentEvents.Select(e => new
            {
                eventType = e.EventType,
                eventSource = e.EventSource,
                category = e.Category,
                difficulty = e.Difficulty,
                mode = e.Mode,
                metadata = e.Metadata ?? new Dictionary<string, object>()
            }),
            currentProfile = new
            {
                confidenceLevel = (double)request.CurrentProfile.ConfidenceLevel,
                churnRiskScore = (double)request.CurrentProfile.ChurnRiskScore,
                frustrationRiskScore = (double)request.CurrentProfile.FrustrationRiskScore,
                notificationFatigueScore = (double)request.CurrentProfile.NotificationFatigueScore,
                archetype = request.CurrentProfile.Archetype
            }
        };

        var response = await _http.PostAsJsonAsync("/personalization/score-player", payload, _json, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SidecarScorePlayerResponse>(_json, ct)
            ?? throw new InvalidOperationException("Sidecar returned empty score-player response.");

        return new SidecarPlayerScoresDto(
            (decimal)result.ChurnRiskScore,
            (decimal)result.FrustrationRiskScore,
            (decimal)result.ConfidenceLevel,
            result.RecommendedArchetype,
            result.CategoryStrengths.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
            result.CategoryWeaknesses.ToDictionary(kv => kv.Key, kv => (decimal)kv.Value),
            result.Signals);
    }

    public async Task<IReadOnlyList<SidecarRecommendationCandidateDto>> GetRecommendationCandidatesAsync(
        SidecarRecommendationRequest request,
        CancellationToken ct = default)
    {
        var payload = new
        {
            playerId = request.PlayerId,
            profile = new
            {
                confidenceLevel = (double)request.Profile.ConfidenceLevel,
                churnRiskScore = (double)request.Profile.ChurnRiskScore,
                frustrationRiskScore = (double)request.Profile.FrustrationRiskScore,
                notificationFatigueScore = (double)request.Profile.NotificationFatigueScore,
                archetype = request.Profile.Archetype
            },
            recentEvents = request.RecentEvents.Select(e => new
            {
                eventType = e.EventType,
                eventSource = e.EventSource,
                category = e.Category,
                difficulty = e.Difficulty,
                mode = e.Mode,
                metadata = e.Metadata ?? new Dictionary<string, object>()
            })
        };

        var response = await _http.PostAsJsonAsync("/personalization/recommendation-candidates", payload, _json, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<SidecarCandidatesResponse>(_json, ct)
            ?? throw new InvalidOperationException("Sidecar returned empty recommendation-candidates response.");

        return result.Candidates.Select(c => new SidecarRecommendationCandidateDto(
            c.Type,
            c.TargetId,
            (decimal)c.Score,
            c.Reason,
            c.Payload)).ToList();
    }

    // Local response shapes (not shared — internal to this client)
    private sealed record SidecarScorePlayerResponse(
        double ChurnRiskScore,
        double FrustrationRiskScore,
        double ConfidenceLevel,
        string RecommendedArchetype,
        Dictionary<string, double> CategoryStrengths,
        Dictionary<string, double> CategoryWeaknesses,
        Dictionary<string, object> Signals);

    private sealed record SidecarCandidatesResponse(List<SidecarCandidateItem> Candidates);

    private sealed record SidecarCandidateItem(
        string Type,
        string? TargetId,
        double Score,
        string Reason,
        Dictionary<string, object> Payload);
}
