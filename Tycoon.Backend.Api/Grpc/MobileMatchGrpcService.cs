using System.Collections.Concurrent;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Tycoon.Backend.Application.Leaderboards;
using Tycoon.Backend.Application.Matches;
using Tycoon.Backend.Api.Grpc;
using Tycoon.Shared.Contracts.Dtos;

namespace Tycoon.Backend.Api.Grpc;

/// <summary>
/// gRPC service for native mobile (Flutter) clients.
/// Exposes four RPCs on the HTTP/2 port (5001):
///
///   StartMatch      — unary; mirrors POST /mobile/matches/start
///   SubmitMatch     — unary; mirrors POST /mobile/matches/submit
///   PlayMatch       — bidirectional stream; live match session
///   WatchLeaderboard — server stream; live rank neighbourhood
///
/// Authentication: clients must send an `authorization: Bearer <jwt>`
/// gRPC metadata header (same token from POST /auth/login).
/// </summary>
public sealed class MobileMatchGrpcService : MobileMatchService.MobileMatchServiceBase
{
    // Active bidirectional match streams keyed by matchId.
    // Used to fan-out MatchEvents (opponent score, timer, end) to all
    // participants in the same match.
    private static readonly ConcurrentDictionary<string, MatchSession> _sessions = new();

    private readonly IMediator _mediator;
    private readonly ILogger<MobileMatchGrpcService> _logger;

    public MobileMatchGrpcService(IMediator mediator, ILogger<MobileMatchGrpcService> logger)
    {
        _mediator = mediator;
        _logger   = logger;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // StartMatch — unary
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<GrpcStartMatchResponse> StartMatch(
        GrpcStartMatchRequest request,
        ServerCallContext context)
    {
        RequireAuth(context);

        if (!Guid.TryParse(request.HostPlayerId, out var hostId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "host_player_id must be a valid UUID"));

        var result = await _mediator.Send(
            new Application.Matches.StartMatch(hostId, request.Mode),
            context.CancellationToken);

        return new GrpcStartMatchResponse
        {
            MatchId   = result.MatchId.ToString(),
            StartedAt = result.StartedAt.ToUnixTimeMilliseconds()
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    // SubmitMatch — unary
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task<GrpcSubmitMatchResponse> SubmitMatch(
        GrpcSubmitMatchRequest request,
        ServerCallContext context)
    {
        RequireAuth(context);

        if (!Guid.TryParse(request.EventId,  out var eventId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "event_id must be a valid UUID"));
        if (!Guid.TryParse(request.MatchId, out var matchId))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "match_id must be a valid UUID"));

        var participants = request.Participants
            .Select(p => new MatchParticipantResultDto(
                Guid.Parse(p.PlayerId),
                p.Score,
                p.Correct,
                p.Wrong,
                p.AvgAnswerTimeMs))
            .ToList();

        var dto = new SubmitMatchRequest(
            eventId,
            matchId,
            request.Mode,
            request.Category,
            request.QuestionCount,
            DateTimeOffset.FromUnixTimeMilliseconds(request.StartedAtUtc),
            DateTimeOffset.FromUnixTimeMilliseconds(request.EndedAtUtc),
            (MatchStatus)request.Status,
            participants);

        var result = await _mediator.Send(new SubmitMatch(dto), context.CancellationToken);

        var response = new GrpcSubmitMatchResponse
        {
            EventId = result.EventId.ToString(),
            MatchId = result.MatchId.ToString(),
            Status  = result.Status
        };

        foreach (var award in result.Awards)
        {
            response.Awards.Add(new MatchAward
            {
                PlayerId     = award.PlayerId.ToString(),
                AwardedXp    = award.AwardedXp,
                AwardedCoins = award.AwardedCoins
            });
        }

        return response;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // PlayMatch — bidirectional streaming
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task PlayMatch(
        IAsyncStreamReader<PlayerAction> requestStream,
        IServerStreamWriter<MatchEvent> responseStream,
        ServerCallContext context)
    {
        RequireAuth(context);

        string? matchId   = null;
        string? playerId  = null;
        MatchSession? session = null;

        try
        {
            await foreach (var action in requestStream.ReadAllAsync(context.CancellationToken))
            {
                switch (action.ActionCase)
                {
                    // ── Join ────────────────────────────────────────────────
                    case PlayerAction.ActionOneofCase.Join:
                        matchId  = action.Join.MatchId;
                        playerId = action.Join.PlayerId;

                        session = _sessions.GetOrAdd(matchId, _ => new MatchSession(matchId));
                        session.AddParticipant(playerId, responseStream);

                        _logger.LogInformation(
                            "gRPC PlayMatch: player {PlayerId} joined match {MatchId}",
                            playerId, matchId);

                        // Acknowledge join
                        await responseStream.WriteAsync(new MatchEvent
                        {
                            Timer = new TimerEvent { QuestionId = "", RemainingSeconds = 0 }
                        }, context.CancellationToken);
                        break;

                    // ── Answer ───────────────────────────────────────────────
                    case PlayerAction.ActionOneofCase.Answer when session is not null:
                        var ans = action.Answer;
                        _logger.LogDebug(
                            "gRPC answer: player={PlayerId} question={QuestionId} option={OptionId}",
                            playerId, ans.QuestionId, ans.SelectedOptionId);

                        // TODO: run answer evaluation through IMediator / match engine
                        // For now, echo back a placeholder result
                        var result = new AnswerResultEvent
                        {
                            QuestionId       = ans.QuestionId,
                            SelectedOptionId = ans.SelectedOptionId,
                            CorrectOptionId  = "",   // TODO: fetch from match engine
                            IsCorrect        = false, // TODO
                            PointsAwarded    = 0,     // TODO
                            RunningScore     = 0      // TODO
                        };
                        await responseStream.WriteAsync(
                            new MatchEvent { AnswerResult = result },
                            context.CancellationToken);

                        // Fan-out opponent score update to all other participants
                        if (session is not null && playerId is not null)
                        {
                            var opponentEvt = new MatchEvent
                            {
                                OpponentScore = new OpponentScoreEvent
                                {
                                    OpponentPlayerId = playerId,
                                    Score            = 0, // TODO: running score
                                    CorrectCount     = 0
                                }
                            };
                            await session.BroadcastExceptAsync(playerId, opponentEvt, context.CancellationToken);
                        }
                        break;

                    // ── Heartbeat ────────────────────────────────────────────
                    case PlayerAction.ActionOneofCase.Ping:
                        // No-op; keeps the stream alive
                        break;
                }
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
            if (matchId is not null && playerId is not null && session is not null)
            {
                session.RemoveParticipant(playerId);
                if (session.IsEmpty)
                    _sessions.TryRemove(matchId, out _);

                _logger.LogInformation(
                    "gRPC PlayMatch: player {PlayerId} left match {MatchId}",
                    playerId, matchId);
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // WatchLeaderboard — server streaming
    // ─────────────────────────────────────────────────────────────────────────

    public override async Task WatchLeaderboard(
        LeaderboardWatchRequest request,
        IServerStreamWriter<LeaderboardUpdate> responseStream,
        ServerCallContext context)
    {
        RequireAuth(context);

        _logger.LogInformation(
            "gRPC WatchLeaderboard: player={PlayerId} mode={Mode} window={Window}",
            request.PlayerId, request.Mode, request.WindowSize);

        var windowSize = request.WindowSize > 0 ? request.WindowSize : 5;

        // Push an initial snapshot immediately, then poll every 15 s.
        // TODO: replace polling with a Redis pub/sub subscription once
        // the leaderboard service exposes a change stream.
        while (!context.CancellationToken.IsCancellationRequested)
        {
            try
            {
                var update = await BuildLiveLeaderboardUpdateAsync(request.PlayerId, windowSize, context.CancellationToken);
                await responseStream.WriteAsync(update, context.CancellationToken);

                await Task.Delay(TimeSpan.FromSeconds(15), context.CancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation(
            "gRPC WatchLeaderboard: player={PlayerId} stream ended", request.PlayerId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void RequireAuth(ServerCallContext context)
    {
        var auth = context.RequestHeaders.GetValue("authorization");
        if (string.IsNullOrWhiteSpace(auth) || !auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Bearer token required"));
    }

    private async Task<LeaderboardUpdate> BuildLiveLeaderboardUpdateAsync(
        string playerId,
        int windowSize,
        CancellationToken ct)
    {
        if (!Guid.TryParse(playerId, out var playerGuid))
            throw new RpcException(new Status(StatusCode.InvalidArgument, "player_id must be a valid UUID"));

        var myTier = await _mediator.Send(new GetMyTier(playerGuid), ct);
        var tierId = myTier?.TierId ?? 1;

        var pageSize = Math.Clamp((windowSize * 2) + 1, 1, 100);
        var leaderboard = await _mediator.Send(new GetTierLeaderboard(tierId, 1, pageSize), ct);

        var update = new LeaderboardUpdate
        {
            PlayerId      = playerId,
            PlayerRank    = myTier?.TierRank ?? 0,
            PlayerScore   = myTier?.Score ?? 0,
            SnapshotAtMs  = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        foreach (var entry in leaderboard.Entries.Take(pageSize))
        {
            update.Nearby.Add(new LeaderboardEntry
            {
                Rank = entry.TierRank,
                PlayerId = entry.PlayerId.ToString(),
                Handle = entry.Username,
                Score = entry.Score,
                Country = entry.CountryCode
            });
        }

        return update;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// MatchSession — in-process fan-out registry for bidirectional match streams.
// Scoped to a single match; stored in MobileMatchGrpcService._sessions.
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class MatchSession(string matchId)
{
    private readonly ConcurrentDictionary<string, IServerStreamWriter<MatchEvent>> _writers = new();

    public string MatchId => matchId;
    public bool   IsEmpty => _writers.IsEmpty;

    public void AddParticipant(string playerId, IServerStreamWriter<MatchEvent> writer)
        => _writers[playerId] = writer;

    public void RemoveParticipant(string playerId)
        => _writers.TryRemove(playerId, out _);

    /// <summary>Sends <paramref name="evt"/> to every participant except <paramref name="excludePlayerId"/>.</summary>
    public async Task BroadcastExceptAsync(string excludePlayerId, MatchEvent evt, CancellationToken ct)
    {
        foreach (var (pid, writer) in _writers)
        {
            if (pid == excludePlayerId) continue;
            try { await writer.WriteAsync(evt, ct); }
            catch { /* participant disconnected; will clean up in its own finally block */ }
        }
    }
}
