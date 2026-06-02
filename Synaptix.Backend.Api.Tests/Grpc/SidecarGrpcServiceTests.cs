using System.Collections;
using System.Text.Json;
using Grpc.Core;
using Mediator;
using Microsoft.Extensions.Logging.Abstractions;
using Synaptix.Backend.Api.Grpc;
using Synaptix.Backend.Application.Analytics.Abstractions;
using Synaptix.Backend.Application.Analytics.Models;
using Synaptix.Backend.Application.Events;
using Synaptix.Shared.Contracts.Dtos;

namespace Synaptix.Backend.Api.Tests.Grpc;

public sealed class SidecarGrpcServiceTests
{
    [Fact]
    public async Task ReportAnalyticsEvent_Should_Reject_Unsupported_Type()
    {
        var svc = CreateService(out var writer, out _);
        var ctx = TestServerCallContext.Create();

        var res = await svc.ReportAnalyticsEvent(new AnalyticsEventRequest
        {
            EventType = "unsupported",
            EntityId = "e1",
            PayloadJson = "{}"
        }, ctx);

        Assert.False(res.Accepted);
        Assert.Contains("unsupported", res.RejectReason, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(writer.Events);
    }

    [Fact]
    public async Task ReportAnalyticsEvent_Should_Persist_QuestionAnswered()
    {
        var svc = CreateService(out var writer, out _);
        var ctx = TestServerCallContext.Create();

        var res = await svc.ReportAnalyticsEvent(new AnalyticsEventRequest
        {
            EventType = "question_answered",
            EntityId = "evt-1",
            PayloadJson = ValidQuestionAnsweredPayload()
        }, ctx);

        Assert.True(res.Accepted);
        Assert.Equal("evt-1", res.EventId);
        Assert.Single(writer.Events);
    }

    [Fact]
    public async Task StreamAnalyticsEvents_Should_Return_Accepted_And_Rejected_Counts()
    {
        var svc = CreateService(out var writer, out _);
        var ctx = TestServerCallContext.Create();

        var stream = new TestAsyncStreamReader<AnalyticsEventRequest>(
        [
            new AnalyticsEventRequest { EventType = "question_answered", EntityId = "evt-a", PayloadJson = ValidQuestionAnsweredPayload() },
            new AnalyticsEventRequest { EventType = "question_answered", EntityId = "evt-b", PayloadJson = "{}" },
            new AnalyticsEventRequest { EventType = "unsupported", EntityId = "evt-c", PayloadJson = "{}" },
        ]);

        var summary = await svc.StreamAnalyticsEvents(stream, ctx);

        Assert.Equal(3, summary.EventsReceived);
        Assert.Equal(1, summary.EventsAccepted);
        Assert.Equal(2, summary.EventsRejected);
        Assert.Single(writer.Events);
    }

    [Fact]
    public async Task StreamAnalyticsEvents_Should_Stop_At_Configured_Stream_Cap()
    {
        var svc = CreateService(out var writer, out _);
        var ctx = TestServerCallContext.Create();

        var requests = Enumerable.Range(1, SidecarGrpcService.MaxAnalyticsEventsPerStream + 1)
            .Select(i => new AnalyticsEventRequest
            {
                EventType = "question_answered",
                EntityId = $"evt-{i}",
                PayloadJson = ValidQuestionAnsweredPayload()
            })
            .ToList();

        var stream = new TestAsyncStreamReader<AnalyticsEventRequest>(requests);

        var summary = await svc.StreamAnalyticsEvents(stream, ctx);

        Assert.Equal(SidecarGrpcService.MaxAnalyticsEventsPerStream, summary.EventsReceived);
        Assert.Equal(SidecarGrpcService.MaxAnalyticsEventsPerStream, summary.EventsAccepted);
        Assert.Equal(1, summary.EventsRejected);
        Assert.Equal(SidecarGrpcService.MaxAnalyticsEventsPerStream, writer.Events.Count);
    }

    [Fact]
    public async Task StreamAnalyticsEvents_Should_Return_Summary_When_Canceled()
    {
        var svc = CreateService(out var writer, out _);
        var cts = new CancellationTokenSource();
        var ctx = TestServerCallContext.Create(cts);

        var requests = new List<AnalyticsEventRequest>
        {
            new() { EventType = "question_answered", EntityId = "evt-1", PayloadJson = ValidQuestionAnsweredPayload() },
            new() { EventType = "question_answered", EntityId = "evt-2", PayloadJson = ValidQuestionAnsweredPayload() },
        };

        var stream = new TestAsyncStreamReader<AnalyticsEventRequest>(
            requests,
            onMoveNext: index =>
            {
                if (index == 1)
                    cts.Cancel();
            },
            throwIfCanceled: true);

        var summary = await svc.StreamAnalyticsEvents(stream, ctx);

        Assert.Equal(1, summary.EventsReceived);
        Assert.Equal(1, summary.EventsAccepted);
        Assert.Equal(0, summary.EventsRejected);
        Assert.Single(writer.Events);
    }

    [Fact]
    public async Task SubmitInferenceResult_Should_Store_And_Return_RecordId()
    {
        var store = new InMemorySidecarInferenceStore();
        var svc = CreateService(out _, out _, store);
        var ctx = TestServerCallContext.Create();

        var res = await svc.SubmitInferenceResult(new InferenceResultRequest
        {
            ModelName = "churn-risk",
            EntityId = "player-1",
            Score = 0.82f,
            MetadataJson = "{}"
        }, ctx);

        Assert.True(res.Stored);
        Assert.False(string.IsNullOrWhiteSpace(res.RecordId));
    }

    [Fact]
    public async Task SubmitInferenceResult_Should_Be_Idempotent_For_Same_Payload()
    {
        var store = new InMemorySidecarInferenceStore();
        var svc = CreateService(out _, out _, store);
        var ctx = TestServerCallContext.Create();

        var req = new InferenceResultRequest
        {
            ModelName = "churn-risk",
            EntityId = "player-1",
            Score = 0.82f,
            MetadataJson = "{}"
        };

        var first = await svc.SubmitInferenceResult(req, ctx);
        var second = await svc.SubmitInferenceResult(req, ctx);

        Assert.True(first.Stored);
        Assert.True(second.Stored);
        Assert.Equal(first.RecordId, second.RecordId);
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public async Task TriggerBackendAction_Should_Reject_Unsupported_Action()
    {
        var svc = CreateService(out _, out var mediator);
        var ctx = TestServerCallContext.Create();

        var res = await svc.TriggerBackendAction(new BackendActionRequest
        {
            Action = "unknown",
            TargetId = "x"
        }, ctx);

        Assert.False(res.Executed);
        Assert.Contains("unsupported action", res.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(mediator.SentRequests);
    }

    [Fact]
    public async Task TriggerBackendAction_Should_Dispatch_Admin_Reprocess()
    {
        var svc = CreateService(out _, out var mediator);
        var ctx = TestServerCallContext.Create();

        var res = await svc.TriggerBackendAction(new BackendActionRequest
        {
            Action = "admin_event_queue_reprocess",
            TargetId = "ignored",
            ParamsJson = JsonSerializer.Serialize(new { scope = "failed", limit = 25, adminUser = "ops@tycoon" })
        }, ctx);

        Assert.True(res.Executed);
        Assert.Contains("queued", res.ResultJson, StringComparison.OrdinalIgnoreCase);
        Assert.Single(mediator.SentRequests);
        Assert.IsType<AdminReprocessEventQueue>(mediator.SentRequests[0]);
    }

    private static SidecarGrpcService CreateService(
        out FakeAnalyticsWriter writer,
        out FakeMediator mediator,
        ISidecarInferenceStore? inferenceStore = null)
    {
        writer = new FakeAnalyticsWriter();
        mediator = new FakeMediator();
        return new SidecarGrpcService(
            NullLogger<SidecarGrpcService>.Instance,
            writer,
            inferenceStore ?? new InMemorySidecarInferenceStore(),
            mediator);
    }

    private static string ValidQuestionAnsweredPayload()
    {
        return JsonSerializer.Serialize(new
        {
            playerId = Guid.NewGuid(),
            matchId = Guid.NewGuid(),
            questionId = "q-1",
            mode = "ranked",
            category = "history",
            difficulty = 2,
            isCorrect = true,
            answerTimeMs = 1234,
            pointsAwarded = 50,
            answeredAtUtc = DateTime.UtcNow
        });
    }

    private sealed class FakeAnalyticsWriter : IAnalyticsEventWriter
    {
        public List<QuestionAnsweredAnalyticsEvent> Events { get; } = [];

        public Task UpsertQuestionAnsweredEventAsync(QuestionAnsweredAnalyticsEvent e, CancellationToken ct)
        {
            Events.Add(e);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMediator : IMediator
    {
        public List<object> SentRequests { get; } = [];

        public ValueTask Publish(object notification, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => ValueTask.CompletedTask;

        public ValueTask<TResponse> Send<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
            where TResponse : notnull
            => Send((IRequest<TResponse>)command, cancellationToken);

        public ValueTask<TResponse> Send<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
            where TResponse : notnull
            => Send((IRequest<TResponse>)query, cancellationToken);

        public ValueTask<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            if (request is AdminReprocessEventQueue)
            {
                return ValueTask.FromResult((TResponse)(object)new AdminEventQueueReprocessResponse("job_1", "queued"));
            }

            throw new InvalidOperationException($"Unsupported request type: {request.GetType().Name}");
        }

        public ValueTask Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            SentRequests.Add(request!);
            return ValueTask.CompletedTask;
        }

        public ValueTask<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            return ValueTask.FromResult<object?>(new { status = "queued" });
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamQuery<TResponse> query, CancellationToken cancellationToken = default)
            where TResponse : notnull
            => CreateStream((IStreamRequest<TResponse>)query, cancellationToken);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamCommand<TResponse> command, CancellationToken cancellationToken = default)
            where TResponse : notnull
            => CreateStream((IStreamRequest<TResponse>)command, cancellationToken);

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();
    }

    private sealed class TestAsyncStreamReader<T>(
        IReadOnlyList<T> items,
        Action<int>? onMoveNext = null,
        bool throwIfCanceled = false) : IAsyncStreamReader<T>
    {
        private int _index = -1;
        public T Current { get; private set; } = default!;

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            _index++;
            onMoveNext?.Invoke(_index);
            if (throwIfCanceled && cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);

            if (_index >= items.Count)
                return Task.FromResult(false);

            Current = items[_index];
            return Task.FromResult(true);
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly CancellationToken _cancellationToken;
        private readonly Metadata _requestHeaders = new();

        private TestServerCallContext(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;
        }

        public static TestServerCallContext Create(CancellationTokenSource? cts = null)
            => new(cts?.Token ?? CancellationToken.None);

        protected override string MethodCore => "test";
        protected override string HostCore => "localhost";
        protected override string PeerCore => "peer";
        protected override DateTime DeadlineCore => DateTime.UtcNow.AddMinutes(1);
        protected override Metadata RequestHeadersCore => _requestHeaders;
        protected override CancellationToken CancellationTokenCore => _cancellationToken;
        protected override Metadata ResponseTrailersCore { get; } = new();
        protected override Status StatusCore { get; set; }
        protected override WriteOptions? WriteOptionsCore { get; set; }
        protected override AuthContext AuthContextCore => new("test", new Dictionary<string, List<AuthProperty>>());
        protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) => throw new NotSupportedException();
        protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
    }
}
