using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Tycoon.Backend.Api.Features.Ml;

public static class MlScoringEndpoints
{
    public static void Map(WebApplication app)
    {
        var g = app.MapGroup("/ml").WithTags("ML").WithOpenApi().RequireAuthorization();
        g.MapPost("/churn-risk", EstimateChurnRisk);
        g.MapPost("/match-quality", EstimateMatchQuality);
    }

    private static async Task<IResult> EstimateChurnRisk(
        [FromBody] ChurnRiskRequest req,
        IConfiguration cfg,
        IHttpClientFactory httpClientFactory,
        CancellationToken ct)
    {
        if (req.PlayerId == Guid.Empty)
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "playerId is required." });

        var url = cfg["MlModels:ChurnRiskUrl"];
        var apiKey = cfg["MlModels:ApiKey"];
        if (!string.IsNullOrWhiteSpace(url))
        {
            var remote = await TryPostModelAsync(httpClientFactory, url, req, ct, apiKey);
            if (remote is not null)
                return Results.Ok(new ChurnRiskResponse(req.PlayerId, remote.Score, ToRiskTier(remote.Score), "deployed-model"));
        }

        var heuristicScore = Math.Clamp(
            (req.DisconnectRate * 0.45m) +
            ((1m - req.CorrectRate) * 0.30m) +
            (Math.Min(req.RecentSessions, 20) < 5 ? 0.15m : 0m) +
            (req.DaysSinceLastSeen >= 5 ? 0.20m : req.DaysSinceLastSeen >= 2 ? 0.10m : 0m),
            0m,
            1m);

        return Results.Ok(new ChurnRiskResponse(req.PlayerId, heuristicScore, ToRiskTier(heuristicScore), "heuristic"));
    }

    private static async Task<IResult> EstimateMatchQuality(
        [FromBody] MatchQualityRequest req,
        IConfiguration cfg,
        IHttpClientFactory httpClientFactory,
        CancellationToken ct)
    {
        if (req.MatchId == Guid.Empty)
            return Results.BadRequest(new { code = "VALIDATION_ERROR", message = "matchId is required." });

        var url = cfg["MlModels:MatchQualityUrl"];
        var apiKey = cfg["MlModels:ApiKey"];
        if (!string.IsNullOrWhiteSpace(url))
        {
            var remote = await TryPostModelAsync(httpClientFactory, url, req, ct, apiKey);
            if (remote is not null)
                return Results.Ok(new MatchQualityResponse(req.MatchId, remote.Score, ToQualityBand(remote.Score), "deployed-model"));
        }

        var speedTerm = req.AverageAnswerTimeMs switch
        {
            <= 0 => 0m,
            <= 4500 => 0.35m,
            <= 9000 => 0.25m,
            _ => 0.10m
        };

        var heuristicScore = Math.Clamp(
            speedTerm +
            (req.CorrectRate * 0.45m) +
            ((1m - req.DisconnectRate) * 0.20m),
            0m,
            1m);

        return Results.Ok(new MatchQualityResponse(req.MatchId, heuristicScore, ToQualityBand(heuristicScore), "heuristic"));
    }

    private static async Task<ModelScorePayload?> TryPostModelAsync(
        IHttpClientFactory httpClientFactory,
        string url,
        object payload,
        CancellationToken ct,
        string? bearerToken)
    {
        try
        {
            var http = httpClientFactory.CreateClient();
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = JsonContent.Create(payload)
            };
            if (!string.IsNullOrWhiteSpace(bearerToken))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken.Trim());

            using var resp = await http.SendAsync(req, ct);
            if (!resp.IsSuccessStatusCode) return null;
            return await resp.Content.ReadFromJsonAsync<ModelScorePayload>(cancellationToken: ct);
        }
        catch
        {
            return null;
        }
    }

    private static string ToRiskTier(decimal score)
        => score >= 0.70m ? "high" : score >= 0.40m ? "medium" : "low";

    private static string ToQualityBand(decimal score)
        => score >= 0.80m ? "excellent" : score >= 0.60m ? "good" : score >= 0.40m ? "fair" : "poor";

    public sealed record ChurnRiskRequest(Guid PlayerId, decimal CorrectRate, decimal DisconnectRate, int RecentSessions, int DaysSinceLastSeen);
    public sealed record ChurnRiskResponse(Guid PlayerId, decimal Score, string RiskTier, string Source);

    public sealed record MatchQualityRequest(Guid MatchId, decimal CorrectRate, decimal DisconnectRate, int AverageAnswerTimeMs);
    public sealed record MatchQualityResponse(Guid MatchId, decimal Score, string QualityBand, string Source);

    private sealed record ModelScorePayload(decimal Score);
}
