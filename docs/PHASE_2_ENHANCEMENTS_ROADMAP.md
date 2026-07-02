# Phase 2: Post-Launch Enhancements Roadmap

**Timeline:** Post-production deployment (Week 2-4)  
**Status:** Planning phase  
**Enhancements Selected:** 3

---

## Enhancement 1: Learning Hub Integration

**Goal:** Link quiz review wrong answers to relevant Learning Hub lessons for contextual education.

### Architecture

```
Quiz Review Screen
    ↓ User taps "Learn More" on wrong answer
    ↓
Learning Hub Integration Service
    ↓ Search for lessons matching question category/topic
    ↓
Learning Hub Screen (module_detail_screen.dart)
    ↓ User completes lesson
    ↓
Return to quiz review with lesson completion indicator
```

### Implementation Steps

#### Step 1: Create Integration Model (Flutter)

**File:** `lib/arcade/leaderboards/quiz_learning_link.dart`

```dart
class QuizLearningLink {
  final String questionId;      // From AnsweredQuestionRecord.prompt
  final String category;        // e.g., "mathematics", "science"
  final String suggestedLesson; // Lesson ID to link to
  final String lessonTitle;
  final int difficulty;         // 1=Easy, 2=Medium, 3=Hard, 4=Expert
}

class QuizLearningService {
  /// Find relevant lessons for a wrong answer
  Future<List<QuizLearningLink>> findLessonsForAnswer(
    AnsweredQuestionRecord record
  ) async {
    // Extract category from question prompt
    // Query Learning Hub API for matching lessons
    // Return ranked by relevance
  }
}
```

#### Step 2: Enhance Quiz Review Screen

**File:** `lib/arcade/ui/screens/quiz_review_screen.dart` (modify)

```dart
// In _QuestionTile, when isCorrect == false, add:
ElevatedButton.icon(
  icon: Icon(Icons.school),
  label: Text('Learn About This'),
  onPressed: () => _showLessonSuggestions(context, record),
)

// Show modal with suggested lessons
Future<void> _showLessonSuggestions(
  BuildContext context,
  AnsweredQuestionRecord record
) async {
  final service = ref.read(quizLearningServiceProvider);
  final lessons = await service.findLessonsForAnswer(record);
  
  showModalBottomSheet(
    builder: (_) => LessonSuggestionsModal(
      lessons: lessons,
      onSelectLesson: (lesson) {
        context.push('/learn-hub/module/${lesson.moduleId}');
      },
    ),
  );
}
```

#### Step 3: Create Backend Lesson Suggestion Endpoint

**File:** `Synaptix.Backend.Api/Features/Learning/LessonSuggestionEndpoints.cs` (new)

```csharp
public static void Map(IEndpointRouteBuilder app)
{
  var g = app.MapGroup("/learning").WithTags("Learning");

  g.MapPost("/suggest-lessons", SuggestLessons)
    .WithName("SuggestLessons");
}

private static async Task<IResult> SuggestLessons(
  [FromBody] LessonSuggestionRequest request,
  IMediator mediator,
  CancellationToken ct)
{
  // Extract topic from question
  // Query Learning Hub for matching lessons
  // Return ranked by difficulty relevance
  
  var response = await mediator.Send(
    new GetSuggestedLessons(request.Category, request.Difficulty),
    ct
  );
  
  return Results.Ok(response);
}
```

#### Step 4: Create Learning Hub API Service

**File:** `lib/core/repositories/learning_hub_integration_service.dart` (new)

```dart
class LearningHubIntegrationService {
  Future<List<LessonDto>> getLessonsByCategory(
    String category,
    int difficulty,
  ) async {
    // Call backend endpoint
    // Filter by category + difficulty
    // Return matching lessons
  }
}
```

### Testing

- [ ] Unit tests: Lesson matching logic
- [ ] Integration tests: Backend suggestion endpoint
- [ ] E2E: Wrong answer → Lesson selection → Navigation

### Effort Estimate: 4-6 hours

---

## Enhancement 2: Seasonal Leaderboards

**Goal:** Add time-based leaderboard filtering (All-Time, Monthly, Weekly).

### Architecture

```
Database Changes:
  - Add 'Season' column to arcade_scores table
  - Season = date-based grouping (YYYY-MM, YYYY-WW)
  - Index on (gameId, difficulty, season, score DESC)

API Changes:
  - GET /leaderboards/arcade/{gameId}/{difficulty}?season=YYYY-MM
  - Season values: "all-time", "current-month", "current-week", "YYYY-MM", "YYYY-WW"

Frontend Changes:
  - Add season picker to leaderboard screens
  - Show current season by default
  - Cache results per season
```

### Implementation Steps

#### Step 1: Extend Database Schema

**File:** `Synaptix.Backend.Migrations/Migrations/[new migration]`

```csharp
migrationBuilder.AddColumn<string>(
  name: "season",
  table: "arcade_scores",
  type: "text",
  nullable: false,
  defaultValue: "all-time"); // for existing records

migrationBuilder.CreateIndex(
  name: "ix_arcade_scores_game_id_difficulty_season_score",
  table: "arcade_scores",
  columns: ["game_id", "difficulty", "season", "score"],
  descending: [false, false, false, true]);
```

#### Step 2: Update GetArcadeLeaderboard Query Handler

**File:** `Synaptix.Backend.Application/Leaderboards/GetArcadeLeaderboard.cs` (modify)

```csharp
// Add season parameter
public sealed record GetArcadeLeaderboard(
  string GameId,
  string Difficulty,
  int Page = 1,
  int PageSize = 50,
  Guid? PlayerId = null,
  string Season = "all-time"  // NEW
) : IRequest<ArcadeLeaderboardResponseDto>;

// Modify query to filter by season
var query = from e in db.ArcadeScores.AsNoTracking()
  where e.GameId == r.GameId 
    && e.Difficulty == r.Difficulty
    && (r.Season == "all-time" || e.Season == r.Season)
  orderby e.Score descending, e.DurationMs ascending
  select e;
```

#### Step 3: Update API Endpoint

**File:** `Synaptix.Backend.Api/Features/Leaderboards/ArcadeLeaderboardEndpoints.cs` (modify)

```csharp
private static async Task<IResult> GetLeaderboard(
  [FromRoute] string gameId,
  [FromRoute] string difficulty,
  HttpContext httpContext,
  IMediator mediator,
  CancellationToken ct,
  [FromQuery] int page = 1,
  [FromQuery] int pageSize = 50,
  [FromQuery] string season = "all-time")  // NEW
{
  // Validate season format (YYYY-MM, YYYY-WW, or all-time)
  if (!ValidateSeason(season))
    return Results.BadRequest(new { error = "Invalid season format" });
  
  var response = await mediator.Send(
    new GetArcadeLeaderboard(gameId, difficulty, page, pageSize, playerId, season),
    ct
  );
  
  return Results.Ok(response);
}
```

#### Step 4: Update Submission Handler

**File:** `Synaptix.Backend.Application/Leaderboards/SubmitArcadeScore.cs` (modify)

```csharp
// Auto-compute season when creating entry
var currentSeason = ComputeCurrentSeason(); // e.g., "2026-07" for July 2026

var entry = new ArcadeScoreEntry(
  r.PlayerId,
  r.GameId,
  r.Difficulty,
  r.Score,
  r.DurationMs,
  DateTimeOffset.UtcNow,
  currentSeason  // NEW
);
```

#### Step 5: Update Flutter UI

**File:** `lib/arcade/leaderboards/arcade_global_leaderboard_view.dart` (modify)

```dart
class ArcadeGlobalLeaderboardView extends ConsumerStatefulWidget {
  @override
  ConsumerState<ArcadeGlobalLeaderboardView> createState() =>
    _ArcadeGlobalLeaderboardViewState();
}

class _ArcadeGlobalLeaderboardViewState extends ConsumerState {
  String _selectedSeason = "all-time";

  @override
  Widget build(BuildContext context) {
    return Column(
      children: [
        // Season picker
        Padding(
          padding: const EdgeInsets.all(12),
          child: SegmentedButton<String>(
            segments: const [
              ButtonSegment(value: "current-week", label: Text("Weekly")),
              ButtonSegment(value: "current-month", label: Text("Monthly")),
              ButtonSegment(value: "all-time", label: Text("All-Time")),
            ],
            selected: {_selectedSeason},
            onSelectionChanged: (Set<String> newSelection) {
              setState(() => _selectedSeason = newSelection.first);
              _loadPage();
            },
          ),
        ),
        // Leaderboard view (use _selectedSeason in query)
        Expanded(
          child: _buildLeaderboard(),
        ),
      ],
    );
  }

  Future<void> _loadPage() async {
    final service = ref.read(arcadeLeaderboardApiServiceProvider);
    _currentData = await service.fetchLeaderboard(
      gameId: widget.gameId,
      difficulty: widget.difficulty,
      page: _currentPage,
      pageSize: _pageSize,
      season: _selectedSeason,  // NEW
    );
  }
}
```

#### Step 6: Update API Service

**File:** `lib/arcade/leaderboards/arcade_leaderboard_api_service.dart` (modify)

```dart
Future<ArcadeLeaderboardPage> fetchLeaderboard(
  ArcadeGameId gameId,
  ArcadeDifficulty difficulty, {
  int page = 1,
  int pageSize = 50,
  String season = "all-time",  // NEW
}) async {
  final response = await _apiClient.getJson(
    '/leaderboards/arcade/${gameId.name}/${difficulty.name}',
    query: {
      'page': page.toString(),
      'pageSize': pageSize.toString(),
      'season': season,  // NEW
    },
  );

  return ArcadeLeaderboardPage.fromJson(response as Map<String, dynamic>);
}
```

### Testing

- [ ] Unit tests: Season filtering logic
- [ ] Integration tests: Seasonal queries return correct data
- [ ] E2E: Switch between seasons, verify data updates
- [ ] Migration testing: Existing scores assigned to "all-time"

### Effort Estimate: 6-8 hours

---

## Enhancement 3: Performance Caching

**Goal:** Cache top 100 global scores in-memory to reduce database queries.

### Architecture

```
Request Flow:
  GET /leaderboards/arcade/{gameId}/{difficulty}
    ↓
  Check cache for {gameId, difficulty, page=1}
    ↓ Cache hit
  Return cached data (< 1ms response)
    ↓ Cache miss
  Query database
    ↓
  Update cache
    ↓
  Return data (50-100ms response)

Cache Invalidation:
  On score submission:
    - If new score is in top 100 → invalidate cache
    - Otherwise → keep cache valid
```

### Implementation Steps

#### Step 1: Create Caching Service

**File:** `Synaptix.Backend.Application/Caching/ArcadeLeaderboardCacheService.cs` (new)

```csharp
public interface IArcadeLeaderboardCacheService
{
  Task<CachedLeaderboard?> GetAsync(string gameId, string difficulty);
  Task SetAsync(string gameId, string difficulty, CachedLeaderboard data, TimeSpan ttl);
  Task InvalidateAsync(string gameId, string difficulty);
}

public class ArcadeLeaderboardCacheService : IArcadeLeaderboardCacheService
{
  private readonly IMemoryCache _cache;
  private readonly TimeSpan _defaultTtl = TimeSpan.FromMinutes(5);

  public async Task<CachedLeaderboard?> GetAsync(string gameId, string difficulty)
  {
    var key = $"arcade_leaderboard:{gameId}:{difficulty}";
    return _cache.TryGetValue(key, out var value) ? value as CachedLeaderboard : null;
  }

  public async Task SetAsync(string gameId, string difficulty, CachedLeaderboard data, TimeSpan? ttl = null)
  {
    var key = $"arcade_leaderboard:{gameId}:{difficulty}";
    _cache.Set(key, data, ttl ?? _defaultTtl);
  }

  public async Task InvalidateAsync(string gameId, string difficulty)
  {
    var key = $"arcade_leaderboard:{gameId}:{difficulty}";
    _cache.Remove(key);
  }
}

public record CachedLeaderboard(
  List<ArcadeLeaderboardEntryDto> TopEntries,
  DateTime CachedAt,
  int TotalCount
);
```

#### Step 2: Modify GetArcadeLeaderboard Handler

**File:** `Synaptix.Backend.Application/Leaderboards/GetArcadeLeaderboard.cs` (modify)

```csharp
public class GetArcadeLeaderboardHandler : IRequestHandler<GetArcadeLeaderboard, ArcadeLeaderboardResponseDto>
{
  private readonly IAppDb _db;
  private readonly IArcadeLeaderboardCacheService _cache;

  public async ValueTask<ArcadeLeaderboardResponseDto> Handle(
    GetArcadeLeaderboard r,
    CancellationToken ct)
  {
    // For first page, check cache
    if (r.Page == 1)
    {
      var cached = await _cache.GetAsync(r.GameId, r.Difficulty);
      if (cached != null && DateTime.UtcNow.Subtract(cached.CachedAt).TotalMinutes < 5)
      {
        // Return from cache (skip database)
        return MapCachedToResponse(cached, r);
      }
    }

    // Query database (existing logic)
    var response = await FetchFromDatabaseAsync(r, ct);

    // Cache top 100 for first page
    if (r.Page == 1)
    {
      await _cache.SetAsync(r.GameId, r.Difficulty,
        new CachedLeaderboard(response.Items, DateTime.UtcNow, response.Total));
    }

    return response;
  }

  private ArcadeLeaderboardResponseDto MapCachedToResponse(
    CachedLeaderboard cached,
    GetArcadeLeaderboard r)
  {
    // Return cache with pagination applied
    var items = cached.TopEntries
      .Skip((r.Page - 1) * r.PageSize)
      .Take(r.PageSize)
      .ToList();

    return new ArcadeLeaderboardResponseDto(
      r.GameId, r.Difficulty, r.Page, r.PageSize,
      cached.TotalCount, items,
      null, null  // myRank, myScore
    );
  }
}
```

#### Step 3: Modify SubmitArcadeScore Handler

**File:** `Synaptix.Backend.Application/Leaderboards/SubmitArcadeScore.cs` (modify)

```csharp
public class SubmitArcadeScoreHandler : IRequestHandler<SubmitArcadeScore, bool>
{
  private readonly IAppDb _db;
  private readonly IArcadeLeaderboardCacheService _cache;

  public async ValueTask<bool> Handle(SubmitArcadeScore r, CancellationToken ct)
  {
    // ... existing logic ...

    // After successful submit, check if score is in top 100
    var rankInLeaderboard = await CheckIfInTopAsync(r.PlayerId, r.GameId, r.Difficulty, r.Score, ct);
    
    if (rankInLeaderboard <= 100)
    {
      // Invalidate cache - new top 100 entry
      await _cache.InvalidateAsync(r.GameId, r.Difficulty);
    }

    return true;
  }

  private async Task<int> CheckIfInTopAsync(
    Guid playerId, string gameId, string difficulty, int score, CancellationToken ct)
  {
    return await _db.ArcadeScores
      .AsNoTracking()
      .Where(e => e.GameId == gameId && e.Difficulty == difficulty && e.Score > score)
      .CountAsync(ct) + 1;
  }
}
```

#### Step 4: Register Cache Service

**File:** `Synaptix.Backend.Api/Program.cs` (modify)

```csharp
// Add memory cache
builder.Services.AddMemoryCache();

// Register caching service
builder.Services.AddScoped<IArcadeLeaderboardCacheService, ArcadeLeaderboardCacheService>();
```

### Monitoring

```csharp
// Add metrics to track cache effectiveness
public class CacheMetrics
{
  public long CacheHits { get; set; }
  public long CacheMisses { get; set; }
  public double HitRate => CacheHits / (double)(CacheHits + CacheMisses) * 100;
}
```

Expected results after caching:
- **P50 latency:** 50ms → 5ms (10x faster for cached queries)
- **P95 latency:** 200ms → 50ms (4x faster)
- **Database load:** 60% → 30% (50% reduction)
- **Cache hit rate:** 90%+ (for stable leaderboards)

### Testing

- [ ] Unit tests: Cache hit/miss logic
- [ ] Integration tests: Cache invalidation on score submit
- [ ] Load tests: Verify cache reduces database queries
- [ ] Metrics: Monitor hit rate in production

### Effort Estimate: 4-6 hours

---

## Phase 2 Implementation Timeline

| Enhancement | Duration | Effort | Priority |
|-------------|----------|--------|----------|
| Learning Hub Integration | 4-6 hrs | Medium | High |
| Seasonal Leaderboards | 6-8 hrs | High | Medium |
| Performance Caching | 4-6 hrs | Medium | High |
| **Total** | **14-20 hrs** | **Medium-High** | |

### Recommended Rollout Schedule

```
Week 1 (Post-Launch): Monitoring & bug fixes
Week 2: Implement Learning Hub Integration + Performance Caching (parallel)
Week 3: Testing & QA
Week 4: Deploy Learning Hub + Caching, implement Seasonal Leaderboards
Week 5: Test Seasonal Leaderboards
Week 6: Deploy Seasonal Leaderboards
```

---

## Success Metrics

### Learning Hub Integration
- ✅ 30%+ of wrong answers link to lessons
- ✅ 20%+ of users click "Learn More"
- ✅ Lesson completion rate > 50%

### Seasonal Leaderboards
- ✅ Weekly/Monthly views used 40%+ of time
- ✅ User engagement increases 15%+
- ✅ Repeat leaderboard visits +25%

### Performance Caching
- ✅ Cache hit rate 85%+
- ✅ P50 latency < 10ms
- ✅ Database CPU usage -40%
- ✅ Zero cache invalidation bugs

---

## Next Steps

1. ✅ **Phase 1 Complete:** Deploy current features to production
2. ⏳ **Phase 2 Ready:** Execute enhancements per timeline above
3. 📊 **Phase 3 Planned:** Analytics dashboard + social features

**Ready to deploy to production?** See [DEPLOYMENT_GUIDE.md](./DEPLOYMENT_GUIDE.md) for step-by-step instructions.
