// ─────────────────────────────────────────────────────────────────────────────
// Tycoon Mobile — Personalization providers and UI hooks
//
// This file provides Riverpod providers, Dart models, and example UI widgets
// for consuming the backend personalization API.  It is reference code — adapt
// patterns to your own app structure.
//
// Backend endpoints consumed (all require JWT bearer auth):
//   GET  /personalization/home/{playerId}
//   GET  /personalization/recommendations/{playerId}
//   GET  /coach/{playerId}/daily-brief
//   POST /personalization/recommendations/{recommendationId}/accept?playerId={id}
//   POST /personalization/recommendations/{recommendationId}/dismiss?playerId={id}
//
// ─────────────────────────────────────────────────────────────────────────────
// Required pubspec.yaml additions:
//   dependencies:
//     dio: ^5.4.0
//     flutter_riverpod: ^2.5.1
//     freezed_annotation: ^2.4.4
//
//   dev_dependencies:
//     freezed: ^2.5.2
//     json_serializable: ^6.8.0
//     build_runner: ^2.4.0
// ─────────────────────────────────────────────────────────────────────────────

import 'package:dio/dio.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';

// ─────────────────────────────────────────────────────────────────────────────
// 1. Dart model classes (mirrors of backend DTOs)
// ─────────────────────────────────────────────────────────────────────────────

/// Mirrors `PlayerRecommendationDto` from the backend.
class PlayerRecommendationDto {
  final String id;
  final String type;
  final String source;
  final int priority;
  final double score;
  final String reason;
  final Map<String, dynamic> payload;
  final Map<String, dynamic> guardrails;
  final DateTime? expiresAt;

  const PlayerRecommendationDto({
    required this.id,
    required this.type,
    required this.source,
    required this.priority,
    required this.score,
    required this.reason,
    required this.payload,
    required this.guardrails,
    this.expiresAt,
  });

  factory PlayerRecommendationDto.fromJson(Map<String, dynamic> json) =>
      PlayerRecommendationDto(
        id: json['id'] as String,
        type: json['type'] as String,
        source: json['source'] as String,
        priority: json['priority'] as int,
        score: (json['score'] as num).toDouble(),
        reason: json['reason'] as String,
        payload: Map<String, dynamic>.from(json['payload'] as Map),
        guardrails: Map<String, dynamic>.from(json['guardrails'] as Map),
        expiresAt: json['expiresAt'] != null
            ? DateTime.parse(json['expiresAt'] as String)
            : null,
      );
}

/// Mirrors `CoachBriefDto` from the backend.
class CoachBriefDto {
  final String title;
  final String message;
  final String recommendedAction;
  final String? targetRoute;
  final String tone;

  const CoachBriefDto({
    required this.title,
    required this.message,
    required this.recommendedAction,
    this.targetRoute,
    required this.tone,
  });

  factory CoachBriefDto.fromJson(Map<String, dynamic> json) => CoachBriefDto(
        title: json['title'] as String,
        message: json['message'] as String,
        recommendedAction: json['recommendedAction'] as String,
        targetRoute: json['targetRoute'] as String?,
        tone: json['tone'] as String,
      );
}

/// Mirrors `MissionRecommendationDto` from the backend.
class MissionRecommendationDto {
  final String missionArchetype;
  final String reason;
  final bool isLowPressure;

  const MissionRecommendationDto({
    required this.missionArchetype,
    required this.reason,
    required this.isLowPressure,
  });

  factory MissionRecommendationDto.fromJson(Map<String, dynamic> json) =>
      MissionRecommendationDto(
        missionArchetype: json['missionArchetype'] as String,
        reason: json['reason'] as String,
        isLowPressure: json['isLowPressure'] as bool,
      );
}

/// Mirrors `PlayerHomePersonalizationDto` from the backend.
class PlayerHomePersonalizationDto {
  final String playerId;
  final String recommendedMode;
  final String? recommendedCategory;
  final String? recommendedDifficulty;
  final List<PlayerRecommendationDto> recommendations;
  final CoachBriefDto? coachBrief;
  final Map<String, dynamic> guardrails;
  final List<MissionRecommendationDto> recommendedMissions;

  const PlayerHomePersonalizationDto({
    required this.playerId,
    required this.recommendedMode,
    this.recommendedCategory,
    this.recommendedDifficulty,
    required this.recommendations,
    this.coachBrief,
    required this.guardrails,
    required this.recommendedMissions,
  });

  factory PlayerHomePersonalizationDto.fromJson(Map<String, dynamic> json) =>
      PlayerHomePersonalizationDto(
        playerId: json['playerId'] as String,
        recommendedMode: json['recommendedMode'] as String,
        recommendedCategory: json['recommendedCategory'] as String?,
        recommendedDifficulty: json['recommendedDifficulty'] as String?,
        recommendations: (json['recommendations'] as List<dynamic>)
            .map((e) => PlayerRecommendationDto.fromJson(
                e as Map<String, dynamic>))
            .toList(),
        coachBrief: json['coachBrief'] != null
            ? CoachBriefDto.fromJson(
                json['coachBrief'] as Map<String, dynamic>)
            : null,
        guardrails: Map<String, dynamic>.from(json['guardrails'] as Map),
        recommendedMissions: (json['recommendedMissions'] as List<dynamic>)
            .map((e) => MissionRecommendationDto.fromJson(
                e as Map<String, dynamic>))
            .toList(),
      );
}

// ─────────────────────────────────────────────────────────────────────────────
// 2. Personalization service (API client layer)
// ─────────────────────────────────────────────────────────────────────────────

/// Low-level service that wraps all personalization API calls.
/// Inject [Dio] with auth interceptors already configured (see
/// ApiClient in FLUTTER_INTEGRATION.md).
class PersonalizationService {
  const PersonalizationService(this._dio);

  final Dio _dio;

  /// Fetches the full home personalization bundle for [playerId].
  /// Returns [PlayerHomePersonalizationDto] on success.
  Future<PlayerHomePersonalizationDto> getHome(String playerId) async {
    final response =
        await _dio.get('/personalization/home/$playerId');
    return PlayerHomePersonalizationDto.fromJson(
        response.data as Map<String, dynamic>);
  }

  /// Fetches only the recommendations list for [playerId].
  Future<List<PlayerRecommendationDto>> getRecommendations(
      String playerId) async {
    final response =
        await _dio.get('/personalization/recommendations/$playerId');
    return (response.data as List<dynamic>)
        .map((e) =>
            PlayerRecommendationDto.fromJson(e as Map<String, dynamic>))
        .toList();
  }

  /// Fetches the coach daily brief for [playerId].
  Future<CoachBriefDto> getDailyBrief(String playerId) async {
    final response =
        await _dio.get('/coach/$playerId/daily-brief');
    return CoachBriefDto.fromJson(response.data as Map<String, dynamic>);
  }

  /// Marks [recommendationId] as accepted on behalf of [playerId].
  /// The backend records the accept event for learning; returns 204.
  Future<void> acceptRecommendation(
      String recommendationId, String playerId) async {
    await _dio.post(
      '/personalization/recommendations/$recommendationId/accept',
      queryParameters: {'playerId': playerId},
    );
  }

  /// Marks [recommendationId] as dismissed on behalf of [playerId].
  /// The backend records the dismiss event for learning; returns 204.
  Future<void> dismissRecommendation(
      String recommendationId, String playerId) async {
    await _dio.post(
      '/personalization/recommendations/$recommendationId/dismiss',
      queryParameters: {'playerId': playerId},
    );
  }
}

// ─────────────────────────────────────────────────────────────────────────────
// 3. Riverpod providers
// ─────────────────────────────────────────────────────────────────────────────

/// Expose a [PersonalizationService] instance backed by the configured [Dio].
///
/// Override [dioProvider] in your dependency-injection setup to supply a
/// [Dio] instance that already has the auth interceptor attached.
final dioProvider = Provider<Dio>((ref) => Dio());

final personalizationServiceProvider =
    Provider<PersonalizationService>((ref) {
  return PersonalizationService(ref.watch(dioProvider));
});

/// Loads the full home personalization bundle for [playerId].
/// Cached per unique player ID via [family].
///
/// Usage:
/// ```dart
/// final home = ref.watch(homePersonalizationProvider(playerId));
/// ```
final homePersonalizationProvider = FutureProvider.family<
    PlayerHomePersonalizationDto, String>((ref, playerId) async {
  final service = ref.watch(personalizationServiceProvider);
  return service.getHome(playerId);
});

/// Loads the recommendations list for [playerId].
/// Use [ref.invalidate] to force a refresh after an accept/dismiss action.
final recommendationsProvider = FutureProvider.family<
    List<PlayerRecommendationDto>, String>((ref, playerId) async {
  final service = ref.watch(personalizationServiceProvider);
  return service.getRecommendations(playerId);
});

/// Loads the coach daily brief for [playerId].
final coachDailyBriefProvider =
    FutureProvider.family<CoachBriefDto, String>((ref, playerId) async {
  final service = ref.watch(personalizationServiceProvider);
  return service.getDailyBrief(playerId);
});

// ─────────────────────────────────────────────────────────────────────────────
// 4. Recommendation actions notifier
// ─────────────────────────────────────────────────────────────────────────────

/// Manages accept/dismiss actions for a specific player's recommendations.
/// After a successful action the recommendations list is invalidated so the
/// UI refreshes automatically.
class RecommendationActionsNotifier extends AsyncNotifier<void> {
  @override
  Future<void> build() async {}

  Future<void> accept(String recommendationId, String playerId) async {
    final service = ref.read(personalizationServiceProvider);
    await service.acceptRecommendation(recommendationId, playerId);
    ref.invalidate(recommendationsProvider(playerId));
    ref.invalidate(homePersonalizationProvider(playerId));
  }

  Future<void> dismiss(String recommendationId, String playerId) async {
    final service = ref.read(personalizationServiceProvider);
    await service.dismissRecommendation(recommendationId, playerId);
    ref.invalidate(recommendationsProvider(playerId));
    ref.invalidate(homePersonalizationProvider(playerId));
  }
}

final recommendationActionsProvider =
    AsyncNotifierProvider<RecommendationActionsNotifier, void>(
        RecommendationActionsNotifier.new);

// ─────────────────────────────────────────────────────────────────────────────
// 5. Example UI widgets
// ─────────────────────────────────────────────────────────────────────────────

/// Renders the coach daily brief as a dismissible card.
///
/// Example:
/// ```dart
/// CoachBriefCard(playerId: currentPlayerId)
/// ```
class CoachBriefCard extends ConsumerWidget {
  const CoachBriefCard({super.key, required this.playerId});

  final String playerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final briefAsync = ref.watch(coachDailyBriefProvider(playerId));

    return briefAsync.when(
      loading: () => const Card(
        child: ListTile(
          leading: CircularProgressIndicator(),
          title: Text('Loading coach brief…'),
        ),
      ),
      error: (e, _) => const SizedBox.shrink(), // silently suppress
      data: (brief) => Card(
        color: _toneColor(brief.tone, context),
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  const Icon(Icons.smart_toy_outlined),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      brief.title,
                      style: Theme.of(context).textTheme.titleMedium,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Text(brief.message),
              const SizedBox(height: 12),
              ElevatedButton(
                onPressed: brief.targetRoute != null
                    ? () => Navigator.of(context)
                        .pushNamed(brief.targetRoute!)
                    : null,
                child: Text(brief.recommendedAction),
              ),
            ],
          ),
        ),
      ),
    );
  }

  /// Maps the backend `tone` value to a subtle tint color.
  Color _toneColor(String tone, BuildContext context) {
    switch (tone.toLowerCase()) {
      case 'encouraging':
        return Colors.green.shade50;
      case 'challenge':
        return Colors.orange.shade50;
      case 'calm':
        return Colors.blue.shade50;
      default:
        return Theme.of(context).cardColor;
    }
  }
}

/// Renders the list of personalized recommendations with accept/dismiss
/// buttons.  Frontend does **not** implement ToM logic — it only renders
/// what the backend returns.
///
/// Example:
/// ```dart
/// RecommendationsList(playerId: currentPlayerId)
/// ```
class RecommendationsList extends ConsumerWidget {
  const RecommendationsList({super.key, required this.playerId});

  final String playerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final recsAsync = ref.watch(recommendationsProvider(playerId));

    return recsAsync.when(
      loading: () => const Center(child: CircularProgressIndicator()),
      error: (e, _) => Center(child: Text('Could not load suggestions: $e')),
      data: (recs) {
        if (recs.isEmpty) {
          return const Center(
            child: Text('No suggestions right now — check back later!'),
          );
        }
        return ListView.separated(
          itemCount: recs.length,
          separatorBuilder: (_, __) => const Divider(height: 1),
          itemBuilder: (context, i) =>
              _RecommendationTile(rec: recs[i], playerId: playerId),
        );
      },
    );
  }
}

class _RecommendationTile extends ConsumerWidget {
  const _RecommendationTile({
    required this.rec,
    required this.playerId,
  });

  final PlayerRecommendationDto rec;
  final String playerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final actionsAsync = ref.watch(recommendationActionsProvider);
    final isLoading = actionsAsync.isLoading;

    return ListTile(
      leading: _typeIcon(rec.type),
      title: Text(rec.reason),
      subtitle: rec.payload['label'] != null
          ? Text(rec.payload['label'].toString())
          : null,
      trailing: isLoading
          ? const SizedBox(
              width: 24, height: 24, child: CircularProgressIndicator())
          : Row(
              mainAxisSize: MainAxisSize.min,
              children: [
                // ── Accept ────────────────────────────────────────────────
                IconButton(
                  tooltip: 'Accept suggestion',
                  icon: const Icon(Icons.check_circle_outline),
                  color: Colors.green,
                  onPressed: () => ref
                      .read(recommendationActionsProvider.notifier)
                      .accept(rec.id, playerId),
                ),
                // ── Dismiss ───────────────────────────────────────────────
                IconButton(
                  tooltip: 'Dismiss suggestion',
                  icon: const Icon(Icons.cancel_outlined),
                  color: Colors.grey,
                  onPressed: () => ref
                      .read(recommendationActionsProvider.notifier)
                      .dismiss(rec.id, playerId),
                ),
              ],
            ),
    );
  }

  Widget _typeIcon(String type) {
    switch (type.toLowerCase()) {
      case 'mode_suggestion':
        return const Icon(Icons.gamepad_outlined);
      case 'category_focus':
        return const Icon(Icons.category_outlined);
      case 'difficulty_adjust':
        return const Icon(Icons.tune_outlined);
      case 'store_offer':
        return const Icon(Icons.store_outlined);
      default:
        return const Icon(Icons.lightbulb_outline);
    }
  }
}

/// Full personalization home screen that combines coach brief, recommendations,
/// and mission suggestions into a single scrollable view.
///
/// Wrap with [ProviderScope] at app root; pass the authenticated player's UUID.
///
/// Example:
/// ```dart
/// PersonalizationHomeScreen(playerId: currentPlayerId)
/// ```
class PersonalizationHomeScreen extends ConsumerWidget {
  const PersonalizationHomeScreen({super.key, required this.playerId});

  final String playerId;

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final homeAsync = ref.watch(homePersonalizationProvider(playerId));

    return Scaffold(
      appBar: AppBar(title: const Text('For You')),
      body: homeAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (e, _) => Center(child: Text('Error loading content: $e')),
        data: (home) => RefreshIndicator(
          onRefresh: () async {
            ref.invalidate(homePersonalizationProvider(playerId));
          },
          child: ListView(
            padding: const EdgeInsets.all(16),
            children: [
              // ── Coach brief ──────────────────────────────────────────────
              if (home.coachBrief != null) ...[
                Text('Your Coach',
                    style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                CoachBriefCard(playerId: playerId),
                const SizedBox(height: 24),
              ],

              // ── Suggested actions (recommendations) ──────────────────────
              if (home.recommendations.isNotEmpty) ...[
                Text('Suggested Actions',
                    style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                ...home.recommendations.map(
                  (rec) => _RecommendationTile(rec: rec, playerId: playerId),
                ),
                const SizedBox(height: 24),
              ],

              // ── Recommended missions ──────────────────────────────────────
              if (home.recommendedMissions.isNotEmpty) ...[
                Text('Recommended Missions',
                    style: Theme.of(context).textTheme.titleLarge),
                const SizedBox(height: 8),
                ...home.recommendedMissions.map(
                  (m) => ListTile(
                    leading: m.isLowPressure
                        ? const Icon(Icons.spa_outlined)
                        : const Icon(Icons.emoji_events_outlined),
                    title: Text(m.missionArchetype),
                    subtitle: Text(m.reason),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
