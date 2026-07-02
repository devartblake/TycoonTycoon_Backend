# Deferred Enhancements — Phase 2 & Beyond

**Document:** Optional enhancements evaluated but not selected for immediate implementation  
**Status:** Backlog / Future Consideration  
**Last Updated:** July 1, 2026

---

## Overview

During the Phase 2 planning process, six enhancement candidates were evaluated. **Three were selected** for Phase 2 implementation (2-3 weeks post-launch), and **three were deferred** to future phases.

This document outlines the deferred enhancements, their business value, technical approach, and rationale for deferral.

---

## Selected for Phase 2 ✅

These enhancements will ship 2-3 weeks after production launch:

1. **Learning Hub Integration** — Link wrong answers to lessons
2. **Seasonal Leaderboards** — Weekly/Monthly/All-Time filtering
3. **Performance Caching** — Cache top 100 scores in-memory

See [PHASE_2_ENHANCEMENTS_ROADMAP.md](./PHASE_2_ENHANCEMENTS_ROADMAP.md) for implementation details.

---

## Deferred Enhancements (Phase 3+)

### 1. Difficulty Picker (Local Leaderboard View)

**Status:** ⏳ Deferred to Phase 3  
**Effort Estimate:** 2-3 hours  
**Business Value:** Medium  
**Priority:** Low (nice-to-have, not critical)

#### Description

Add difficulty filtering to the local arcade leaderboard view, enabling players to see their best scores per difficulty on their device.

#### Current State

Local leaderboard shows top 10 scores merged across all difficulties:
```
Local Leaderboard (Device Storage)
├─ Pattern Sprint
│  ├─ Score 1500 (Easy)
│  ├─ Score 1400 (Normal)
│  ├─ Score 1300 (Hard)
│  └─ Score 1200 (Insane)
```

#### Desired State

Add difficulty toggle to show only scores for selected difficulty:
```
Local Leaderboard (Device Storage)
├─ Difficulty Picker: [Easy] [Normal] [Hard] [Insane]
├─ Pattern Sprint (Hard)
│  ├─ Score 1300 (Duration: 28s)
│  ├─ Score 1280 (Duration: 29s)
│  └─ Score 1250 (Duration: 31s)
```

#### Implementation Approach

**File:** `lib/arcade/leaderboards/local_arcade_leaderboard_screen.dart` (modify `_buildGameLeaderboard` method)

```dart
// Current: Shows top 10 for all difficulties
final scores = svc.topForGame(game.id, limit: 10);

// After enhancement: Shows top 10 for selected difficulty
ArcadeDifficulty _selectedDifficulty = ArcadeDifficulty.normal;

final scores = svc.topForGame(game.id, limit: 10)
    .where((s) => s.difficulty == _selectedDifficulty)
    .toList();

// Add difficulty picker widget (similar to global leaderboard)
```

**Complexity:** Low (reuse existing difficulty picker from global view)  
**Testing:** Unit test local filtering logic

#### Why Deferred

- **Low priority:** Global leaderboard already has difficulty picker (good enough for MVP)
- **Limited impact:** Local view shows merged difficulties, which is acceptable for immediate post-launch
- **Can enhance later:** No blocking dependencies; safe to add in Phase 3
- **UX is acceptable:** Current merged view works; not a blocker for launch

#### Business Impact

- Would improve local leaderboard UX by 10-15%
- Helps players track difficulty-specific progress
- Low technical risk, can be added anytime

#### Reconsidering in Phase 3

This enhancement should be reconsidered if:
- User feedback shows local view needs difficulty filtering
- Difficulty-specific tracking becomes important for players
- It's paired with other local leaderboard improvements

---

### 2. Analytics Dashboard

**Status:** ⏳ Deferred to Phase 4  
**Effort Estimate:** 20-30 hours  
**Business Value:** High  
**Priority:** Medium (important for insights, not critical for MVP)

#### Description

Build an internal analytics dashboard to track leaderboard engagement, quiz review usage, and player behavior metrics.

#### Metrics to Track

**Engagement Metrics**
```
- Total leaderboard views (per game, per difficulty, global vs local)
- Daily active players using leaderboards
- Repeat leaderboard visitors (% who return within 24hr/7d/30d)
- Leaderboard view duration
```

**Quiz Review Metrics**
```
- Total quiz reviews opened
- Avg questions reviewed per session
- % of wrong answers that trigger "Learn More"
- Learning Hub conversion rate (review → lesson)
```

**Score Submission Metrics**
```
- Total scores submitted per game per difficulty
- Avg score per submission
- Personal best update frequency
- Score distribution percentiles
```

**Leaderboard Tier Analytics**
```
- Top 10 / Top 100 turnover rate
- Avg time at rank X
- Seasonal engagement trends
- Day/week/hour patterns
```

#### Dashboard Mockup

```
Analytics Dashboard
├─ Overview
│  ├─ Total Views (24h): 12,543
│  ├─ Active Players: 3,421 (45% of DAU)
│  ├─ Quiz Reviews Opened: 2,156
│  ├─ Avg Score Submitted: 1,245
│  └─ Top Game: Pattern Sprint (68% of submissions)
│
├─ Leaderboard Engagement
│  ├─ Game Breakdown
│  │  ├─ Pattern Sprint: 8,432 views (67%)
│  │  ├─ Memory Flip: 2,156 views (17%)
│  │  └─ Quick Math Rush: 1,955 views (16%)
│  │
│  ├─ Difficulty Breakdown (Pattern Sprint)
│  │  ├─ Easy: 2,104 views (25%)
│  │  ├─ Normal: 4,216 views (50%)
│  │  ├─ Hard: 1,584 views (18%)
│  │  └─ Insane: 528 views (7%)
│  │
│  └─ Trends
│     ├─ Peak hours: 6-9pm UTC
│     ├─ Weekend spike: +35% engagement
│     └─ Seasonal: Flat engagement
│
├─ Quiz Review Analysis
│  ├─ Total Reviews: 2,156 opened
│  ├─ Learn More Clicks: 643 (30%)
│  ├─ Lesson Starts: 412 (64% of clicks)
│  └─ Top Topics Reviewed: Mathematics (45%), Science (30%), History (25%)
│
└─ Score Submission Patterns
   ├─ Submissions/Hour
   ├─ Personal Best Updates: 31% of submissions
   └─ Score Distribution: [chart]
```

#### Implementation Approach

**Backend (C# — Synaptix.Backend.Api)**

Create analytics endpoint:
```csharp
// New file: Features/Analytics/AnalyticsEndpoints.cs
public static void Map(IEndpointRouteBuilder app)
{
  var g = app.MapGroup("/analytics").WithTags("Analytics");

  g.MapGet("/leaderboard/overview", GetLeaderboardOverview)
    .RequireAuthorization(policy: "AdminOnly");
  
  g.MapGet("/leaderboard/{gameId}/{difficulty}/trends", GetTrends)
    .RequireAuthorization(policy: "AdminOnly");
  
  g.MapGet("/quiz-review/metrics", GetQuizReviewMetrics)
    .RequireAuthorization(policy: "AdminOnly");
  
  g.MapGet("/score-submissions/distribution", GetScoreDistribution)
    .RequireAuthorization(policy: "AdminOnly");
}
```

**Database Enhancements**

Track events via new tables:
```sql
-- Event log for analytics
CREATE TABLE arcade_leaderboard_events (
  id UUID PRIMARY KEY,
  player_id UUID NOT NULL,
  event_type VARCHAR(50), -- 'view_leaderboard', 'submit_score', 'open_review'
  game_id VARCHAR(50),
  difficulty VARCHAR(20),
  timestamp TIMESTAMPTZ DEFAULT NOW(),
  FOREIGN KEY (player_id) REFERENCES users(id)
);

CREATE INDEX ix_arcade_events_game_difficulty_timestamp
  ON arcade_leaderboard_events (game_id, difficulty, timestamp DESC);

-- Event aggregations (updated hourly)
CREATE TABLE arcade_leaderboard_hourly_metrics (
  id UUID PRIMARY KEY,
  game_id VARCHAR(50),
  difficulty VARCHAR(20),
  hour TIMESTAMPTZ,
  view_count INT,
  unique_players INT,
  avg_score DECIMAL,
  submission_count INT,
  UNIQUE(game_id, difficulty, hour)
);
```

**Frontend (Flutter — Analytics Dashboard Screen)**

Create admin dashboard:
```dart
// New file: lib/admin/screens/analytics_dashboard_screen.dart
class AnalyticsDashboardScreen extends ConsumerStatefulWidget {
  @override
  ConsumerState<AnalyticsDashboardScreen> createState() =>
    _AnalyticsDashboardScreenState();
}

class _AnalyticsDashboardScreenState extends ConsumerState {
  DateRange _selectedDateRange = DateRange.last24h;

  @override
  Widget build(BuildContext context) {
    final overviewAsync = ref.watch(
      leaderboardOverviewProvider(_selectedDateRange)
    );

    return Scaffold(
      body: overviewAsync.when(
        loading: () => const LoadingScreen(),
        error: (e, _) => ErrorScreen(error: e),
        data: (overview) => SingleChildScrollView(
          child: Column(
            children: [
              // Date range picker
              DateRangePicker(
                selected: _selectedDateRange,
                onChanged: (range) => setState(() => _selectedDateRange = range),
              ),
              
              // KPI Cards
              Row(
                children: [
                  KpiCard(
                    title: 'Total Views',
                    value: overview.totalViews,
                    change: overview.changePercent,
                  ),
                  KpiCard(
                    title: 'Active Players',
                    value: overview.activePlayerCount,
                    change: overview.playerChangePercent,
                  ),
                  // ... more KPIs
                ],
              ),

              // Game breakdown chart
              GameBreakdownChart(data: overview.gameBreakdown),
              
              // Difficulty breakdown (for selected game)
              DifficultyChart(data: overview.difficultyBreakdown),
              
              // Trends over time
              EngagementTrendChart(data: overview.trends),
              
              // Quiz review metrics
              QuizReviewMetricsSection(metrics: overview.quizMetrics),
              
              // Score distribution
              ScoreDistributionChart(distribution: overview.scoreDistribution),
            ],
          ),
        ),
      ),
    );
  }
}
```

#### Complexity

**High**
- Requires event tracking throughout system
- Database schema changes for analytics tables
- Multiple UI components for dashboard
- Admin authorization policy needed
- Real-time aggregation (or batch job)

#### Why Deferred

- **Not critical for MVP:** Launch doesn't require analytics
- **High effort:** 20-30 hours of development
- **Can add post-launch:** Doesn't block player-facing features
- **Batch aggregation:** Can start with simple daily batch jobs before moving to real-time
- **Admin-only feature:** Doesn't impact player experience initially

#### Business Impact

- **High value for product team:** Understand player behavior
- **Strategic importance:** Drives future feature prioritization
- **Data-driven decisions:** Can optimize leaderboard engagement
- **Monetization insights:** Understand premium user patterns

#### Reconsidering in Phase 4

This enhancement should be prioritized if:
- Product team needs behavioral insights for feature planning
- Marketing team wants engagement metrics for user acquisition
- Leadership wants to understand monetization impact of leaderboards
- Player retention metrics show engagement plateauing

#### Rough Timeline for Phase 4

```
Week 1: Schema design + backend events
Week 2: Backend analytics endpoints + aggregation
Week 3: Flutter dashboard UI + charts
Week 4: Testing, optimization, documentation
```

---

### 3. Social Leaderboard Features (Phase 4+)

**Status:** ⏳ Deferred to Phase 4+  
**Effort Estimate:** 30-40 hours  
**Business Value:** Very High  
**Priority:** Medium-High (important for retention & engagement)

#### Description

Enable social features: friend leaderboards, head-to-head comparisons, challenges, and score notifications.

#### Proposed Features

**Friend Leaderboards**
```
Friends' Scores (Pattern Sprint - Normal)
├─ Your Rank: #42 globally, #3 among friends
├─ Friends Playing This Game: 8/12
└─ Top Friend Scores
   ├─ Alice: 1,800 (Rank #12)
   ├─ Bob: 1,650 (Rank #28)
   ├─ Carol: 1,550 (Rank #45)
   └─ [View All Friends]
```

**Head-to-Head Challenges**
```
Challenge Notifications
├─ "Alice challenged you to Pattern Sprint (Hard)"
│  ├─ Current Score: Alice 1,450 vs You 0
│  └─ [Accept Challenge] [Decline]
│
└─ Challenge Results
   ├─ You beat Alice! 1,500 > 1,450
   ├─ You gained 50 Challenge Points
   └─ Alice can challenge back
```

**Score Notifications**
```
Notification Settings
├─ Friend Beats Your Score
│  └─ "Alice just beat your Pattern Sprint score: 1,600 > 1,500"
│
├─ Friend Enters Leaderboard Top 100
│  └─ "Bob entered the top 100 in Quick Math Rush (Hard)!"
│
└─ Weekly Digest
   └─ "You're #3 among friends in Pattern Sprint"
```

#### Implementation Approach

**Database Schema**
```sql
CREATE TABLE friend_relationships (
  id UUID PRIMARY KEY,
  player_1_id UUID NOT NULL,
  player_2_id UUID NOT NULL,
  status VARCHAR(20), -- 'accepted', 'pending', 'blocked'
  created_at TIMESTAMPTZ,
  FOREIGN KEY (player_1_id) REFERENCES users(id),
  FOREIGN KEY (player_2_id) REFERENCES users(id),
  UNIQUE(player_1_id, player_2_id)
);

CREATE TABLE score_challenges (
  id UUID PRIMARY KEY,
  challenger_id UUID NOT NULL,
  challenged_id UUID NOT NULL,
  game_id VARCHAR(50),
  difficulty VARCHAR(20),
  challenger_score INT,
  challenged_score INT,
  winner_id UUID,
  challenge_points INT,
  created_at TIMESTAMPTZ,
  completed_at TIMESTAMPTZ,
  FOREIGN KEY (challenger_id) REFERENCES users(id),
  FOREIGN KEY (challenged_id) REFERENCES users(id)
);

CREATE TABLE social_notifications (
  id UUID PRIMARY KEY,
  player_id UUID NOT NULL,
  notification_type VARCHAR(50),
  related_player_id UUID,
  game_id VARCHAR(50),
  score INT,
  is_read BOOLEAN DEFAULT FALSE,
  created_at TIMESTAMPTZ,
  FOREIGN KEY (player_id) REFERENCES users(id),
  FOREIGN KEY (related_player_id) REFERENCES users(id)
);
```

**Backend Endpoints**
```csharp
// Social endpoints
GET    /social/friends                  -- Get user's friends
POST   /social/friends/{userId}         -- Send friend request
PUT    /social/friends/{userId}/accept  -- Accept friend request
DELETE /social/friends/{userId}         -- Remove friend

GET    /social/leaderboard/{gameId}/{difficulty}  -- Friends' scores
GET    /social/challenges               -- List active challenges
POST   /social/challenges/{userId}      -- Challenge a friend
PUT    /social/challenges/{challengeId} -- Submit challenge score

GET    /social/notifications            -- List notifications
PUT    /social/notifications/{notifId}  -- Mark as read
```

**Flutter UI Screens**
```dart
// New screens
- lib/screens/social/friends_screen.dart
- lib/screens/social/friend_leaderboard_screen.dart
- lib/screens/social/challenges_screen.dart
- lib/screens/social/notifications_screen.dart

// Widget components
- lib/widgets/friend_score_card.dart
- lib/widgets/challenge_card.dart
- lib/widgets/social_notification_item.dart
```

#### Complexity

**Very High**
- Friend relationship management
- Challenge lifecycle (creation, completion, results)
- Real-time notifications
- Social graph queries (can be expensive)
- Privacy/blocking features
- Push notifications infrastructure

#### Why Deferred

- **Architectural complexity:** Friend relationships add data model complexity
- **Notification infrastructure:** Requires push notification setup
- **Not critical for MVP:** Leaderboards work fine without social features initially
- **Can iterate separately:** Social features can be independent from core leaderboards
- **Privacy considerations:** Need to design blocking/privacy features carefully

#### Business Impact

- **Very high engagement multiplier:** Social comparison drives replays (3-5x)
- **Retention driver:** Friends playing together = higher retention
- **Viral coefficient:** Friend invites = user acquisition
- **Monetization: Challenge rewards encourage spending

#### Reconsidering in Phase 4+

This enhancement should be prioritized if:
- Engagement plateau detected after 2-3 weeks
- Player retention dropping (want to re-engage friends)
- Competitive players requesting features
- Marketing wants social viral coefficient

#### Rough Timeline for Phase 4+ (4+ weeks work)

```
Week 1: Schema design + friend relationship endpoints
Week 2: Challenge system implementation
Week 3: Notifications infrastructure + backend
Week 4: Flutter UI implementation
Week 5: Testing, push notifications, documentation
```

---

## Summary: Deferred vs. Selected

| Enhancement | Selected | Status | Effort | Business Value | Defer Reason |
|-------------|----------|--------|--------|---|---|
| **Learning Hub Integration** | ✅ | Phase 2 | 4-6 hrs | High | — |
| **Seasonal Leaderboards** | ✅ | Phase 2 | 6-8 hrs | Medium | — |
| **Performance Caching** | ✅ | Phase 2 | 4-6 hrs | Medium | — |
| **Difficulty Picker (Local)** | ❌ | Phase 3 | 2-3 hrs | Low | Low priority, can add anytime |
| **Analytics Dashboard** | ❌ | Phase 4 | 20-30 hrs | High | High effort, valuable post-launch |
| **Social Features** | ❌ | Phase 4+ | 30-40 hrs | Very High | Very high effort & complexity |

---

## Evaluation Criteria Used

When selecting Phase 2 enhancements, we prioritized:

1. **Time to Market** — Phase 2 had 2-3 week budget; chose items doable in that time
2. **Player Impact** — Chose features with immediate benefit to players
3. **Technical Feasibility** — Chose lower-complexity features to reduce risk
4. **Non-Blocking** — Chose features that don't depend on others

Deferred enhancements didn't fail on these criteria; they just scored lower or were higher complexity.

---

## Revisiting Deferred Enhancements

### Phase 3 Decision Point (Week 3 Post-Launch)

After Phase 2 launches, reassess:
- [ ] User feedback: Do players want difficulty picker?
- [ ] Engagement metrics: Is leaderboard engagement strong?
- [ ] Roadmap: Any product shifts that change priority?

**Action:** If answers are yes/yes/no, bump Difficulty Picker to Phase 3.

### Phase 4 Decision Point (Week 6 Post-Launch)

After Phase 2+3 are stable:
- [ ] Product analytics: Do we need the dashboard?
- [ ] Resource availability: Do we have 20-30 hours?
- [ ] Strategic priorities: Is engagement > monetization?

**Action:** Pick 1-2 enhancements from Phase 4 queue based on answers.

---

## Future Enhancement Candidates (Phase 5+)

If you ship all of the above, consider:

1. **Leaderboard Seasons** — Reset rankings periodically (monthly, quarterly)
   - Effort: 8-12 hrs
   - Value: Keeps leaderboards fresh, rewards consistent players
   
2. **Achievements & Badges** — Unlock badges for leaderboard milestones
   - Effort: 10-15 hrs
   - Value: Gamification, retention driver
   
3. **Leaderboard Moderation** — Flag/remove suspicious scores
   - Effort: 4-6 hrs
   - Value: Anti-cheat, leaderboard integrity
   
4. **Replay Analysis** — Replay arcade game runs to debug issues
   - Effort: 15-20 hrs
   - Value: Support tool, cheat detection
   
5. **Streaming Integration** — Share scores to Twitch/YouTube
   - Effort: 10-15 hrs
   - Value: Viral marketing, player engagement

---

## Notes

- This document should be reviewed quarterly to reassess priorities
- Track user feedback on deferred features for future planning
- Monitor competitive landscape for social/analytics features other apps ship
- Consider pairing deferred enhancements (e.g., Analytics + Social go together)

---

**Last Updated:** July 1, 2026  
**Next Review:** October 1, 2026 (post-Phase 2 stability)
