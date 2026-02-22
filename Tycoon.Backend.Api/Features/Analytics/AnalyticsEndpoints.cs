using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using System.Text.Json;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;

namespace Tycoon.Backend.Api.Features.Analytics
{
    public static class AnalyticsEndpoints
    {
        public static void Map(WebApplication app)
        {
            var analytics = app.MapGroup("/analytics").WithTags("Analytics").WithOpenApi();
            var analyticsV1 = app.MapGroup("/api/v1/analytics").WithTags("Analytics").WithOpenApi();

            MapIngestionRoutes(analytics);
            MapIngestionRoutes(analyticsV1);
        }

        private static void MapIngestionRoutes(RouteGroupBuilder group)
        {
            group.MapPost("/events", async (
                [FromBody] JsonElement body,
                IAnalyticsEventWriter writer,
                CancellationToken ct) =>
            {
                var accepted = 0;
                var skipped = 0;

                if (body.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in body.EnumerateArray())
                    {
                        if (TryMapQuestionAnsweredEvent(item, out var evt))
                        {
                            await writer.UpsertQuestionAnsweredEventAsync(evt, ct);
                            accepted++;
                        }
                        else
                        {
                            skipped++;
                        }
                    }
                }
                else if (body.ValueKind == JsonValueKind.Object)
                {
                    if (TryMapQuestionAnsweredEvent(body, out var evt))
                    {
                        await writer.UpsertQuestionAnsweredEventAsync(evt, ct);
                        accepted++;
                    }
                    else
                    {
                        skipped++;
                    }
                }
                else
                {
                    skipped++;
                }

                return Results.Accepted(value: new
                {
                    accepted,
                    skipped,
                    message = "Analytics event ingestion accepted."
                });
            }).AllowAnonymous();
        }

        private static bool TryMapQuestionAnsweredEvent(JsonElement src, out QuestionAnsweredAnalyticsEvent evt)
        {
            evt = default!;

            if (!TryGetGuid(src, "playerId", out var playerId)) return false;
            if (!TryGetGuid(src, "matchId", out var matchId)) return false;
            if (!TryGetString(src, "questionId", out var questionId)) return false;

            var id = TryGetString(src, "id", out var existingId)
                ? existingId
                : (TryGetString(src, "eventId", out var eventId)
                    ? eventId
                    : $"{playerId:N}:{questionId}:{DateTime.UtcNow.Ticks}");

            var mode = TryGetString(src, "mode", out var modeValue) ? modeValue : "unknown";
            var category = TryGetString(src, "category", out var categoryValue) ? categoryValue : "unknown";
            var difficulty = TryGetInt(src, "difficulty", out var difficultyValue) ? difficultyValue : 0;
            var isCorrect = TryGetBool(src, "isCorrect", out var isCorrectValue) && isCorrectValue;
            var answerTimeMs = TryGetInt(src, "answerTimeMs", out var answerTimeMsValue) ? answerTimeMsValue : 0;
            var pointsAwarded = TryGetInt(src, "pointsAwarded", out var pointsAwardedValue) ? pointsAwardedValue : 0;
            var answeredAtUtc = TryGetDateTime(src, "answeredAtUtc", out var answeredAtUtcValue)
                ? answeredAtUtcValue
                : DateTime.UtcNow;

            evt = new QuestionAnsweredAnalyticsEvent(id, matchId, playerId, mode, category, difficulty, isCorrect, answerTimeMs, answeredAtUtc)
            {
                QuestionId = questionId,
                PointsAwarded = pointsAwarded
            };

            return true;
        }

        private static bool TryGetString(JsonElement src, string key, out string value)
        {
            value = string.Empty;
            if (!src.TryGetProperty(key, out var prop) || prop.ValueKind != JsonValueKind.String)
                return false;

            value = prop.GetString() ?? string.Empty;
            return !string.IsNullOrWhiteSpace(value);
        }

        private static bool TryGetGuid(JsonElement src, string key, out Guid value)
        {
            value = Guid.Empty;
            if (!TryGetString(src, key, out var raw))
                return false;

            return Guid.TryParse(raw, out value);
        }

        private static bool TryGetInt(JsonElement src, string key, out int value)
        {
            value = 0;
            if (!src.TryGetProperty(key, out var prop))
                return false;

            if (prop.ValueKind == JsonValueKind.Number)
                return prop.TryGetInt32(out value);

            if (prop.ValueKind == JsonValueKind.String)
                return int.TryParse(prop.GetString(), out value);

            return false;
        }

        private static bool TryGetBool(JsonElement src, string key, out bool value)
        {
            value = false;
            if (!src.TryGetProperty(key, out var prop))
                return false;

            if (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False)
            {
                value = prop.GetBoolean();
                return true;
            }

            if (prop.ValueKind == JsonValueKind.String)
                return bool.TryParse(prop.GetString(), out value);

            return false;
        }

        private static bool TryGetDateTime(JsonElement src, string key, out DateTime value)
        {
            value = default;
            if (!TryGetString(src, key, out var raw))
                return false;

            return DateTime.TryParse(raw, out value);
        }
    }
}
