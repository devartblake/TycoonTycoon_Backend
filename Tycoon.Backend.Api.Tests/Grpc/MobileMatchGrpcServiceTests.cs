using Grpc.Core;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Tycoon.Backend.Api.Grpc;
using Tycoon.Backend.Application.Leaderboards;
using Tycoon.Backend.Application.Matches;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Tests.Grpc;

public sealed class MobileMatchGrpcServiceTests
{
    [Fact]
    public async Task PlayMatch_Should_Emit_AnswerResult_With_RunningScore()
    {
        var mediator = new FakeMediator();
        var service = CreateService(mediator);

        var actions = new List<PlayerAction>
        {
            new()
            {
                Join = new JoinMatchAction
                {
                    MatchId = "m-1",
                    PlayerId = "p-1"
                }
            },
            new()
            {
                Answer = new SubmitAnswerAction
                {
                    MatchId = "m-1",
                    QuestionId = Guid.NewGuid().ToString(),
                    SelectedOptionId = "A"
                }
            }
        };

        var requestStream = new TestAsyncStreamReader<PlayerAction>(actions);
        var responseStream = new TestServerStreamWriter<MatchEvent>();
        var ctx = TestServerCallContext.CreateWithBearer();

        await service.PlayMatch(requestStream, responseStream, ctx);

        var answerResult = responseStream.Events.Select(e => e.AnswerResult).FirstOrDefault(e => e is not null);
        Assert.NotNull(answerResult);
        Assert.True(answerResult!.IsCorrect);
        Assert.Equal(100, answerResult.PointsAwarded);
        Assert.Equal(100, answerResult.RunningScore);
    }

    [Fact]
    public async Task WatchLeaderboard_Should_Stream_Live_Leaderboard_Update()
    {
        var mediator = new FakeMediator();
        var service = CreateService(mediator);

        var cts = new CancellationTokenSource();
        var ctx = TestServerCallContext.CreateWithBearer(cts);
        var writer = new TestServerStreamWriter<LeaderboardUpdate>(() => cts.Cancel());

        await service.WatchLeaderboard(new LeaderboardWatchRequest
        {
            PlayerId = Guid.NewGuid().ToString(),
            Mode = "ranked",
            WindowSize = 3
        }, writer, ctx);

        Assert.Single(writer.Events);
        var update = writer.Events[0];
        Assert.NotEqual(0, update.SnapshotAtMs);
        Assert.NotEmpty(update.Nearby);
    }

    private sealed class FakeMediator : IMediator
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            return request switch
            {
                EvaluateMatchAnswer => Task.FromResult((TResponse)(object)new MatchAnswerEvaluationResult("A", true, 100)),
                GetMyTier => Task.FromResult((TResponse)(object)new MyTierDto(Guid.NewGuid(), 1, 7, 42, 1234, 0.5)),
                GetTierLeaderboard => Task.FromResult((TResponse)(object)new TierLeaderboardDto(
                    1,
                    1,
                    5,
                    1,
                    [new TierLeaderboardEntryDto(Guid.NewGuid(), "PlayerOne", "US", 10, 1234, 42, 7, 0.5)])),
                _ => throw new InvalidOperationException($"Unhandled request type: {request.GetType().Name}")
            };
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();
    }

    private static MobileMatchGrpcService CreateService(IMediator mediator)
    {
        var logger = NullLogger<MobileMatchGrpcService>.Instance;
        var ctors = typeof(MobileMatchGrpcService).GetConstructors();

        var twoParamCtor = ctors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 2
                && typeof(IMediator).IsAssignableFrom(p[0].ParameterType)
                && typeof(ILogger<MobileMatchGrpcService>).IsAssignableFrom(p[1].ParameterType);
        });
        if (twoParamCtor is not null)
            return (MobileMatchGrpcService)twoParamCtor.Invoke([mediator, logger]);

        var threeParamCtor = ctors.FirstOrDefault(c =>
        {
            var p = c.GetParameters();
            return p.Length == 3
                && typeof(IMediator).IsAssignableFrom(p[0].ParameterType)
                && typeof(ILogger<MobileMatchGrpcService>).IsAssignableFrom(p[2].ParameterType);
        });
        if (threeParamCtor is not null)
            return (MobileMatchGrpcService)threeParamCtor.Invoke([mediator, null!, logger]);

        throw new InvalidOperationException("No supported MobileMatchGrpcService constructor signature found.");
    }

    private sealed class TestAsyncStreamReader<T>(IReadOnlyList<T> items) : IAsyncStreamReader<T>
    {
        private int _index = -1;
        public T Current { get; private set; } = default!;

        public Task<bool> MoveNext(CancellationToken cancellationToken)
        {
            _index++;
            if (_index >= items.Count)
                return Task.FromResult(false);

            Current = items[_index];
            return Task.FromResult(true);
        }
    }

    private sealed class TestServerStreamWriter<T>(Action? onFirstWrite = null) : IServerStreamWriter<T>
    {
        private bool _wrote;
        public List<T> Events { get; } = [];
        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(T message)
        {
            Events.Add(message);
            Trigger();
            return Task.CompletedTask;
        }

        public Task WriteAsync(T message, CancellationToken cancellationToken)
        {
            Events.Add(message);
            Trigger();
            return Task.CompletedTask;
        }

        private void Trigger()
        {
            if (_wrote) return;
            _wrote = true;
            onFirstWrite?.Invoke();
        }
    }

    private sealed class TestServerCallContext : ServerCallContext
    {
        private readonly Metadata _requestHeaders;
        private readonly CancellationToken _cancellationToken;

        private TestServerCallContext(Metadata headers, CancellationToken cancellationToken)
        {
            _requestHeaders = headers;
            _cancellationToken = cancellationToken;
        }

        public static TestServerCallContext CreateWithBearer(CancellationTokenSource? cts = null)
        {
            var headers = new Metadata { { "authorization", "Bearer test-token" } };
            return new TestServerCallContext(headers, cts?.Token ?? CancellationToken.None);
        }

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
