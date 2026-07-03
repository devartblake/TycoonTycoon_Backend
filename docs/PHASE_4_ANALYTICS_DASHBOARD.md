# Phase 4 Analytics Dashboard — Detailed Implementation Plan

**Timeline:** 4+ weeks post-launch  
**Target Completion:** 2026-08-01 (Month 2)  
**Status:** 🔴 Planning Phase (Not started)  
**Priority:** High (enables data-driven decisions)

---

## Strategic Overview

The Analytics Dashboard provides production visibility and business intelligence for product decisions. It's intentionally deferred to Phase 4 because:

1. **Post-launch stability** — Need 2+ weeks of production data first
2. **High effort** — 20-30 hours of work across backend, database, and frontend
3. **Non-critical for MVP** — Core features (Quiz Review, Leaderboards) don't depend on it
4. **Data-informed** — Better to defer and build based on real usage patterns

### Success Metrics

- [ ] **Analytics Accuracy:** ±1% vs. actual metrics
- [ ] **Dashboard Load Time:** < 2 seconds for all views
- [ ] **Data Freshness:** < 5-minute lag for real-time metrics
- [ ] **Query Performance:** < 500ms for all analytics queries
- [ ] **Storage:** < 10GB for 1 month of analytics data

---

## Part 1: Event Collection & Storage

### Event Types to Track

```
LeaderboardView
├─ player_id (UUID)
├─ game_id (string)
├─ difficulty (string)
├─ view_duration_seconds (int)
├─ timestamp (datetime)
└─ source (local|global|main-leaderboard)

ScoreSubmission
├─ player_id (UUID)
├─ game_id (string)
├─ difficulty (string)
├─ score (int)
├─ duration_ms (int)
├─ is_personal_best (bool)
├─ timestamp (datetime)

QuizReviewOpen
├─ player_id (UUID)
├─ game_id (string)
├─ opened_from (results-modal|leaderboard-entry)
├─ view_duration_seconds (int)
├─ timestamp (datetime)

LearnMoreClick
├─ player_id (UUID)
├─ question_id (UUID)
├─ context (quiz-review|search)
├─ timestamp (datetime)
```

### Database Schema

```sql
-- Main event log table
CREATE TABLE analytics_events (
  id UUID PRIMARY KEY,
  event_type VARCHAR(50) NOT NULL,
  player_id UUID NOT NULL,
  game_id VARCHAR(50),
  difficulty VARCHAR(20),
  event_data JSONB, -- Flexible schema for event-specific data
  timestamp TIMESTAMPTZ DEFAULT NOW(),
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Partitioned by month for performance
-- CREATE TABLE analytics_events_2026_07 PARTITION OF analytics_events
--   FOR VALUES FROM ('2026-07-01') TO ('2026-08-01');

-- Aggregations table (updated hourly)
CREATE TABLE analytics_hourly_metrics (
  id UUID PRIMARY KEY,
  metric_name VARCHAR(100),
  game_id VARCHAR(50),
  difficulty VARCHAR(20),
  hour TIMESTAMPTZ,
  value INT,
  value_sum DECIMAL,
  value_avg DECIMAL,
  unique_players INT,
  created_at TIMESTAMPTZ DEFAULT NOW()
);

-- Indexes for performance
CREATE INDEX ix_events_timestamp ON analytics_events(timestamp DESC);
CREATE INDEX ix_events_player_timestamp ON analytics_events(player_id, timestamp DESC);
CREATE INDEX ix_events_game_difficulty ON analytics_events(game_id, difficulty);
CREATE INDEX ix_hourly_metrics_timestamp ON analytics_hourly_metrics(hour DESC);
```

### Backend Event Tracking Service

```csharp
// Synaptix.Backend.Application/Analytics/AnalyticsEventService.cs
public interface IAnalyticsEventService
{
    Task TrackEventAsync(AnalyticsEvent evt, CancellationToken ct = default);
    Task TrackBatchAsync(IEnumerable<AnalyticsEvent> events, CancellationToken ct = default);
}

public class AnalyticsEventService : IAnalyticsEventService
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<AnalyticsEventService> _logger;
    private readonly IQueue<AnalyticsEvent> _eventQueue;

    public async Task TrackEventAsync(AnalyticsEvent evt, CancellationToken ct)
    {
        // Queue event for async processing
        await _eventQueue.EnqueueAsync(evt, ct);
    }

    public async Task TrackBatchAsync(
        IEnumerable<AnalyticsEvent> events,
        CancellationToken ct)
    {
        // Bulk insert for efficiency
        await _repository.InsertEventsAsync(events, ct);
    }
}

// Usage in handlers
public class SubmitArcadeScoreHandler : IRequestHandler<SubmitArcadeScore, Result>
{
    private readonly IAnalyticsEventService _analytics;

    public async Task<Result> Handle(SubmitArcadeScore request, CancellationToken ct)
    {
        // ... existing score submission logic ...

        // Track event
        await _analytics.TrackEventAsync(new AnalyticsEvent
        {
            EventType = "ScoreSubmission",
            PlayerId = request.PlayerId,
            GameId = request.GameId,
            Difficulty = request.Difficulty,
            EventData = new
            {
                request.Score,
                request.DurationMs,
                IsPersonalBest = isNewBest,
            }
        }, ct);

        return result;
    }
}
```

---

## Part 2: Analytics API Endpoints

### Core Endpoints

```csharp
// Synaptix.Backend.Api/Features/Analytics/AnalyticsEndpoints.cs

namespace Synaptix.Backend.Api.Features.Analytics;

public static class AnalyticsEndpoints
{
    public static void MapAnalyticsRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/analytics")
            .WithTags("Analytics")
            .RequireAuthorization("AdminOnly");

        group.MapGet("/overview", GetAnalyticsOverview)
            .WithName("GetAnalyticsOverview")
            .WithOpenApi();

        group.MapGet("/leaderboard/{gameId}/{difficulty}/trends", GetLeaderboardTrends)
            .WithName("GetLeaderboardTrends")
            .WithOpenApi();

        group.MapGet("/leaderboard/top-games", GetTopGames)
            .WithName("GetTopGames")
            .WithOpenApi();

        group.MapGet("/quiz-review/metrics", GetQuizReviewMetrics)
            .WithName("GetQuizReviewMetrics")
            .WithOpenApi();

        group.MapGet("/score-submissions/distribution", GetScoreDistribution)
            .WithName("GetScoreDistribution")
            .WithOpenApi();

        group.MapGet("/engagement/daily-active-users", GetDailyActiveUsers)
            .WithName("GetDailyActiveUsers")
            .WithOpenApi();

        group.MapGet("/engagement/retention-cohort", GetRetentionCohort)
            .WithName("GetRetentionCohort")
            .WithOpenApi();
    }

    private static async Task<IResult> GetAnalyticsOverview(
        IAnalyticsService service,
        CancellationToken ct)
    {
        var result = await service.GetOverviewAsync(
            last24Hours: true,
            ct: ct
        );
        return Results.Ok(result);
    }

    private static async Task<IResult> GetLeaderboardTrends(
        string gameId,
        string difficulty,
        IAnalyticsService service,
        CancellationToken ct)
    {
        var result = await service.GetLeaderboardTrendsAsync(
            gameId,
            difficulty,
            daysBack: 7,
            ct: ct
        );
        return Results.Ok(result);
    }

    // ... other endpoints ...
}
```

### Analytics Service Layer

```csharp
// Synaptix.Backend.Application/Analytics/AnalyticsService.cs

public interface IAnalyticsService
{
    Task<AnalyticsOverviewDto> GetOverviewAsync(bool last24Hours = false, CancellationToken ct = default);
    Task<LeaderboardTrendsDto> GetLeaderboardTrendsAsync(string gameId, string difficulty, int daysBack = 7, CancellationToken ct = default);
    Task<List<GameAnalyticsDto>> GetTopGamesAsync(int topN = 10, CancellationToken ct = default);
    Task<QuizReviewAnalyticsDto> GetQuizReviewMetricsAsync(CancellationToken ct = default);
    Task<ScoreDistributionDto> GetScoreDistributionAsync(string gameId, string difficulty, CancellationToken ct = default);
}

public class AnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsRepository _repository;
    private readonly ILogger<AnalyticsService> _logger;

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        bool last24Hours = false,
        CancellationToken ct = default)
    {
        var metrics = await _repository.GetHourlyMetricsAsync(
            hoursBack: last24Hours ? 24 : 168, // 24h or 7d
            ct: ct
        );

        return new AnalyticsOverviewDto
        {
            TotalViews = metrics.Sum(m => m.Value),
            UniquePlayerCount = metrics.Sum(m => m.UniquePlayerCount),
            AverageSessionDuration = metrics.Average(m => m.SessionDuration),
            TopGame = metrics
                .GroupBy(m => m.GameId)
                .OrderByDescending(g => g.Sum(m => m.Value))
                .First()
                .Key,
            PeakHour = metrics
                .OrderByDescending(m => m.Value)
                .First()
                .Hour,
        };
    }

    public async Task<LeaderboardTrendsDto> GetLeaderboardTrendsAsync(
        string gameId,
        string difficulty,
        int daysBack = 7,
        CancellationToken ct = default)
    {
        var events = await _repository.GetEventsAsync(
            gameId: gameId,
            difficulty: difficulty,
            daysBack: daysBack,
            eventTypes: new[] { "LeaderboardView", "ScoreSubmission" },
            ct: ct
        );

        return new LeaderboardTrendsDto
        {
            GameId = gameId,
            Difficulty = difficulty,
            DailyViews = events
                .Where(e => e.EventType == "LeaderboardView")
                .GroupBy(e => e.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyMetricDto
                {
                    Date = g.Key,
                    Count = g.Count(),
                })
                .ToList(),
            DailySubmissions = events
                .Where(e => e.EventType == "ScoreSubmission")
                .GroupBy(e => e.Timestamp.Date)
                .OrderBy(g => g.Key)
                .Select(g => new DailyMetricDto
                {
                    Date = g.Key,
                    Count = g.Count(),
                })
                .ToList(),
        };
    }
}
```

---

## Part 3: Analytics Dashboard UI (Flutter)

### Admin Dashboard Screen

```dart
// lib/admin/screens/analytics_dashboard_screen.dart

class AnalyticsDashboardScreen extends ConsumerStatefulWidget {
  const AnalyticsDashboardScreen({Key? key}) : super(key: key);

  @override
  ConsumerState<AnalyticsDashboardScreen> createState() =>
      _AnalyticsDashboardScreenState();
}

class _AnalyticsDashboardScreenState
    extends ConsumerState<AnalyticsDashboardScreen> {
  DateRange _selectedDateRange = DateRange.last7Days;
  String? _selectedGame;

  @override
  Widget build(BuildContext context) {
    final analyticsAsync = ref.watch(
      analyticsOverviewProvider(_selectedDateRange),
    );

    return Scaffold(
      appBar: AppBar(
        title: const Text('Analytics Dashboard'),
        centerTitle: true,
      ),
      body: analyticsAsync.when(
        loading: () => const Center(child: CircularProgressIndicator()),
        error: (error, stack) => ErrorScreen(error: error),
        data: (analytics) => SingleChildScrollView(
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              children: [
                // Date range selector
                _buildDateRangeSelector(),
                const SizedBox(height: 24),

                // KPI Cards
                _buildKpiSection(analytics),
                const SizedBox(height: 24),

                // Charts
                _buildGameBreakdownChart(analytics),
                const SizedBox(height: 24),

                _buildTrendsChart(analytics),
                const SizedBox(height: 24),

                _buildQuizReviewMetrics(analytics),
              ],
            ),
          ),
        ),
      ),
    );
  }

  Widget _buildDateRangeSelector() {
    return SegmentedButton<DateRange>(
      segments: const [
        ButtonSegment(value: DateRange.last24Hours, label: Text('24h')),
        ButtonSegment(value: DateRange.last7Days, label: Text('7d')),
        ButtonSegment(value: DateRange.last30Days, label: Text('30d')),
      ],
      selected: {_selectedDateRange},
      onSelectionChanged: (selected) {
        setState(() => _selectedDateRange = selected.first);
      },
    );
  }

  Widget _buildKpiSection(AnalyticsOverviewDto analytics) {
    return GridView.count(
      crossAxisCount: 2,
      shrinkWrap: true,
      physics: const NeverScrollableScrollPhysics(),
      mainAxisSpacing: 16,
      crossAxisSpacing: 16,
      children: [
        _buildKpiCard(
          title: 'Total Views',
          value: '${analytics.totalViews}',
          change: '+12%',
        ),
        _buildKpiCard(
          title: 'Active Players',
          value: '${analytics.uniquePlayerCount}',
          change: '+8%',
        ),
        _buildKpiCard(
          title: 'Avg Session',
          value: '${analytics.averageSessionDuration}m',
          change: '+3m',
        ),
        _buildKpiCard(
          title: 'Quiz Reviews',
          value: '${analytics.quizReviewCount}',
          change: '+25%',
        ),
      ],
    );
  }

  Widget _buildKpiCard({
    required String title,
    required String value,
    required String change,
  }) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(context).textTheme.labelMedium,
            ),
            const SizedBox(height: 8),
            Text(
              value,
              style: Theme.of(context).textTheme.headlineSmall,
            ),
            const SizedBox(height: 8),
            Text(
              change,
              style: TextStyle(
                color: change.startsWith('+') ? Colors.green : Colors.red,
                fontSize: 12,
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildGameBreakdownChart(AnalyticsOverviewDto analytics) {
    // Use fl_chart for pie/bar charts
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Views by Game',
              style: Theme.of(context).textTheme.labelLarge,
            ),
            const SizedBox(height: 16),
            // PieChart widget here
          ],
        ),
      ),
    );
  }

  Widget _buildTrendsChart(AnalyticsOverviewDto analytics) {
    // Time-series line chart
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Engagement Trends',
              style: Theme.of(context).textTheme.labelLarge,
            ),
            const SizedBox(height: 16),
            // LineChart widget here
          ],
        ),
      ),
    );
  }

  Widget _buildQuizReviewMetrics(AnalyticsOverviewDto analytics) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              'Quiz Review Metrics',
              style: Theme.of(context).textTheme.labelLarge,
            ),
            const SizedBox(height: 16),
            ListTile(
              title: const Text('Reviews Opened'),
              trailing: Text('${analytics.quizReviewCount}'),
            ),
            ListTile(
              title: const Text('Learn More Clicks'),
              trailing: Text('${analytics.learnMoreClickCount}'),
            ),
          ],
        ),
      ),
    );
  }
}
```

### Riverpod Providers

```dart
// lib/admin/providers/analytics_providers.dart

enum DateRange { last24Hours, last7Days, last30Days }

final analyticsOverviewProvider =
    FutureProvider.autoDispose.family<AnalyticsOverviewDto, DateRange>(
  (ref, dateRange) async {
    final client = ref.watch(synaptixApiClientProvider);
    final response = await client.get('/analytics/overview');
    return AnalyticsOverviewDto.fromJson(response);
  },
);

final leaderboardTrendsProvider = FutureProvider.autoDispose.family<
  LeaderboardTrendsDto,
  ({String gameId, String difficulty})
>((ref, params) async {
  final client = ref.watch(synaptixApiClientProvider);
  final response = await client.get(
    '/analytics/leaderboard/${params.gameId}/${params.difficulty}/trends',
  );
  return LeaderboardTrendsDto.fromJson(response);
});

final topGamesProvider = FutureProvider.autoDispose<List<GameAnalyticsDto>>(
  (ref) async {
    final client = ref.watch(synaptixApiClientProvider);
    final response = await client.get('/analytics/leaderboard/top-games');
    return (response['games'] as List)
        .map((json) => GameAnalyticsDto.fromJson(json))
        .toList();
  },
);
```

---

## Part 4: Performance Optimization

### Query Optimization

```sql
-- Materialized view for hourly metrics (refresh every hour)
CREATE MATERIALIZED VIEW mv_hourly_metrics AS
SELECT
    DATE_TRUNC('hour', timestamp) as hour,
    event_type,
    game_id,
    difficulty,
    COUNT(*) as event_count,
    COUNT(DISTINCT player_id) as unique_players,
    AVG((event_data->>'view_duration_seconds')::INT) as avg_duration
FROM analytics_events
WHERE timestamp > NOW() - INTERVAL '30 days'
GROUP BY hour, event_type, game_id, difficulty;

CREATE UNIQUE INDEX ON mv_hourly_metrics (hour, event_type, game_id, difficulty);

-- Refresh view hourly
REFRESH MATERIALIZED VIEW CONCURRENTLY mv_hourly_metrics;
```

### Caching Strategy

```csharp
// Use Redis for frequently accessed metrics
public class CachedAnalyticsService : IAnalyticsService
{
    private readonly IAnalyticsService _inner;
    private readonly IDistributedCache _cache;

    public async Task<AnalyticsOverviewDto> GetOverviewAsync(
        bool last24Hours = false,
        CancellationToken ct = default)
    {
        var cacheKey = $"analytics:overview:{(last24Hours ? "24h" : "7d")}";

        var cached = await _cache.GetAsync(cacheKey, ct);
        if (cached != null)
        {
            return JsonSerializer.Deserialize<AnalyticsOverviewDto>(cached)!;
        }

        var result = await _inner.GetOverviewAsync(last24Hours, ct);

        // Cache for 5 minutes
        await _cache.SetAsync(
            cacheKey,
            JsonSerializer.SerializeToUtf8Bytes(result),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
            },
            ct
        );

        return result;
    }
}
```

---

## Part 5: Data Retention & Cleanup

### Archive Old Events

```sql
-- Archive events older than 90 days
CREATE PROCEDURE ArchiveOldAnalyticsEvents()
AS
BEGIN
    -- Move to archive table
    INSERT INTO analytics_events_archive
    SELECT * FROM analytics_events
    WHERE timestamp < NOW() - INTERVAL '90 days';

    -- Delete from main table
    DELETE FROM analytics_events
    WHERE timestamp < NOW() - INTERVAL '90 days';

    -- Vacuum to reclaim space
    VACUUM ANALYZE analytics_events;
END;

-- Run daily via scheduled job
EXEC msdb.dbo.sp_add_job @job_name = 'ArchiveOldAnalyticsEvents'
EXEC msdb.dbo.sp_add_jobstep @job_name = 'ArchiveOldAnalyticsEvents', @command = 'EXEC ArchiveOldAnalyticsEvents'
EXEC msdb.dbo.sp_add_schedule @schedule_name = 'DailyMidnight', @freq_type = 4, @freq_interval = 1, @active_start_time = 000000
EXEC msdb.dbo.sp_attach_schedule @job_name = 'ArchiveOldAnalyticsEvents', @schedule_name = 'DailyMidnight'
```

---

## Implementation Timeline

### Week 1 (Days 29-35 post-launch)

**Backend Setup (2-3 days):**
- [ ] Create event tracking infrastructure
- [ ] Implement IAnalyticsEventService
- [ ] Set up event queue/worker
- [ ] Create database schema (events, metrics, archive)
- [ ] Wire tracking into existing handlers

**API Endpoints (1-2 days):**
- [ ] Implement GetAnalyticsOverview
- [ ] Implement GetLeaderboardTrends
- [ ] Implement GetTopGames
- [ ] Add admin authorization checks

### Week 2 (Days 36-42 post-launch)

**Dashboard UI (2-3 days):**
- [ ] Create AnalyticsDashboardScreen
- [ ] Implement all chart types (KPI cards, pie, line, bar)
- [ ] Add date range selector
- [ ] Add game/difficulty filter

**Testing & Polish (1-2 days):**
- [ ] Backend tests (20+ test cases)
- [ ] Frontend integration testing
- [ ] Performance benchmarking
- [ ] Documentation

---

## Success Metrics

- ✅ Dashboard loads in < 2 seconds
- ✅ Metrics accurate within ±1%
- ✅ No N+1 query problems
- ✅ All analytics queries < 500ms
- ✅ 99.9% uptime for analytics endpoints
- ✅ Actionable insights derived from dashboarard

---

## Known Limitations & Future Work

1. **Real-time Updates:** Current design is near-real-time (5min lag). Future: WebSocket for live metrics
2. **Predictive Analytics:** Not included. Future: ML models for churn/engagement prediction
3. **Custom Reports:** Not included. Future: Allow admins to build custom analytics views
4. **Alerts:** Not included. Future: Alert admins on anomalies (crash spikes, drop in engagement)

---

**Status:** 🔴 Planning Phase (Deferred to Week 4+)  
**Estimated Completion:** 2026-08-01  
**Last Updated:** 2026-07-01
