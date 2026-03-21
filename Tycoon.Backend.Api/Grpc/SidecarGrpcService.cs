using Grpc.Core;
using Tycoon.Backend.Api.Grpc;

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

    public SidecarGrpcService(ILogger<SidecarGrpcService> logger)
    {
        _logger = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Analytics — single event
    // ─────────────────────────────────────────────────────────────────────────

    public override Task<AnalyticsEventResponse> ReportAnalyticsEvent(
        AnalyticsEventRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            return Task.FromResult(new AnalyticsEventResponse
            {
                Accepted = false,
                RejectReason = "event_type is required"
            });
        }

        var eventId = Guid.NewGuid().ToString("N");

        _logger.LogDebug(
            "gRPC analytics event received: type={EventType} entity={EntityId} id={EventId}",
            request.EventType, request.EntityId, eventId);

        // TODO: forward to IAnalyticsService / MediatR command once wired
        return Task.FromResult(new AnalyticsEventResponse
        {
            Accepted = true,
            EventId  = eventId
        });
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

            // TODO: batch-insert via IAnalyticsService
            _logger.LogDebug(
                "gRPC stream event: type={EventType} entity={EntityId}",
                request.EventType, request.EntityId);

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

    public override Task<InferenceResultResponse> SubmitInferenceResult(
        InferenceResultRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.ModelName) || string.IsNullOrWhiteSpace(request.EntityId))
        {
            return Task.FromResult(new InferenceResultResponse { Stored = false });
        }

        var recordId = Guid.NewGuid().ToString("N");

        _logger.LogInformation(
            "gRPC inference result: model={Model} entity={EntityId} score={Score:F3} record={RecordId}",
            request.ModelName, request.EntityId, request.Score, recordId);

        // TODO: persist via IInferenceResultRepository once available
        return Task.FromResult(new InferenceResultResponse
        {
            Stored   = true,
            RecordId = recordId
        });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Backend action trigger
    // ─────────────────────────────────────────────────────────────────────────

    public override Task<BackendActionResponse> TriggerBackendAction(
        BackendActionRequest request,
        ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.Action))
        {
            return Task.FromResult(new BackendActionResponse
            {
                Executed     = false,
                ErrorMessage = "action is required"
            });
        }

        _logger.LogInformation(
            "gRPC backend action: action={Action} target={TargetId}",
            request.Action, request.TargetId);

        // TODO: dispatch to MediatR based on request.Action string
        return Task.FromResult(new BackendActionResponse
        {
            Executed   = true,
            ResultJson = "{\"status\":\"queued\"}"
        });
    }
}
