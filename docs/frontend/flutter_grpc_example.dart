// ─────────────────────────────────────────────────────────────────────────────
// Synaptix Mobile — Flutter gRPC client example
//
// This file shows how to call the MobileMatchService gRPC endpoints from a
// Flutter app.  It is NOT production code — adapt patterns to your app's
// state management (Riverpod / Bloc / etc.).
//
// Wire package (Wave 4): synaptix.mobile
//   Full method paths look like:
//     /synaptix.mobile.MobileMatchService/StartMatch
//   Clients generated against package tycoon.mobile must be regenerated.
//
// Setup (pubspec.yaml):
//   dependencies:
//     grpc: ^3.2.4
//     protobuf: ^3.1.0
//     fixnum: ^1.1.0          # required by protobuf
//
// Code generation:
//   Install protoc + dart plugin (protoc-gen-dart), then run from repo root:
//     protoc --dart_out=grpc:lib/src/grpc/generated \
//            --proto_path=protos \
//            protos/mobile.proto
//   This generates:
//     lib/src/grpc/generated/mobile.pb.dart
//     lib/src/grpc/generated/mobile.pbgrpc.dart
//     lib/src/grpc/generated/mobile.pbjson.dart
//
// Dart protoc plugin: https://pub.dev/packages/protoc_plugin
// ─────────────────────────────────────────────────────────────────────────────

import 'dart:async';

import 'package:grpc/grpc.dart';
import 'package:fixnum/fixnum.dart';

// Generated from protos/mobile.proto (package synaptix.mobile)
// ignore: uri_does_not_exist
import 'package:synaptix_app/src/grpc/generated/mobile.pb.dart';
// ignore: uri_does_not_exist
import 'package:synaptix_app/src/grpc/generated/mobile.pbgrpc.dart';

// ─────────────────────────────────────────────────────────────────────────────
// SynaptixGrpcClient — singleton wrapper around the gRPC channel + stub
// ─────────────────────────────────────────────────────────────────────────────

class SynaptixGrpcClient {
  static SynaptixGrpcClient? _instance;

  final ClientChannel _channel;
  final MobileMatchServiceClient _stub;
  final String _bearerToken;

  SynaptixGrpcClient._({
    required ClientChannel channel,
    required MobileMatchServiceClient stub,
    required String bearerToken,
  })  : _channel = channel,
        _stub = stub,
        _bearerToken = bearerToken;

  /// Create (or replace) the singleton.
  ///
  /// Call after the user logs in and you have a JWT:
  ///   SynaptixGrpcClient.init(host: 'api.synaptix.app', port: 5001, token: jwt);
  factory SynaptixGrpcClient.init({
    required String host,
    required int port,
    required String bearerToken,
    bool useTls = true,
  }) {
    final channel = ClientChannel(
      host,
      port: port,
      options: ChannelOptions(
        credentials: useTls
            ? const ChannelCredentials.secure()
            : const ChannelCredentials.insecure(),
        // Keep the channel alive between matches
        keepAlive: const ClientKeepAliveOptions(
          pingInterval: Duration(seconds: 30),
          timeout: Duration(seconds: 10),
          permitWithoutCalls: true,
        ),
      ),
    );

    final stub = MobileMatchServiceClient(channel);
    _instance = SynaptixGrpcClient._(
      channel: channel,
      stub: stub,
      bearerToken: bearerToken,
    );
    return _instance!;
  }

  static SynaptixGrpcClient get instance {
    assert(_instance != null, 'Call SynaptixGrpcClient.init() first');
    return _instance!;
  }

  /// Auth metadata sent with every call.
  CallOptions get _auth => CallOptions(
    metadata: {'authorization': 'Bearer $_bearerToken'},
  );

  Future<void> shutdown() => _channel.shutdown();

  // ─────────────────────────────────────────────────────────────────────────
  // StartMatch
  // ─────────────────────────────────────────────────────────────────────────

  Future<GrpcStartMatchResponse> startMatch({
    required String hostPlayerId,
    required String mode,
  }) async {
    final request = GrpcStartMatchRequest()
      ..hostPlayerId = hostPlayerId
      ..mode = mode;

    return _stub.startMatch(request, options: _auth);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // SubmitMatch
  // ─────────────────────────────────────────────────────────────────────────

  Future<GrpcSubmitMatchResponse> submitMatch({
    required String eventId,
    required String matchId,
    required String mode,
    required String category,
    required int questionCount,
    required DateTime startedAtUtc,
    required DateTime endedAtUtc,
    required int status, // 1 = Completed, 2 = Aborted
    required List<ParticipantResult> participants,
  }) async {
    final request = GrpcSubmitMatchRequest()
      ..eventId = eventId
      ..matchId = matchId
      ..mode = mode
      ..category = category
      ..questionCount = questionCount
      ..startedAtUtc = Int64(startedAtUtc.millisecondsSinceEpoch)
      ..endedAtUtc = Int64(endedAtUtc.millisecondsSinceEpoch)
      ..status = status
      ..participants.addAll(participants);

    return _stub.submitMatch(request, options: _auth);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // PlayMatch — bidirectional streaming
  // ─────────────────────────────────────────────────────────────────────────

  /// Opens a bidirectional gRPC stream for a live match session.
  ///
  /// Returns a [MatchStreamSession] that lets you send player actions and
  /// receive match events as a Dart Stream.
  ///
  /// Example:
  ///   final session = await client.playMatch();
  ///   session.join(matchId: matchId, playerId: playerId);
  ///
  ///   session.events.listen((event) {
  ///     if (event.hasQuestion())   _showQuestion(event.question);
  ///     if (event.hasMatchEnd())   _showResults(event.matchEnd);
  ///     if (event.hasAnswerResult()) _highlightAnswer(event.answerResult);
  ///   });
  ///
  ///   // When player selects an answer:
  ///   session.submitAnswer(questionId: q.questionId, optionId: selected);
  ///
  ///   // On disconnect / match end:
  ///   session.close();
  MatchStreamSession playMatch() {
    final responseStream = _stub.playMatch(options: _auth);
    return MatchStreamSession(responseStream);
  }

  // ─────────────────────────────────────────────────────────────────────────
  // WatchLeaderboard — server streaming
  // ─────────────────────────────────────────────────────────────────────────

  /// Subscribe to live leaderboard updates.
  ///
  /// The server pushes a [LeaderboardUpdate] every ~15 s (and immediately
  /// on first connection).  Cancel the subscription to stop receiving.
  ///
  /// Example:
  ///   final sub = client
  ///       .watchLeaderboard(playerId: userId, mode: 'ranked', windowSize: 5)
  ///       .listen((update) => setState(() => _leaderboard = update));
  ///
  ///   // On dispose:
  ///   sub.cancel();
  Stream<LeaderboardUpdate> watchLeaderboard({
    required String playerId,
    String mode = '',
    int windowSize = 5,
  }) {
    final request = LeaderboardWatchRequest()
      ..playerId = playerId
      ..mode = mode
      ..windowSize = windowSize;

    return _stub.watchLeaderboard(request, options: _auth);
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// MatchStreamSession — wraps the bidirectional PlayMatch stream
// ─────────────────────────────────────────────────────────────────────────────

class MatchStreamSession {
  final ResponseStream<MatchEvent> _responseStream;
  final StreamController<PlayerAction> _controller = StreamController();

  MatchStreamSession(this._responseStream);

  /// Stream of MatchEvent messages pushed by the server.
  Stream<MatchEvent> get events => _responseStream;

  void _send(PlayerAction action) => _controller.add(action);

  void join({required String matchId, required String playerId}) {
    _send(PlayerAction()
      ..join = (JoinMatchAction()
        ..matchId = matchId
        ..playerId = playerId));
  }

  void submitAnswer({
    required String matchId,
    required String questionId,
    required String optionId,
  }) {
    _send(PlayerAction()
      ..answer = (SubmitAnswerAction()
        ..matchId = matchId
        ..questionId = questionId
        ..selectedOptionId = optionId
        ..answeredAtMs = Int64(DateTime.now().millisecondsSinceEpoch)));
  }

  void sendHeartbeat() {
    _send(PlayerAction()
      ..ping = (HeartbeatAction()
        ..clientTimestampMs = Int64(DateTime.now().millisecondsSinceEpoch)));
  }

  Future<void> close() async {
    await _controller.close();
    await _responseStream.cancel();
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// Usage example (inside a StatefulWidget or Riverpod notifier)
// ─────────────────────────────────────────────────────────────────────────────

Future<void> exampleUsage() async {
  // 1. Initialize after login (do this once, store the client)
  final client = SynaptixGrpcClient.init(
    host: 'api.synaptix.app',
    port: 5001,
    bearerToken: '<jwt-from-login>',
    useTls: true,
  );

  // 2. Start a match
  final startResp = await client.startMatch(
    hostPlayerId: 'player-uuid',
    mode: 'ranked',
  );
  print('Match started: ${startResp.matchId}');

  // 3. Open the live match stream
  final session = client.playMatch();
  session.join(matchId: startResp.matchId, playerId: 'player-uuid');

  final sub = session.events.listen((event) {
    if (event.hasQuestion()) {
      final q = event.question;
      print('Question: ${q.text}');
      // Show question UI, start countdown timer (q.timeLimitS seconds)
    }
    if (event.hasAnswerResult()) {
      final r = event.answerResult;
      print('Correct: ${r.isCorrect}  Points: ${r.pointsAwarded}  Score: ${r.runningScore}');
    }
    if (event.hasOpponentScore()) {
      final o = event.opponentScore;
      print('Opponent ${o.opponentPlayerId} score: ${o.score}');
    }
    if (event.hasMatchEnd()) {
      final m = event.matchEnd;
      print('Match ended: ${m.outcome}  XP: ${m.awardedXp}  Coins: ${m.awardedCoins}');
    }
  });

  // 4. Player selects an answer
  await Future.delayed(const Duration(seconds: 3));
  session.submitAnswer(
    matchId: startResp.matchId,
    questionId: 'question-uuid',
    optionId: 'option-b',
  );

  // 5. Clean up after match ends
  await sub.cancel();
  await session.close();

  // 6. Submit completed match for XP / coin awards
  await client.submitMatch(
    eventId: 'event-idempotency-uuid',
    matchId: startResp.matchId,
    mode: 'ranked',
    category: 'sports',
    questionCount: 10,
    startedAtUtc: DateTime.now().subtract(const Duration(minutes: 5)),
    endedAtUtc: DateTime.now(),
    status: 1, // Completed
    participants: [
      ParticipantResult()
        ..playerId = 'player-uuid'
        ..score = 850
        ..correct = 8
        ..wrong = 2
        ..avgAnswerTimeMs = 3200.0,
    ],
  );

  // 7. Watch leaderboard in a separate screen
  final leaderboardSub = client
      .watchLeaderboard(playerId: 'player-uuid', mode: 'ranked', windowSize: 5)
      .listen((update) {
    print('My rank: ${update.playerRank}  Score: ${update.playerScore}');
    for (final entry in update.nearby) {
      print('  #${entry.rank}  ${entry.handle}  ${entry.score}');
    }
  });

  // Cancel when leaving the leaderboard screen
  await leaderboardSub.cancel();
}
