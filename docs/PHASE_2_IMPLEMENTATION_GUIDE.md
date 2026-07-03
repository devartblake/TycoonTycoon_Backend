# Phase 2 Implementation Guide — Post-Launch Enhancements

**Timeline:** 2-3 weeks post-launch (Week 2-3)  
**Target Completion:** 2026-07-15  
**Status:** 🟡 Planning Phase

---

## Overview

Three enhancements scheduled for Phase 2 (2-3 weeks after v4.0.0 launch):

| Enhancement | Effort | Business Value | Priority |
|---|---|---|---|
| Learning Hub Integration | 4-6 hours | High | 1 |
| Seasonal Leaderboards | 6-8 hours | Medium | 2 |
| Performance Caching | 4-6 hours | Medium | 3 |

**Total Effort:** 14-20 hours (1.5-2.5 days work)

---

## Enhancement #1: Learning Hub Integration

**Objective:** Link quiz questions answered incorrectly to learning resources (lessons, tutorials)

### Business Value
- **Expected ROI:** 20%+ lesson completion increase from quiz reviews
- **Engagement:** Players discover learning hub through incorrect answers
- **Retention:** Educational content keeps players engaged longer

### Technical Approach

#### Database Changes (Backend)

Create question-to-lesson mapping:
```sql
CREATE TABLE question_lesson_mappings (
  id UUID PRIMARY KEY,
  question_id UUID NOT NULL,
  lesson_id UUID NOT NULL,
  topic VARCHAR(100),
  difficulty_level INT,
  created_at TIMESTAMPTZ DEFAULT NOW(),
  UNIQUE(question_id, lesson_id),
  FOREIGN KEY (question_id) REFERENCES questions(id),
  FOREIGN KEY (lesson_id) REFERENCES lessons(id)
);

CREATE INDEX ix_qm_question ON question_lesson_mappings(question_id);
CREATE INDEX ix_qm_lesson ON question_lesson_mappings(lesson_id);
```

#### API Endpoints (Backend)

```csharp
// New endpoint to get lessons for a question
GET /api/v1/questions/{questionId}/lessons
Response: { lessons: LessonDto[] }

// Endpoint to track "learn more" clicks
POST /api/v1/quiz-review/learn-more-click
Request: { questionId, lessonId, context: "quiz-review" }
Response: { success: bool }

// Get recommended lessons based on quiz performance
GET /api/v1/quiz/recommended-lessons
Query: ?category={category}&difficulty={difficulty}
Response: { recommendedLessons: LessonDto[] }
```

#### Flutter Implementation

**1. Update QuizReviewScreen to show "Learn More" button:**

```dart
// lib/arcade/ui/screens/quiz_review_screen.dart
class QuizReviewScreen extends StatefulWidget {
  final List<AnsweredQuestionRecord> records;

  @override
  State<QuizReviewScreen> createState() => _QuizReviewScreenState();
}

class _QuizReviewScreenState extends State<QuizReviewScreen> {
  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('Review Your Answers')),
      body: ListView.builder(
        itemCount: records.length,
        itemBuilder: (context, index) {
          final record = records[index];
          return ExpansionTile(
            title: Text('Question ${index + 1}'),
            children: [
              // Existing question review content
              if (!record.isCorrect)
                Padding(
                  padding: EdgeInsets.all(16),
                  child: OutlinedButton.icon(
                    onPressed: () => _navigateToLesson(record.questionId),
                    icon: Icon(Icons.school),
                    label: Text('Learn More'),
                  ),
                ),
            ],
          );
        },
      ),
    );
  }

  void _navigateToLesson(String questionId) async {
    final lessonsProvider = ref.read(
      questionsLessonsProvider(questionId)
    );
    // Track click event
    await ref.read(quizReviewAnalyticsProvider).trackLearnMoreClick(questionId);
    // Navigate to learning hub or lesson details
    context.go('/learning-hub/lessons?fromQuestion=$questionId');
  }
}
```

**2. Create Riverpod provider for question-lesson mapping:**

```dart
// lib/game/providers/learning_providers.dart
final questionsLessonsProvider = FutureProvider.autoDispose.family<
  List<LessonDto>,
  String
>((ref, questionId) async {
  final client = ref.watch(synaptixApiClientProvider);
  final response = await client.get(
    '/questions/$questionId/lessons',
  );
  return (response['lessons'] as List)
    .map((json) => LessonDto.fromJson(json))
    .toList();
});

final quizReviewAnalyticsProvider = Provider<QuizReviewAnalyticsService>(
  (ref) => QuizReviewAnalyticsService(
    ref.watch(apiServiceProvider),
  ),
);
```

**3. Create analytics service:**

```dart
// lib/core/services/quiz_review_analytics_service.dart
class QuizReviewAnalyticsService {
  final ApiService _apiService;

  QuizReviewAnalyticsService(this._apiService);

  Future<void> trackLearnMoreClick(String questionId) async {
    await _apiService.post(
      '/quiz-review/learn-more-click',
      data: {
        'questionId': questionId,
        'context': 'quiz-review',
      },
    );
  }
}
```

#### Implementation Checklist

**Backend (2-3 hours):**
- [ ] Create question_lesson_mappings table
- [ ] Create migration file
- [ ] Add API endpoints (3 new endpoints)
- [ ] Add analytics tracking
- [ ] Seed question-lesson mapping data (coordinate with content team)
- [ ] Add tests (10+ test cases)
- [ ] Documentation

**Frontend (1-2 hours):**
- [ ] Update QuizReviewScreen with "Learn More" button
- [ ] Create questionsLessonsProvider
- [ ] Create QuizReviewAnalyticsService
- [ ] Update navigation to learning hub
- [ ] Add loading/error states
- [ ] Test on all platforms

---

## Enhancement #2: Seasonal Leaderboards

**Objective:** Filter leaderboards by time period (Weekly, Monthly, All-Time)

### Business Value
- **Expected ROI:** 15%+ engagement increase
- **Engagement:** Weekly resets keep leaderboard fresh
- **Retention:** Recurring reset cycles encourage return visits

### Technical Approach

#### Database Changes (Backend)

Add season tracking to arcade scores:

```sql
-- Add columns to arcade_scores table
ALTER TABLE arcade_scores ADD COLUMN season_id UUID;
ALTER TABLE arcade_scores ADD COLUMN season_type VARCHAR(20) DEFAULT 'all-time';
-- season_type: 'weekly', 'monthly', 'all-time'

-- Create seasons table
CREATE TABLE arcade_seasons (
  id UUID PRIMARY KEY,
  type VARCHAR(20) NOT NULL, -- 'weekly', 'monthly'
  start_date TIMESTAMPTZ NOT NULL,
  end_date TIMESTAMPTZ NOT NULL,
  status VARCHAR(20) DEFAULT 'active', -- 'active', 'archived'
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX ix_arcade_scores_season ON arcade_scores(season_id);
CREATE INDEX ix_seasons_type_dates ON arcade_seasons(type, start_date, end_date);
```

#### API Endpoints (Backend)

```csharp
// Get leaderboard for specific season
GET /api/v1/leaderboards/arcade/{gameId}/{difficulty}?seasonType={weekly|monthly|all-time}&page={page}
Response: {
  gameId, difficulty, seasonType, seasonPeriod,
  items: [{ rank, playerId, username, score, durationMs }]
}

// Get available seasons
GET /api/v1/leaderboards/arcade/seasons
Response: {
  weekly: { current, previous: [] },
  monthly: { current, previous: [] },
  allTime: { }
}

// Get player's seasonal statistics
GET /api/v1/leaderboards/arcade/player-stats
Response: {
  weekly: { rank, score, percentile },
  monthly: { rank, score, percentile },
  allTime: { rank, score, percentile }
}
```

#### Frontend Implementation

**1. Update leaderboard screen for season selection:**

```dart
// lib/arcade/leaderboards/arcade_global_leaderboard_view.dart
class ArcadeGlobalLeaderboardView extends ConsumerWidget {
  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final selectedSeason = ref.watch(selectedSeasonProvider);
    
    return Column(
      children: [
        // Season selector (segmented button)
        SegmentedButton<SeasonType>(
          segments: [
            ButtonSegment(
              value: SeasonType.weekly,
              label: Text('This Week'),
            ),
            ButtonSegment(
              value: SeasonType.monthly,
              label: Text('This Month'),
            ),
            ButtonSegment(
              value: SeasonType.allTime,
              label: Text('All Time'),
            ),
          ],
          selected: {selectedSeason},
          onSelectionChanged: (selected) {
            ref.read(selectedSeasonProvider.notifier).state = selected.first;
          },
        ),
        SizedBox(height: 16),
        
        // Leaderboard list (already implemented, reuse)
        Expanded(
          child: LeaderboardListView(seasonType: selectedSeason),
        ),
      ],
    );
  }
}
```

**2. Create season provider:**

```dart
// lib/game/providers/leaderboard_providers.dart
enum SeasonType { weekly, monthly, allTime }

final selectedSeasonProvider = StateProvider<SeasonType>(
  (ref) => SeasonType.allTime,
);

final leaderboardWithSeasonProvider = FutureProvider.autoDispose.family<
  ArcadeLeaderboardPage,
  ({String gameId, String difficulty, SeasonType season, int page})
>((ref, params) async {
  final client = ref.watch(synaptixApiClientProvider);
  final seasonParam = params.season == SeasonType.weekly
    ? 'weekly'
    : params.season == SeasonType.monthly
    ? 'monthly'
    : 'all-time';
  
  final response = await client.get(
    '/leaderboards/arcade/${params.gameId}/${params.difficulty}',
    queryParameters: {
      'seasonType': seasonParam,
      'page': params.page.toString(),
    },
  );
  return ArcadeLeaderboardPage.fromJson(response);
});
```

#### Implementation Checklist

**Backend (3-4 hours):**
- [ ] Add season_id and season_type columns to arcade_scores
- [ ] Create arcade_seasons table
- [ ] Create migration file
- [ ] Update SubmitArcadeScore handler to assign season
- [ ] Update GetArcadeLeaderboard handler to filter by season
- [ ] Add season availability endpoints
- [ ] Add background job to archive old seasons (cleanup)
- [ ] Add tests (15+ test cases)
- [ ] Documentation

**Frontend (2-3 hours):**
- [ ] Add season selector UI (segmented button)
- [ ] Create selectedSeasonProvider
- [ ] Create leaderboardWithSeasonProvider
- [ ] Update LeaderboardListView to use season parameter
- [ ] Add loading/error states
- [ ] Test on all platforms

---

## Enhancement #3: Performance Caching

**Objective:** Cache top 100 leaderboard scores in-memory for faster load times

### Business Value
- **Expected ROI:** 40% database load reduction
- **Performance:** 10x latency improvement (200ms → 20ms)
- **Scalability:** Supports more concurrent users

### Technical Approach

#### Backend Caching Strategy

```csharp
// lib/core/services/arcade/arcade_leaderboard_cache_service.dart
public interface ILeaderboardCacheService
{
    Task<IEnumerable<ArcadeLeaderboardEntryDto>> GetTopScores(
        string gameId, string difficulty, int top = 100);
    Task InvalidateCache(string gameId, string difficulty);
    Task<CacheStatistics> GetStatistics();
}

public class InMemoryLeaderboardCacheService : ILeaderboardCacheService
{
    private readonly IMemoryCache _cache;
    private readonly ILogger<InMemoryLeaderboardCacheService> _logger;
    private readonly ConcurrentDictionary<string, CacheMetadata> _metadata;

    public async Task<IEnumerable<ArcadeLeaderboardEntryDto>> GetTopScores(
        string gameId, string difficulty, int top = 100)
    {
        var cacheKey = $"leaderboard:{gameId}:{difficulty}:top{top}";
        
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            _metadata[cacheKey].HitCount++;
            _logger.LogDebug("Cache HIT: {CacheKey}", cacheKey);
            return (IEnumerable<ArcadeLeaderboardEntryDto>)cached;
        }

        // Cache miss: fetch from database
        var scores = await _repository.GetTopScoresAsync(gameId, difficulty, top);
        
        // Cache for 5 minutes
        var options = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromMinutes(5))
            .SetSlidingExpiration(TimeSpan.FromMinutes(1));
        
        _cache.Set(cacheKey, scores, options);
        _metadata[cacheKey] = new CacheMetadata { 
            CreatedAt = DateTime.UtcNow,
            MissCount = 1 
        };
        
        _logger.LogDebug("Cache MISS: {CacheKey} (fetched {Count} items)", 
            cacheKey, scores.Count());
        
        return scores;
    }

    public async Task InvalidateCache(string gameId, string difficulty)
    {
        // Invalidate on new score submission
        var cacheKey = $"leaderboard:{gameId}:{difficulty}:*";
        // Pattern removal (if using distributed cache)
        _logger.LogInformation("Cache invalidated: {CacheKey}", cacheKey);
    }

    public Task<CacheStatistics> GetStatistics()
    {
        var stats = _metadata.Values.Aggregate(
            new CacheStatistics(),
            (acc, meta) => new CacheStatistics
            {
                TotalCacheEntries = _metadata.Count,
                TotalHits = acc.TotalHits + meta.HitCount,
                TotalMisses = acc.TotalMisses + meta.MissCount,
                HitRate = (meta.HitCount / (meta.HitCount + meta.MissCount) * 100),
            }
        );
        return Task.FromResult(stats);
    }
}
```

#### API Changes

```csharp
// Update GetArcadeLeaderboard handler to use cache
public class GetArcadeLeaderboardHandler : IRequestHandler<GetArcadeLeaderboard, Result>
{
    private readonly ILeaderboardCacheService _cache;
    private readonly ILeaderboardRepository _repository;

    public async Task<Result> Handle(GetArcadeLeaderboard request, CancellationToken ct)
    {
        // Try cache first (top 100)
        if (request.PageSize >= 100 && request.Page == 1)
        {
            var cachedScores = await _cache.GetTopScores(
                request.GameId, request.Difficulty, 100);
            return Result.Success(cachedScores);
        }

        // Fallback to database for deep pagination
        var scores = await _repository.GetLeaderboardAsync(
            request.GameId, request.Difficulty, request.Page, request.PageSize);
        return Result.Success(scores);
    }
}

// New endpoint for cache statistics (admin only)
GET /api/v1/admin/leaderboard-cache/stats
Response: {
    totalCacheEntries: int,
    totalHits: int,
    totalMisses: int,
    hitRate: double,
    avgQueryTime: int
}
```

#### Flutter Side-Caching (Local)

```dart
// lib/arcade/leaderboards/arcade_leaderboard_cache_service.dart
class LocalLeaderboardCacheService {
  static const _cacheDurationMinutes = 5;
  
  final Map<String, CachedLeaderboard> _cache = {};

  Future<ArcadeLeaderboardPage?> getTopScores(
    String gameId,
    String difficulty,
  ) async {
    final key = '$gameId:$difficulty';
    final cached = _cache[key];

    // Check if cache is still valid
    if (cached != null) {
      final age = DateTime.now().difference(cached.cachedAt);
      if (age.inMinutes < _cacheDurationMinutes) {
        return cached.data;
      }
      // Cache expired, remove it
      _cache.remove(key);
    }

    return null;
  }

  void setTopScores(
    String gameId,
    String difficulty,
    ArcadeLeaderboardPage data,
  ) {
    final key = '$gameId:$difficulty';
    _cache[key] = CachedLeaderboard(
      data: data,
      cachedAt: DateTime.now(),
    );
  }

  void invalidate(String gameId, String difficulty) {
    _cache.remove('$gameId:$difficulty');
  }
}
```

#### Implementation Checklist

**Backend (2-3 hours):**
- [ ] Create ILeaderboardCacheService interface
- [ ] Implement InMemoryLeaderboardCacheService
- [ ] Register cache in dependency injection
- [ ] Update GetArcadeLeaderboard handler to use cache
- [ ] Add cache invalidation on SubmitArcadeScore
- [ ] Add cache statistics endpoint (admin only)
- [ ] Add tests for cache hit/miss scenarios (10+ tests)
- [ ] Documentation and cache strategy docs

**Frontend (1-2 hours):**
- [ ] Create LocalLeaderboardCacheService (Flutter)
- [ ] Integrate cache into ArcadeLeaderboardApiService
- [ ] Add cache hit/miss logging
- [ ] Test cache behavior on slow networks
- [ ] Monitor cache performance

---

## Development Timeline

### Week 2 (Days 8-10 post-launch)

**Monday (Day 8):**
- [ ] Code review of Phase 2 implementation plans
- [ ] Backend team: Start Learning Hub Integration
- [ ] Frontend team: Start Seasonal Leaderboards UI

**Tuesday-Wednesday (Days 9-10):**
- [ ] Learning Hub Integration: API endpoints + tests
- [ ] Seasonal Leaderboards: Backend season tracking + migration
- [ ] Performance Caching: Backend cache implementation

**Thursday (Day 11):**
- [ ] Learning Hub: Frontend integration
- [ ] Seasonal: Frontend UI implementation
- [ ] Caching: Frontend caching layer

**Friday (Day 12):**
- [ ] Integration testing across all features
- [ ] Bug fixes and polish
- [ ] Documentation completion

### Week 3 (Days 13-15 post-launch)

**Early Week (Days 13-14):**
- [ ] QA testing and verification
- [ ] Performance benchmarking
- [ ] User feedback integration

**Friday (Day 15):**
- [ ] Final testing
- [ ] Staging deployment
- [ ] Production deployment

---

## Testing & Verification

### Testing Matrix

| Feature | Unit Tests | Integration Tests | Manual Tests |
|---|---|---|---|
| Learning Hub | 10+ | 5+ | 3 scenarios |
| Seasonal Leaderboards | 15+ | 8+ | 4 scenarios |
| Performance Caching | 10+ | 5+ | Load test |

### Performance Benchmarks

**Before Caching:**
- Leaderboard query: ~200ms (P95)
- Database load: 100%

**After Caching:**
- Leaderboard query: ~20ms (P95) for top 100
- Database load: 40% reduction
- Cache hit rate: 85-95% (typical)

### User Acceptance Testing

- [ ] Learning Hub properly links to incorrect answers
- [ ] Lessons load and display correctly
- [ ] Seasonal leaderboards update correctly weekly/monthly
- [ ] Cache doesn't show stale data
- [ ] Performance is noticeably faster

---

## Success Criteria

- ✅ All Phase 2 features implemented and tested
- ✅ Learning Hub Integration: 20%+ lesson completion increase
- ✅ Seasonal Leaderboards: Active user engagement visible
- ✅ Performance Caching: 40% database load reduction
- ✅ Zero regressions to existing features
- ✅ All tests passing (230+)
- ✅ Performance targets met

---

## Rollback Plan

If critical issues detected:

1. **Learning Hub:** Disable "Learn More" button (safe to leave infrastructure)
2. **Seasonal Leaderboards:** Revert to all-time only (keep data)
3. **Caching:** Disable cache, hit database directly (rollback service)

---

## Post-Implementation Tasks

- [ ] Update documentation
- [ ] Create user-facing release notes
- [ ] Monitor analytics for engagement improvements
- [ ] Collect user feedback
- [ ] Plan Phase 3 based on learnings

---

**Status:** 🟡 Ready for implementation  
**Next Review:** After Phase 2 completion  
**Estimated Completion:** 2026-07-15
