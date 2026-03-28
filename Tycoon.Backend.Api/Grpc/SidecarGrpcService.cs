using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Tycoon.Backend.Application.Analytics.Abstractions;
using Tycoon.Backend.Application.Analytics.Models;
using Tycoon.Backend.Application.Events;
using Tycoon.Backend.Api.Grpc;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Grpc;

/// <summary>
/// gRPC service consumed by the Tycoon.Sidecar (Python/FastAPI) process.
/// Listens on the dedicated HTTP/2 port (5001) so the main REST API on
/// port 5000 continues to use HTTP/1.1 without any protocol negotiation overhead.
///
/// Clients call this service to:
///   • Report analytics events (single or streamed batch)
///   • Submit ML inference results (match quality, churn risk, etc.)
///   • Trigger internal backend actions (flag player, update stats, …)
/// </summary>
public sealed class SidecarGrpcService : SidecarService.SidecarServiceBase
{
    private readonly ILogger<SidecarGrpcService> _logger;
    private readonly IAnalyticsEventWriter _analyticsWriter;
    private readonly ISidecarInferenceStore _inferenceStore;
    private readonly IMediator _mediator;

    public SidecarGrpcService(
        ILogger<SidecarGrpcService> logger,
        IAnalyticsEventWriter analyticsWriter,
        ISidecarInferenceStore inferenceStore,
        IMediator mediator)
    {
        _logger = logger;
        _analyticsWriter = analyticsWriter;
        _inferenceStore = inferenceStore;
        _mediator = mediator;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Analytics — single event
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<AnalyticsEventResponse> ReportAnalyticsEvent(
        AnalyticsEventRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return new AnalyticsEventResponse
            {
                Accepted = false,
                RejectReason = "event_type is required"
            };
        }

        if (!TryMapQuestionAnsweredEvent(request, out var evt))
        {
            return new AnalyticsEventResponse
            {
                Accepted = false,
                RejectReason = "unsupported event_type or invalid payload_json"
            };
        }

        await _analyticsWriter.UpsertQuestionAnsweredEventAsync(evt, context.CancellationToken);

        _logger.LogDebug(
            "gRPC analytics event received: type={EventType} entity={EntityId} id={EventId}",
            request.EventType, request.EntityId, evt.Id);

        return new AnalyticsEventResponse
        {
            Accepted = true,
            EventId = evt.Id
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Analytics — client-streaming batch
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<StreamSummary> StreamAnalyticsEvents(
        IAsyncStreamReader<AnalyticsEventRequest> requestStream,
        ServerCallContext context)
    {
        int received = 0, accepted = 0, rejected = 0;

        await foreach (var request in requestStream.ReadAllAsync(context.CancellationToken))
        {
            received++;

            if (string.IsNullOrWhiteSpace(request.EventType))
            {
                rejected++;
                continue;
            }

            if (!TryMapQuestionAnsweredEvent(request, out var evt))
            {
                rejected++;
                continue;
            }

            await _analyticsWriter.UpsertQuestionAnsweredEventAsync(evt, context.CancellationToken);
            accepted++;
        }

        _logger.LogInformation(
            "gRPC analytics stream complete: received={Received} accepted={Accepted} rejected={Rejected}",
            received, accepted, rejected);

        return new StreamSummary
        {
            EventsReceived = received,
            EventsAccepted = accepted,
            EventsRejected = rejected
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ML inference results
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<InferenceResultResponse> SubmitInferenceResult(
        InferenceResultRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ModelName) || string.IsNullOrWhiteSpace(request.EntityId))
        {
            return new InferenceResultResponse { Stored = false };
        }

        var recordId = await _inferenceStore.StoreAsync(
            request.ModelName,
            request.EntityId,
            request.Score,
            request.MetadataJson,
            context.CancellationToken);

        _logger.LogInformation(
            "gRPC inference result: model={Model} entity={EntityId} score={Score:F3} record={RecordId}",
            request.ModelName, request.EntityId, request.Score, recordId);

        return new InferenceResultResponse
        {
            Stored   = true,
            RecordId = recordId
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Backend action trigger
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<BackendActionResponse> TriggerBackendAction(
        BackendActionRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return new BackendActionResponse
            {
                Executed     = false,
                ErrorMessage = "action is required"
            };
        }

        _logger.LogInformation(
            "gRPC backend action: action={Action} target={TargetId}",
            request.Action, request.TargetId);

        if (!string.Equals(request.Action, "admin_event_queue_reprocess", StringComparison.OrdinalIgnoreCase))
        {
            return new BackendActionResponse
            {
                Executed = false,
                ErrorMessage = $"unsupported action '{request.Action}'"
            };
        }

        var parseOk = TryParseReprocessParams(request.ParamsJson, out var reprocessScope, out var reprocessLimit, out var adminUser);
        if (!parseOk)
        {
            return new BackendActionResponse
            {
                Executed = false,
                ErrorMessage = "invalid params_json for admin_event_queue_reprocess"
            };
        }

        var reprocessResult = await _mediator.Send(
            new AdminReprocessEventQueue(
                new AdminEventQueueReprocessRequest(reprocessScope, reprocessLimit),
                adminUser),
            context.CancellationToken);

        return new BackendActionResponse
        {
            Executed = true,
            ResultJson = JsonSerializer.Serialize(reprocessResult)
        };
    }

    private static bool TryMapQuestionAnsweredEvent(AnalyticsEventRequest request, out QuestionAnsweredAnalyticsEvent evt)
    {
        evt = default!;

        if (!string.Equals(request.EventType, "question_answered", StringComparison.OrdinalIgnoreCase))
            return false;

        if (string.IsNullOrWhiteSpace(request.PayloadJson))
            return false;

        using var doc = JsonDocument.Parse(request.PayloadJson);
        var src = doc.RootElement;

        if (!TryGetGuid(src, "playerId", out var playerId)) return false;
        if (!TryGetGuid(src, "matchId", out var matchId)) return false;
        if (!TryGetString(src, "questionId", out var questionId)) return false;

        var id = !string.IsNullOrWhiteSpace(request.EntityId)
            ? request.EntityId
            : $"{playerId:N}:{questionId}:{DateTime.UtcNow.Ticks}";

        var mode = TryGetString(src, "mode", out var modeValue) ? modeValue : "unknown";
        var category = TryGetString(src, "category", out var categoryValue) ? categoryValue : "unknown";
        var difficulty = TryGetInt(src, "difficulty", out var difficultyValue) ? difficultyValue : 0;
        var isCorrect = TryGetBool(src, "isCorrect", out var isCorrectValue) && isCorrectValue;
        var answerTimeMs = TryGetInt(src, "answerTimeMs", out var answerTimeMsValue) ? answerTimeMsValue : 0;
        var pointsAwarded = TryGetInt(src, "pointsAwarded", out var pointsAwardedValue) ? pointsAwardedValue : 0;
        var answeredAtUtc = request.TimestampMs > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(request.TimestampMs).UtcDateTime
            : (TryGetDateTime(src, "answeredAtUtc", out var answeredAtUtcValue) ? answeredAtUtcValue : DateTime.UtcNow);

        evt = new QuestionAnsweredAnalyticsEvent(id, matchId, playerId, mode, category, difficulty, isCorrect, answerTimeMs, answeredAtUtc)
        {
            QuestionId = questionId,
            PointsAwarded = pointsAwarded
        };

        return true;
    }

    private static bool TryParseReprocessParams(string paramsJson, out string scope, out int limit, out string? adminUser)
    {
        scope = "all";
        limit = 1000;
        adminUser = null;

        if (string.IsNullOrWhiteSpace(paramsJson))
            return true;

        try
        {
            using var doc = JsonDocument.Parse(paramsJson);
            var root = doc.RootElement;

            if (TryGetString(root, "scope", out var parsedScope))
                scope = parsedScope;

            if (TryGetInt(root, "limit", out var parsedLimit) && parsedLimit > 0)
                limit = parsedLimit;

            if (TryGetString(root, "adminUser", out var parsedAdminUser))
                adminUser = parsedAdminUser;

            return true;
        }
        catch
        {
            return false;
        }
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
