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
                        if (await TryHandleIncomingEventAsync(item, writer, ct))
                        {
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
                    if (await TryHandleIncomingEventAsync(body, writer, ct))
                    {
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

            group.MapPost("/track", async (
                [FromBody] JsonElement body,
                IAnalyticsEventWriter writer,
                CancellationToken ct) =>
            {
                if (body.ValueKind != JsonValueKind.Object)
                {
                    return Results.Accepted(value: new
                    {
                        accepted = 0,
                        skipped = 1,
                        message = "Analytics track request accepted."
                    });
                }

                if (TryExtractTrackRequest(body, out var eventName, out var payload, out var timestampUtc) &&
                    string.Equals(eventName, "question_answered", StringComparison.OrdinalIgnoreCase) &&
                    TryMapQuestionAnsweredEvent(payload, timestampUtc, out var evt))
                {
                    await writer.UpsertQuestionAnsweredEventAsync(evt, ct);

                    return Results.Accepted(value: new
                    {
                        accepted = 1,
                        skipped = 0,
                        message = "Analytics track event accepted."
                    });
                }

                return Results.Accepted(value: new
                {
                    accepted = 0,
                    skipped = 1,
                    message = "Analytics track event accepted."
                });
            }).AllowAnonymous();

            // Frontend compatibility endpoint. Some clients post app startup telemetry to
            // /analytics/startup_event. We accept and no-op so clients don't fail noisily.
            group.MapPost("/startup_event", (
                [FromBody] JsonElement body,
                CancellationToken ct) =>
            {
                return Results.Accepted(value: new
                {
                    accepted = 1,
                    skipped = 0,
                    message = "Startup analytics event accepted."
                });
            }).AllowAnonymous();

            group.MapPost("/session_start", (
                [FromBody] JsonElement body,
                CancellationToken ct) =>
            {
                return Results.Accepted(value: new
                {
                    accepted = 0,
                    skipped = 1,
                    message = "Session analytics event accepted."
                });
            }).AllowAnonymous();
        }

        private static async Task<bool> TryHandleIncomingEventAsync(
            JsonElement src,
            IAnalyticsEventWriter writer,
            CancellationToken ct)
        {
            if (TryMapQuestionAnsweredEvent(src, out var directEvent))
            {
                await writer.UpsertQuestionAnsweredEventAsync(directEvent, ct);
                return true;
            }

            if (TryExtractEnvelopePayload(src, out var payload) &&
                TryMapQuestionAnsweredEvent(payload, out var wrappedEvent))
            {
                await writer.UpsertQuestionAnsweredEventAsync(wrappedEvent, ct);
                return true;
            }

            return false;
        }

        private static bool TryExtractEnvelopePayload(JsonElement src, out JsonElement payload)
        {
            payload = default;

            if (src.ValueKind != JsonValueKind.Object)
                return false;

            if (src.TryGetProperty("event", out var eventProp) &&
                eventProp.ValueKind == JsonValueKind.String)
            {
                var eventName = eventProp.GetString();
                if (!string.Equals(eventName, "question_answered", StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            if (!src.TryGetProperty("payload", out var payloadProp) ||
                payloadProp.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            payload = payloadProp;
            return true;
        }

        private static bool TryExtractTrackRequest(
            JsonElement src,
            out string eventName,
            out JsonElement payload,
            out DateTime? timestampUtc)
        {
            eventName = string.Empty;
            payload = default;
            timestampUtc = null;

            if (src.ValueKind != JsonValueKind.Object)
                return false;

            if (!TryGetString(src, "eventName", out eventName))
                return false;

            if (!src.TryGetProperty("properties", out var properties) ||
                properties.ValueKind != JsonValueKind.Object)
            {
                return false;
            }

            if (TryGetDateTime(src, "timestamp", out var parsedTimestamp))
                timestampUtc = parsedTimestamp;

            payload = properties;
            return true;
        }

        private static bool TryMapQuestionAnsweredEvent(JsonElement src, out QuestionAnsweredAnalyticsEvent evt)
            => TryMapQuestionAnsweredEvent(src, null, out evt);

        private static bool TryMapQuestionAnsweredEvent(
            JsonElement src,
            DateTime? fallbackAnsweredAtUtc,
            out QuestionAnsweredAnalyticsEvent evt)
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
                : (fallbackAnsweredAtUtc ?? DateTime.UtcNow);

            // ── Synaptix analytics dimensions (optional, nullable) ──
            var synaptixMode = TryGetString(src, "synaptixMode", out var synaptixModeValue) ? synaptixModeValue : null;
            var surface = TryGetString(src, "surface", out var surfaceValue) ? surfaceValue : null;
            var audienceSegment = TryGetString(src, "audienceSegment", out var audienceSegmentValue) ? audienceSegmentValue : null;
            var entryPoint = TryGetString(src, "entryPoint", out var entryPointValue) ? entryPointValue : null;
            var brandVersion = TryGetString(src, "brandVersion", out var brandVersionValue) ? brandVersionValue : null;

            evt = new QuestionAnsweredAnalyticsEvent(id, matchId, playerId, mode, category, difficulty, isCorrect, answerTimeMs, answeredAtUtc)
            {
                QuestionId = questionId,
                PointsAwarded = pointsAwarded,
                SynaptixMode = synaptixMode,
                Surface = surface,
                AudienceSegment = audienceSegment,
                EntryPoint = entryPoint,
                BrandVersion = brandVersion
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
