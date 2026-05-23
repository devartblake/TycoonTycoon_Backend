using Grpc.Core;
using Synaptix.Backend.Api.Grpc;

namespace Synaptix.Backend.Api.Tests.Grpc;

public sealed class MobileMatchSessionTests
{
    [Fact]
    public void ApplyAnswerResult_Should_Update_RunningScore_And_CorrectCount()
    {
        var session = new MatchSession("match-1");
        var writer = new TestServerStreamWriter();
        session.AddParticipant("p1", writer);

        var first = session.ApplyAnswerResult("p1", pointsAwarded: 100, isCorrect: true);
        var second = session.ApplyAnswerResult("p1", pointsAwarded: 50, isCorrect: false);

        Assert.Equal((100, 1), first);
        Assert.Equal((150, 1), second);
    }

    [Fact]
    public async Task BroadcastExceptAsync_Should_Only_Write_To_Other_Participants()
    {
        var session = new MatchSession("match-1");
        var writerP1 = new TestServerStreamWriter();
        var writerP2 = new TestServerStreamWriter();

        session.AddParticipant("p1", writerP1);
        session.AddParticipant("p2", writerP2);

        var evt = new MatchEvent
        {
            OpponentScore = new OpponentScoreEvent
            {
                OpponentPlayerId = "p1",
                Score = 100,
                CorrectCount = 1
            }
        };

        await session.BroadcastExceptAsync("p1", evt, CancellationToken.None);

        Assert.Empty(writerP1.Events);
        Assert.Single(writerP2.Events);
    }

    [Fact]
    public async Task ApplyAnswerResult_Should_Keep_Player_State_Consistent_Under_Concurrent_Updates()
    {
        var session = new MatchSession("match-1");
        session.AddParticipant("p1", new TestServerStreamWriter());
        session.AddParticipant("p2", new TestServerStreamWriter());

        var p1Updates = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => session.ApplyAnswerResult("p1", pointsAwarded: 1, isCorrect: true)));
        var p2Updates = Enumerable.Range(0, 100)
            .Select(_ => Task.Run(() => session.ApplyAnswerResult("p2", pointsAwarded: 2, isCorrect: false)));

        await Task.WhenAll(p1Updates.Concat(p2Updates));

        var p1Snapshot = session.ApplyAnswerResult("p1", pointsAwarded: 0, isCorrect: false);
        var p2Snapshot = session.ApplyAnswerResult("p2", pointsAwarded: 0, isCorrect: false);

        Assert.Equal((100, 100), p1Snapshot);
        Assert.Equal((200, 0), p2Snapshot);
    }

    private sealed class TestServerStreamWriter : IServerStreamWriter<MatchEvent>
    {
        public List<MatchEvent> Events { get; } = [];
        public WriteOptions? WriteOptions { get; set; }

        public Task WriteAsync(MatchEvent message)
        {
            Events.Add(message);
            return Task.CompletedTask;
        }

        public Task WriteAsync(MatchEvent message, CancellationToken cancellationToken)
        {
            Events.Add(message);
            return Task.CompletedTask;
        }
    }
}
