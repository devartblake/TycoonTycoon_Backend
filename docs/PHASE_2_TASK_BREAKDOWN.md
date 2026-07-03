# Phase 2 Task Breakdown — Implementation Checklist

**Timeline:** 2-3 weeks post-launch (July 8-19, 2026)  
**Total Effort:** 14-20 hours (1.5-2.5 days of development)  
**Status:** 🟡 Ready for Implementation

---

## Project Structure

```
Phase 2 Enhancement: Learning Hub Integration (4-6 hours)
├─ BACKEND (2-3 hours)
│  ├─ Database: Create question_lesson_mappings table
│  ├─ Migration: Write EF Core migration
│  ├─ API: Implement GET /questions/{questionId}/lessons
│  ├─ API: Implement POST /quiz-review/learn-more-click
│  ├─ API: Implement GET /quiz/recommended-lessons
│  ├─ Service: Create QuizReviewAnalyticsService
│  └─ Tests: Add 10+ unit/integration tests
│
└─ FRONTEND (1-2 hours)
   ├─ Model: Create LessonDto data class
   ├─ Provider: Add questionsLessonsProvider
   ├─ Provider: Add quizReviewAnalyticsProvider
   ├─ Service: Create QuizReviewAnalyticsService
   ├─ UI: Add "Learn More" button to QuizReviewScreen
   ├─ Navigation: Wire to learning hub screen
   └─ Tests: Integration testing on all platforms

Phase 2 Enhancement: Seasonal Leaderboards (6-8 hours)
├─ BACKEND (3-4 hours)
│  ├─ Database: Add season_id, season_type columns to arcade_scores
│  ├─ Database: Create arcade_seasons table
│  ├─ Migration: Write EF Core migration
│  ├─ Service: Create ISeasonManagementService
│  ├─ API: Update GetArcadeLeaderboard with seasonType filter
│  ├─ API: Add GET /leaderboards/arcade/seasons
│  ├─ API: Add GET /leaderboards/arcade/player-stats
│  ├─ Handler: Update SubmitArcadeScore to assign season
│  ├─ Job: Create background job for season transitions
│  └─ Tests: Add 15+ unit/integration tests
│
└─ FRONTEND (2-3 hours)
   ├─ Enum: Create SeasonType enum
   ├─ Provider: Add selectedSeasonProvider
   ├─ Provider: Add leaderboardWithSeasonProvider
   ├─ UI: Add season selector (segmented button)
   ├─ UI: Update LeaderboardListView with season filtering
   ├─ Navigation: Update main leaderboard integration
   └─ Tests: Test season filtering on all platforms

Phase 2 Enhancement: Performance Caching (4-6 hours)
├─ BACKEND (2-3 hours)
│  ├─ Service: Create ILeaderboardCacheService interface
│  ├─ Service: Implement InMemoryLeaderboardCacheService
│  ├─ DI: Register cache in dependency injection
│  ├─ Handler: Update GetArcadeLeaderboard to use cache
│  ├─ Handler: Update SubmitArcadeScore to invalidate cache
│  ├─ API: Add GET /admin/leaderboard-cache/stats (admin only)
│  └─ Tests: Add 10+ cache hit/miss scenario tests
│
└─ FRONTEND (1-2 hours)
   ├─ Service: Create LocalLeaderboardCacheService
   ├─ Service: Integrate into ArcadeLeaderboardApiService
   ├─ Logging: Add cache hit/miss logging
   ├─ Testing: Test cache behavior on slow networks
   └─ Monitoring: Setup cache performance tracking
```

---

## Task Cards by Priority

### LEARNING HUB INTEGRATION

#### Card 1.1: Backend - Database Schema
- **Title:** Create question_lesson_mappings table
- **Effort:** 30 minutes
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Create migration file `20260708000000_CreateQuestionLessonMappings.cs`
  - [ ] Add `question_lesson_mappings` table with proper foreign keys
  - [ ] Add indexes on question_id and lesson_id
  - [ ] Add unique constraint (question_id, lesson_id)
  - [ ] Test migration on staging database
  - [ ] Document schema in PR

**Definition of Done:**
- Migration applies without errors
- Table structure verified in database
- Indexes exist and perform well
- PR reviewed and approved

---

#### Card 1.2: Backend - API Endpoints
- **Title:** Implement 3 learning hub API endpoints
- **Effort:** 1-1.5 hours
- **Owner:** Backend Developer
- **Endpoints:**
  1. `GET /api/v1/questions/{questionId}/lessons`
  2. `POST /api/v1/quiz-review/learn-more-click`
  3. `GET /api/v1/quiz/recommended-lessons`

**Checklist:**
- [ ] Implement QuizReviewEndpoints.cs with 3 handlers
- [ ] Add request validation (questionId, lessonId)
- [ ] Add response DTOs (LessonDto, RecommendedLessonsDto)
- [ ] Add error handling (404 for missing question)
- [ ] Add Swagger documentation for all endpoints
- [ ] Add unit tests for each endpoint (happy path + error cases)

**Definition of Done:**
- All endpoints accessible and returning correct data
- Swagger docs complete and accurate
- Tests passing (6+ test cases minimum)
- Code review approved

---

#### Card 1.3: Backend - Analytics Service
- **Title:** Create QuizReviewAnalyticsService for tracking
- **Effort:** 45 minutes
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Create QuizReviewAnalyticsService class
  - [ ] Implement trackLearnMoreClick(questionId, context)
  - [ ] Integrate with IAnalyticsEventService
  - [ ] Add logging for tracking calls
  - [ ] Add tests for tracking logic
  - [ ] Verify events recorded in database

**Definition of Done:**
- Service created and injectable
- Tracking calls logged correctly
- Tests passing (4+ test cases)
- Integration with analytics events verified

---

#### Card 1.4: Backend - Testing
- **Title:** Add 10+ tests for Learning Hub features
- **Effort:** 1 hour
- **Owner:** QA/Backend Developer
- **Test Coverage:**
  - [ ] Test fetching lessons for valid question
  - [ ] Test handling missing questions (404)
  - [ ] Test learn-more click tracking
  - [ ] Test recommended lessons filtering
  - [ ] Test analytics event creation
  - [ ] Test authorization (admin only for some endpoints)
  - [ ] Test error handling (DB failures, timeouts)
  - [ ] Performance tests (response time < 200ms)

**Definition of Done:**
- 10+ tests passing
- Coverage report shows learning hub code > 80% covered
- All edge cases tested

---

#### Card 1.5: Frontend - Data Models & Providers
- **Title:** Create LessonDto and Riverpod providers
- **Effort:** 45 minutes
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Create LessonDto class with JSON serialization
  - [ ] Add questionsLessonsProvider (FutureProvider)
  - [ ] Add quizReviewAnalyticsProvider (Provider)
  - [ ] Add unit tests for providers
  - [ ] Add error handling and loading states

**Definition of Done:**
- Models created and compile without errors
- Providers accessible and return data correctly
- Tests passing (4+ test cases)
- Type safety verified (no `dynamic` casts)

---

#### Card 1.6: Frontend - UI Implementation
- **Title:** Add "Learn More" button to QuizReviewScreen
- **Effort:** 1 hour
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Update QuizReviewScreen to show button for incorrect answers
  - [ ] Implement navigation to learning hub on tap
  - [ ] Add loading state while fetching lessons
  - [ ] Add error handling (show snackbar if lessons fetch fails)
  - [ ] Test on Android, iOS, Web, Windows
  - [ ] Verify visual alignment and styling

**Definition of Done:**
- Button appears only for incorrect answers
- Navigation works on all platforms
- Error states handled gracefully
- Manual testing passed on all devices

---

#### Card 1.7: Frontend - Integration & Testing
- **Title:** End-to-end testing of Learning Hub feature
- **Effort:** 1 hour
- **Owner:** QA/Frontend Developer
- **Testing Scenarios:**
  1. [ ] Play Pattern Sprint game, get question wrong
  2. [ ] Tap "Review Answers" button
  3. [ ] Tap "Learn More" on incorrect answer
  4. [ ] Verify learning hub opens with related lessons
  5. [ ] Verify tracking click is sent to backend
  6. [ ] Test on slow network (throttle to 3G)
  7. [ ] Test offline mode (no network)

**Definition of Done:**
- All scenarios pass on Android/iOS/Web/Windows
- No crashes or unhandled errors
- Performance acceptable (< 2 seconds to load lessons)

---

### SEASONAL LEADERBOARDS

#### Card 2.1: Backend - Database Changes
- **Title:** Add season tracking to arcade_scores table
- **Effort:** 45 minutes
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Add season_id column to arcade_scores
  - [ ] Add season_type column to arcade_scores
  - [ ] Create arcade_seasons table
  - [ ] Add foreign key constraints
  - [ ] Add indexes for season queries
  - [ ] Write migration file
  - [ ] Test migration

**Definition of Done:**
- Tables created with proper schema
- Indexes exist for season queries
- Migration applies cleanly
- No data loss during migration

---

#### Card 2.2: Backend - Season Management Service
- **Title:** Create ISeasonManagementService
- **Effort:** 1 hour
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Create ISeasonManagementService interface
  - [ ] Implement GetCurrentSeason(type: weekly|monthly)
  - [ ] Implement GetAvailableSeasons()
  - [ ] Implement GetSeasonForDate(date)
  - [ ] Add background job for season transitions
  - [ ] Add unit tests

**Definition of Done:**
- Service callable and returns correct seasons
- Background job runs on schedule
- Tests passing (8+ test cases)

---

#### Card 2.3: Backend - API Updates
- **Title:** Update leaderboard API for seasonal filtering
- **Effort:** 1.5 hours
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Update GetArcadeLeaderboard query parameter: `seasonType`
  - [ ] Update SubmitArcadeScore to assign season_id
  - [ ] Add GET /leaderboards/arcade/seasons endpoint
  - [ ] Add GET /leaderboards/arcade/player-stats endpoint
  - [ ] Update response DTOs to include seasonType
  - [ ] Add Swagger documentation
  - [ ] Add tests (10+ test cases)

**Definition of Done:**
- API endpoints working correctly
- Filters apply properly
- Response includes season information
- Tests passing

---

#### Card 2.4: Frontend - Providers & State
- **Title:** Add season selection and filtering providers
- **Effort:** 1 hour
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Create SeasonType enum
  - [ ] Create selectedSeasonProvider (StateProvider)
  - [ ] Create leaderboardWithSeasonProvider (FutureProvider)
  - [ ] Add error handling
  - [ ] Add tests

**Definition of Done:**
- Providers work correctly
- State updates on season selection
- Tests passing

---

#### Card 2.5: Frontend - UI Implementation
- **Title:** Add season selector to leaderboard screens
- **Effort:** 1.5 hours
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Add SegmentedButton for season selection (This Week, This Month, All Time)
  - [ ] Update LeaderboardListView to use selectedSeasonProvider
  - [ ] Update ArcadeGlobalLeaderboardView with season selector
  - [ ] Add loading state while fetching seasonal data
  - [ ] Add animations for season transitions
  - [ ] Test on all platforms

**Definition of Done:**
- Season selector visible and functional
- Leaderboard updates when season changes
- UI responsive on all screen sizes
- No crashes

---

#### Card 2.6: Frontend - Testing
- **Title:** Test seasonal leaderboard functionality
- **Effort:** 1 hour
- **Owner:** QA/Frontend Developer
- **Scenarios:**
  - [ ] Switch between weekly/monthly/all-time
  - [ ] Verify correct leaderboard data shown
  - [ ] Submit score and verify it appears in current season
  - [ ] Test offline mode
  - [ ] Test on slow network

**Definition of Done:**
- All scenarios pass
- No visual glitches
- Performance acceptable

---

### PERFORMANCE CACHING

#### Card 3.1: Backend - Cache Service
- **Title:** Implement InMemoryLeaderboardCacheService
- **Effort:** 1.5 hours
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Create ILeaderboardCacheService interface
  - [ ] Implement InMemoryLeaderboardCacheService
  - [ ] Add 5-minute TTL with sliding expiration
  - [ ] Implement cache invalidation on score submission
  - [ ] Add cache statistics tracking
  - [ ] Add cache diagnostics logging
  - [ ] Add unit tests

**Definition of Done:**
- Cache service working correctly
- Hit rate > 85% in testing
- Invalidation triggers properly
- Tests passing (8+ test cases)

---

#### Card 3.2: Backend - Handler Integration
- **Title:** Integrate cache into GetArcadeLeaderboard handler
- **Effort:** 1 hour
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Inject ILeaderboardCacheService into handler
  - [ ] Check cache before database query
  - [ ] Update cache on miss
  - [ ] Invalidate cache on SubmitArcadeScore
  - [ ] Add logging for cache hits/misses
  - [ ] Add performance tests

**Definition of Done:**
- Cache integrated and working
- Performance improved (< 50ms for cache hits)
- Logging shows cache effectiveness
- Tests passing

---

#### Card 3.3: Backend - Admin Endpoint
- **Title:** Add cache statistics endpoint (admin only)
- **Effort:** 30 minutes
- **Owner:** Backend Developer
- **Checklist:**
  - [ ] Create CacheStatistics DTO
  - [ ] Implement GET /admin/leaderboard-cache/stats
  - [ ] Add admin authorization
  - [ ] Return hit rate, total hits/misses
  - [ ] Add Swagger documentation

**Definition of Done:**
- Endpoint accessible and returns stats
- Only admins can access
- Stats accurate

---

#### Card 3.4: Frontend - Local Cache Service
- **Title:** Create LocalLeaderboardCacheService for Flutter
- **Effort:** 1 hour
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Create LocalLeaderboardCacheService class
  - [ ] Implement caching logic (in-memory)
  - [ ] Integrate into ArcadeLeaderboardApiService
  - [ ] Add cache invalidation
  - [ ] Add logging
  - [ ] Add tests

**Definition of Done:**
- Service created and working
- Caching transparent to consumers
- Tests passing
- Logging shows cache effectiveness

---

#### Card 3.5: Frontend - Monitoring
- **Title:** Setup cache performance monitoring
- **Effort:** 45 minutes
- **Owner:** Frontend Developer
- **Checklist:**
  - [ ] Add cache hit/miss logging
  - [ ] Measure query times (with/without cache)
  - [ ] Monitor cache memory usage
  - [ ] Test on low-memory devices
  - [ ] Add performance assertions

**Definition of Done:**
- Monitoring data available
- Performance meets targets (10x improvement)
- Memory usage acceptable

---

## Weekly Standup Template

### Monday (Day 8 - Start of Phase 2)

**Status:** 🟡 Phase 2 Kickoff

**Planned this week:**
- [ ] Card 1.1-1.4: Learning Hub backend complete
- [ ] Card 2.1-2.3: Seasonal backend complete  
- [ ] Card 3.1-3.2: Caching backend complete
- [ ] Card 1.5-1.6: Learning Hub frontend start

**Blockers:** None identified

---

### Friday (Day 12 - Mid-point)

**Status:** 🟡 50% Complete

**Completed:**
- [x] Learning Hub backend (3 cards)
- [x] Seasonal backend (2 cards)
- [x] Caching backend (2 cards)

**In Progress:**
- [ ] Learning Hub frontend (2 cards)
- [ ] Seasonal frontend (2 cards)
- [ ] Caching frontend (1 card)

**Blockers:** (Update if any)

---

### Friday (Day 15 - End of Phase 2)

**Status:** ✅ Phase 2 Complete

**Completed:**
- [x] Learning Hub (7 cards)
- [x] Seasonal (6 cards)
- [x] Caching (5 cards)

**Testing:**
- [x] Unit tests passing (30+)
- [x] Integration tests passing (15+)
- [x] Platform testing complete (Android/iOS/Web/Windows)

**Next:** Phase 2 deployed to production

---

## Effort Tracking

### Estimated Effort by Feature

| Feature | Backend | Frontend | Total | Actual |
|---------|---------|----------|-------|--------|
| Learning Hub | 2-3h | 1-2h | 4-6h | _ |
| Seasonal | 3-4h | 2-3h | 6-8h | _ |
| Caching | 2-3h | 1-2h | 4-6h | _ |
| **TOTAL** | **7-10h** | **4-7h** | **14-20h** | _ |

### Actual Effort (Update Daily)

- Day 8: ___ hours
- Day 9: ___ hours
- Day 10: ___ hours
- Day 11: ___ hours
- Day 12: ___ hours
- Day 13: ___ hours
- Day 14: ___ hours
- Day 15: ___ hours

---

## Success Criteria

### Functionality
- ✅ Learning Hub links appear and work
- ✅ Seasons filter correctly
- ✅ Cache improves performance 10x

### Testing
- ✅ 30+ unit tests passing
- ✅ 15+ integration tests passing
- ✅ Platform testing complete

### Performance
- ✅ Cache hit rate > 85%
- ✅ Leaderboard queries < 50ms (cached)
- ✅ No performance regressions

### Quality
- ✅ All code reviewed
- ✅ Zero critical bugs found
- ✅ Documentation complete

---

**Ready to Begin:** 2026-07-08  
**Target Completion:** 2026-07-19  
**Status:** 🟡 Ready for Implementation
